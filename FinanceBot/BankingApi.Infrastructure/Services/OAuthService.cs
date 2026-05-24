using System.Net.Http.Json;
using System.Security.Cryptography;
using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Settings;
using BankingApi.Domain.Entities;
using BankingApi.Domain.Enums;
using BankingApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankingApi.Infrastructure.Services;

public class OAuthService(
    AppDbContext context,
    IJwtService jwtService,
    IOptions<OAuthSettings> settings,
    IHttpClientFactory httpClientFactory,
    TimeProvider timeProvider,
    ILogger<OAuthService> logger) : IOAuthService
{
    private readonly OAuthSettings _settings = settings.Value;

    public async Task<InitiateOAuthResult> InitiateAsync(
        string redirectUri,
        string state,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var authCode = GenerateAuthCode();

        context.OAuthSessions.Add(new OAuthSession
        {
            Id = Guid.NewGuid(),
            AuthCode = authCode,
            RedirectUri = redirectUri,
            State = state,
            UserId = null,
            Status = OAuthSessionStatus.Pending,
            ExpiresAt = now.AddMinutes(_settings.SessionExpiryMinutes),
            CreatedAt = now
        });

        await context.SaveChangesAsync(ct);

        var authUrl = $"{_settings.BaseUrl}/oauth/login?code={authCode}";
        return new InitiateOAuthResult(authUrl);
    }

    public async Task CompleteAsync(
        string authCode,
        User user,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var session = await context.OAuthSessions
            .FirstOrDefaultAsync(s => s.AuthCode == authCode
                                   && s.Status == OAuthSessionStatus.Pending
                                   && s.ExpiresAt > now, ct)
            ?? throw new KeyNotFoundException("Сессия не найдена или истекла");

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = now,
            ExpiresAt = now.AddDays(30),
            IsRevoked = false
        });

        session.UserId = user.Id;
        session.Status = OAuthSessionStatus.Completed;

        await context.SaveChangesAsync(ct);

        await SendTokensToServiceAsync(session.RedirectUri, new OAuthCallbackPayload(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            State: session.State), ct);
    }

    private async Task SendTokensToServiceAsync(
        string redirectUri,
        OAuthCallbackPayload payload,
        CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsJsonAsync(redirectUri, payload, ct);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Токены успешно отправлены на {RedirectUri}", redirectUri);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отправки токенов на {RedirectUri}", redirectUri);
            throw;
        }
    }

    private static string GenerateAuthCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}