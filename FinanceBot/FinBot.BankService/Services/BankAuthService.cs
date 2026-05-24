using FinBot.BankService.BankingApi;
using FinBot.BankService.Cache;
using FinBot.BankService.Models;
using FinBot.BankService.Repositories;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Options;

namespace FinBot.BankService.Services;

public class BankAuthService(
    IBankConnectionRepository connections,
    ITokenCache tokenCache,
    IHttpClientFactory httpClientFactory,
    IOptions<BankingApiOptions> bankingApiOptions,
    TimeProvider timeProvider,
    ILogger<BankAuthService> logger) : IBankAuthService
{
    private readonly BankingApiOptions _options = bankingApiOptions.Value;

    public async Task<Result<string>> GetAuthUrlAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await connections.GetByUserIdAsync(userId, ct);
        if (connection is { IsActive: true })
        {
            return Result<string>.Failure("Пользователь уже привязал банковский аккаунт", ErrorType.BadRequest);
        }
        
        var redirectUri = $"{_options.RedirectUrl}/oauth/callback";
        var state = userId.ToString();

        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            $"{_options.BaseUrl}/oauth/authorize?redirectUri={Uri.EscapeDataString(redirectUri)}&state={state}", ct);

        if (!response.IsSuccessStatusCode)
            return Result<string>.Failure("Не удалось получить ссылку авторизации");

        var result = await response.Content.ReadFromJsonAsync<AuthUrlResponse>(ct);
        return Result<string>.Success(result!.AuthUrl);
    }

    public async Task HandleCallbackAsync(OAuthCallbackPayload payload, CancellationToken ct = default)
    {
        if (!Guid.TryParse(payload.State, out var userId))
        {
            logger.LogWarning("Невалидный state в OAuth callback: {State}", payload.State);
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        await tokenCache.SetAsync(userId, payload.AccessToken, TimeSpan.FromMinutes(15));

        var existing = await connections.GetByUserIdAsync(userId, ct);
        if (existing is null)
        {
            await connections.AddAsync(new BankConnection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BankingApiBaseUrl = _options.BaseUrl,
                RefreshToken = payload.RefreshToken,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }, ct);
        }
        else
        {
            await connections.UpdateRefreshTokenAsync(userId, payload.RefreshToken, ct);
        }

        logger.LogInformation("Банк успешно привязан для UserId={UserId}", userId);
    }

    public async Task UnlinkBankAsync(Guid userId, CancellationToken ct = default)
    {
        var connection = await connections.GetByUserIdAsync(userId, ct);

        if (connection != null)
        {
            if (connection.IsActive == false)
            {
                throw new ArgumentException("Пользователь уже отвязал банковский аккаунт");
            }
            
            connection.IsActive = false;
            await connections.UpdateAsync(connection, ct);
        }
    }
}

public record AuthUrlResponse(string AuthUrl);