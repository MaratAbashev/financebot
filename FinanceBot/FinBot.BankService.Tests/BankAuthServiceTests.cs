using FinBot.BankService.BankingApi;
using FinBot.BankService.Cache;
using FinBot.BankService.Models;
using FinBot.BankService.Repositories;
using FinBot.BankService.Services;
using FinBot.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FinBot.BankService.Tests;

/// <summary>
/// Тесты BankAuthService на основе TC-BANK-001 — TC-BANK-004
/// </summary>
public class BankAuthServiceTests
{
    private readonly IBankConnectionRepository _connections;
    private readonly ITokenCache _tokenCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BankAuthService _sut;
 
    public BankAuthServiceTests()
    {
        _connections = Substitute.For<IBankConnectionRepository>();
        _tokenCache = Substitute.For<ITokenCache>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        var bankingApiOptions = Substitute.For<IOptions<BankingApiOptions>>();
        var timeProvider = Substitute.For<TimeProvider>();
        var logger = Substitute.For<ILogger<BankAuthService>>();
 
        bankingApiOptions.Value.Returns(new BankingApiOptions
        {
            BaseUrl = "http://bankingapi:8080",
            RedirectUrl = "http://finbot.bankservice:8080"
        });
 
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
 
        _sut = new BankAuthService(
            _connections,
            _tokenCache,
            _httpClientFactory,
            bankingApiOptions,
            timeProvider,
            logger);
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-001 — Успешная привязка банка
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-001: Новый пользователь — Access токен пишется в Redis, Refresh — в БД.
    /// </summary>
    [Fact]
    public async Task HandleCallbackAsync_NewUser_SavesAccessToRedisAndRefreshToDb()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var payload = new OAuthCallbackPayload(
            AccessToken: "access-token-123",
            RefreshToken: "refresh-token-456",
            State: userId.ToString());
 
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .ReturnsNull();
 
        // Act
        await _sut.HandleCallbackAsync(payload);
 
        // Assert
        await _tokenCache.Received(1).SetAsync(
            userId,
            "access-token-123",
            Arg.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(15)));
 
        await _connections.Received(1).AddAsync(
            Arg.Is<BankConnection>(c =>
                c.UserId == userId &&
                c.RefreshToken == "refresh-token-456" &&
                c.IsActive),
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// TC-BANK-001: Повторная привязка — Refresh обновляется, дубль не создаётся.
    /// </summary>
    [Fact]
    public async Task HandleCallbackAsync_ExistingUser_UpdatesRefreshTokenWithoutCreatingNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = new BankConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshToken = "old-refresh-token",
            IsActive = true
        };
 
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existing);
 
        var payload = new OAuthCallbackPayload(
            AccessToken: "new-access-token",
            RefreshToken: "new-refresh-token",
            State: userId.ToString());
 
        // Act
        await _sut.HandleCallbackAsync(payload);
 
        // Assert
        await _connections.DidNotReceive().AddAsync(
            Arg.Any<BankConnection>(),
            Arg.Any<CancellationToken>());
 
        await _connections.Received(1).UpdateRefreshTokenAsync(
            userId,
            "new-refresh-token",
            Arg.Any<CancellationToken>());
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-002 — Неверные учётные данные при OAuth
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-002: Невалидный state (не Guid) — токены не сохраняются.
    /// </summary>
    [Fact]
    public async Task HandleCallbackAsync_InvalidState_DoesNotSaveTokens()
    {
        // Arrange
        var payload = new OAuthCallbackPayload(
            AccessToken: "access-token",
            RefreshToken: "refresh-token",
            State: "not-a-guid");
 
        // Act
        await _sut.HandleCallbackAsync(payload);
 
        // Assert
        await _tokenCache.DidNotReceive().SetAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>());
 
        await _connections.DidNotReceive().AddAsync(
            Arg.Any<BankConnection>(),
            Arg.Any<CancellationToken>());
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-003 — Попытка привязки уже подключённого банка
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-003: Банк уже привязан — обновляет токен, дубль не создаёт.
    /// </summary>
    [Fact]
    public async Task HandleCallbackAsync_BankAlreadyConnected_UpdatesExistingConnection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = new BankConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshToken = "old-token",
            IsActive = true
        };
 
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existing);
 
        var payload = new OAuthCallbackPayload("new-access", "new-refresh", userId.ToString());
 
        // Act
        await _sut.HandleCallbackAsync(payload);
 
        // Assert
        await _connections.DidNotReceive().AddAsync(
            Arg.Any<BankConnection>(),
            Arg.Any<CancellationToken>());
 
        await _connections.Received(1).UpdateRefreshTokenAsync(
            userId,
            "new-refresh",
            Arg.Any<CancellationToken>());
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-004 — GetAuthUrl для не подключённого пользователя
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-004: Запрос ссылки — BankService обращается к BankingApi и возвращает authUrl.
    /// </summary>
    [Fact]
    public async Task GetAuthUrlAsync_ValidRequest_ReturnsAuthUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
 
        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.OK,
            "{\"authUrl\": \"http://bankingapi/oauth/login?code=abc123\"}");
 
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));
 
        // Act
        var result = await _sut.GetAuthUrlAsync(userId);
 
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("oauth/login", result.Data);
    }
 
    /// <summary>
    /// TC-BANK-004: BankingApi недоступен — возвращается Failure.
    /// </summary>
    [Fact]
    public async Task GetAuthUrlAsync_BankingApiUnavailable_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
 
        var handler = new FakeHttpMessageHandler(
            System.Net.HttpStatusCode.ServiceUnavailable,
            "");
 
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));
 
        // Act
        var result = await _sut.GetAuthUrlAsync(userId);
 
        // Assert
        Assert.False(result.IsSuccess);
    }
}