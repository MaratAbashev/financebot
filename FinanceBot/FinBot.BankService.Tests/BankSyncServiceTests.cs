using FinBot.BankService.BankingApi;
using FinBot.BankService.Cache;
using FinBot.BankService.Repositories;
using FinBot.BankService.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace FinBot.BankService.Tests;

/// <summary>
/// Тесты BankSyncService на основе TC-BANK-005 — TC-BANK-006
/// </summary>
public class BankSyncServiceTests
{
    private readonly IBankConnectionRepository _connections;
    private readonly IBankTransactionRepository _transactions;
    private readonly IExpenseWriteRepository _expenseRepo;
    private readonly ITokenCache _tokenCache;
    private readonly IBankingApiClient _bankingApi;
    private readonly BankSyncService _sut;
 
    public BankSyncServiceTests()
    {
        _connections = Substitute.For<IBankConnectionRepository>();
        _transactions = Substitute.For<IBankTransactionRepository>();
        _expenseRepo = Substitute.For<IExpenseWriteRepository>();
        _tokenCache = Substitute.For<ITokenCache>();
        _bankingApi = Substitute.For<IBankingApiClient>();
        var timeProvider = Substitute.For<TimeProvider>();
        var logger = Substitute.For<ILogger<BankSyncService>>();
 
        timeProvider.GetUtcNow().Returns(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
 
        _sut = new BankSyncService(
            _connections,
            _transactions,
            _tokenCache,
            _bankingApi,
            _expenseRepo,
            timeProvider,
            logger);
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-005 — Отключение банка
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-005: Нет активных подключений — Banking API не вызывается.
    /// </summary>
    [Fact]
    public async Task SyncAllAsync_NoActiveConnections_DoesNotCallBankingApi()
    {
        // Arrange
        _connections.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BankConnection>());
 
        // Act
        await _sut.SyncAllAsync();
 
        // Assert
        await _bankingApi.DidNotReceive().GetAccountsAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// TC-BANK-005: Есть активные подключения — Banking API вызывается для каждого.
    /// </summary>
    [Fact]
    public async Task SyncAllAsync_WithActiveConnections_CallsApiForEachUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
 
        _connections.GetAllActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BankConnection>
            {
                new() { UserId = userId1, RefreshToken = "rt1", IsActive = true },
                new() { UserId = userId2, RefreshToken = "rt2", IsActive = true }
            });
 
        _tokenCache.GetAsync(userId1).Returns("token1");
        _tokenCache.GetAsync(userId2).Returns("token2");
 
        _bankingApi.GetAccountsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<List<AccountDto>>.Success([]));
 
        // Act
        await _sut.SyncAllAsync();
 
        // Assert — API вызван дважды (по одному на каждого пользователя)
        await _bankingApi.Received(2).GetAccountsAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
 
    // ─────────────────────────────────────────────────────────────────
    // TC-BANK-006 — Ручная синхронизация
    // ─────────────────────────────────────────────────────────────────
 
    /// <summary>
    /// TC-BANK-006 (вариант 1): Есть новые транзакции — сохраняются в БД, count > 0.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_NewExpenseTransaction_SavesToBothDbsAndReturnsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        var txDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
 
        _tokenCache.GetAsync(userId).Returns("cached-access-token");
 
        _bankingApi.GetAccountsAsync("cached-access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<AccountDto>>.Success(
            [
                new AccountDto(accountId, "Основной", 85000, "Rub", DateTime.Now)
            ]));
 
        _bankingApi.GetTransactionsAsync(accountId, "cached-access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<TransactionDto>>.Success(
            [
                new TransactionDto(txId, 1500, "Expense", "Продукты", "Пятёрочка", txDate)
            ]));
 
        _transactions.ExistsAsync(txId, Arg.Any<CancellationToken>()).Returns(false);
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert
        Assert.Equal(1, count);
 
        // Сохранено в BankTransaction с правильными полями
        await _transactions.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<BankTransaction>>(list =>
                list.Any(t =>
                    t.ExternalId == txId &&
                    t.UserId == userId &&
                    t.Amount == 1500 &&
                    t.Status == BankTransactionStatus.Pending)),
            Arg.Any<CancellationToken>());
 
        // Сохранено в общую БД как Expense
        await _expenseRepo.Received(1).AddAsync(
            userId,
            1500,
            Arg.Any<ExpenseCategory>(),
            txDate,
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// TC-BANK-006 (вариант 2): Новых транзакций нет — count = 0, Expense не записывается.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_NoNewTransactions_ReturnsZeroAndDoesNotWriteExpense()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var existingTxId = Guid.NewGuid();
 
        _tokenCache.GetAsync(userId).Returns("cached-access-token");
 
        _bankingApi.GetAccountsAsync("cached-access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<AccountDto>>.Success(
            [
                new AccountDto(accountId, "Основной", 85000, "Rub", DateTime.Now)
            ]));
 
        _bankingApi.GetTransactionsAsync(accountId, "cached-access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<TransactionDto>>.Success(
            [
                new TransactionDto(existingTxId, 500, "Expense", "Транспорт", null, DateTime.UtcNow)
            ]));
 
        // Транзакция уже есть в БД
        _transactions.ExistsAsync(existingTxId, Arg.Any<CancellationToken>()).Returns(true);
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert
        Assert.Equal(0, count);
 
        await _expenseRepo.DidNotReceive().AddAsync(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<ExpenseCategory>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// TC-BANK-006: Income транзакции не записываются в Expense.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_IncomeTransaction_DoesNotWriteToExpenseRepo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var txId = Guid.NewGuid();
 
        _tokenCache.GetAsync(userId).Returns("access-token");
 
        _bankingApi.GetAccountsAsync("access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<AccountDto>>.Success(
            [
                new AccountDto(accountId, "Накопительный", 50000, "Rub", DateTime.Now)
            ]));
 
        _bankingApi.GetTransactionsAsync(accountId, "access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<TransactionDto>>.Success(
            [
                new TransactionDto(txId, 150000, "Income", "Зарплата", "За май", DateTime.UtcNow)
            ]));
 
        _transactions.ExistsAsync(txId, Arg.Any<CancellationToken>()).Returns(false);
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert — транзакция сохранена в BankService БД
        Assert.Equal(1, count);
 
        await _transactions.Received(1).AddRangeAsync(
            Arg.Any<IEnumerable<BankTransaction>>(),
            Arg.Any<CancellationToken>());
 
        // Но в Expense не попала
        await _expenseRepo.DidNotReceive().AddAsync(
            Arg.Any<Guid>(),
            Arg.Any<decimal>(),
            Arg.Any<ExpenseCategory>(),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// TC-BANK-004: Нет токена и нет подключения — API не вызывается, возвращает 0.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_NoTokenAndNoConnection_ReturnsZeroWithoutCallingApi()
    {
        // Arrange
        var userId = Guid.NewGuid();
 
        _tokenCache.GetAsync(userId).ReturnsNull();
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).ReturnsNull();
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert
        Assert.Equal(0, count);
 
        await _bankingApi.DidNotReceive().GetAccountsAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
 
    /// <summary>
    /// Ротация токена: Access протух в Redis — рефрешится через BankingApi и кешируется заново.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_AccessTokenExpired_RefreshesTokenAndCaches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connection = new BankConnection
        {
            UserId = userId,
            RefreshToken = "stored-refresh",
            IsActive = true
        };
 
        _tokenCache.GetAsync(userId).ReturnsNull();
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(connection);
 
        _bankingApi.RefreshTokenAsync("stored-refresh", Arg.Any<CancellationToken>())
            .Returns(Result<string>.Success("new-access-token"));
 
        _bankingApi.GetAccountsAsync("new-access-token", Arg.Any<CancellationToken>())
            .Returns(Result<List<AccountDto>>.Success([]));
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert — новый токен закешировался
        await _tokenCache.Received(1).SetAsync(
            userId,
            "new-access-token",
            Arg.Is<TimeSpan>(t => t == TimeSpan.FromMinutes(15)));
 
        Assert.Equal(0, count);
    }
 
    /// <summary>
    /// Refresh токен невалиден — синхронизация не выполняется.
    /// </summary>
    [Fact]
    public async Task SyncUserAsync_RefreshTokenInvalid_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connection = new BankConnection
        {
            UserId = userId,
            RefreshToken = "invalid-refresh",
            IsActive = true
        };
 
        _tokenCache.GetAsync(userId).ReturnsNull();
        _connections.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(connection);
 
        _bankingApi.RefreshTokenAsync("invalid-refresh", Arg.Any<CancellationToken>())
            .Returns(Result<string>.Failure("Не удалось обновить токен", ErrorType.Unauthorized));
 
        // Act
        var count = await _sut.SyncUserAsync(userId);
 
        // Assert
        Assert.Equal(0, count);
 
        await _bankingApi.DidNotReceive().GetAccountsAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
 