using BankingApi.Application.Dto;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Settings;
using BankingApi.Domain.Entities;
using BankingApi.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BankingApi.Infrastructure.Services;

public class AuthService(
    AppDbContext context,
    ISmsService smsService,
    IJwtService jwtService,
    IOptions<JwtSettings> jwtSettings,
    IOptions<VerificationCodeSettings> codeSettings,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly VerificationCodeSettings _codeSettings = codeSettings.Value;
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task SendVerificationCodeAsync(string phoneNumber, CancellationToken ct = default)
    {
        var oldCodes = await context.PhoneVerificationCodes
            .Where(c => c.PhoneNumber == phoneNumber && !c.IsUsed)
            .ToListAsync(ct);

        foreach (var old in oldCodes)
            old.IsUsed = true;

        var code = GenerateCode();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        context.PhoneVerificationCodes.Add(new PhoneVerificationCode
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            Code = code,
            ExpiresAt = now.AddMinutes(_codeSettings.ExpiryMinutes),
            IsUsed = false
        });

        await context.SaveChangesAsync(ct);

        await smsService.SendAsync(phoneNumber, $"Ваш код подтверждения: {code}", ct);
        logger.LogInformation("Verification code sent to {Phone}", phoneNumber);
    }
    
    public async Task<AuthResult> VerifyCodeAsync(string phoneNumber, string code, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var verification = await context.PhoneVerificationCodes
            .Where(c => c.PhoneNumber == phoneNumber
                     && c.Code == code
                     && !c.IsUsed
                     && c.ExpiresAt > now)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (verification is null)
            return AuthResult.Failure("Неверный или истёкший код");

        verification.IsUsed = true;

        var user = await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                PhoneNumber = phoneNumber,
                CreatedAt = now
            };
            context.Users.Add(user);
        }

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, now, ct);

        await context.SaveChangesAsync(ct);

        return AuthResult.Success(accessToken, refreshToken);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var storedToken = await context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt <= now)
            return AuthResult.Failure("Refresh token недействителен");

        storedToken.IsRevoked = true;

        var accessToken = jwtService.GenerateAccessToken(storedToken.User);
        var newRefreshToken = await CreateRefreshTokenAsync(storedToken.UserId, now, ct);

        await context.SaveChangesAsync(ct);

        return AuthResult.Success(accessToken, newRefreshToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var storedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct);

        if (storedToken is not null)
        {
            storedToken.IsRevoked = true;
            await context.SaveChangesAsync(ct);
        }
    }
    
    public async Task<User?> GetOrCreateUserAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var verification = await context.PhoneVerificationCodes
            .Where(c => c.PhoneNumber == phoneNumber
                        && c.Code == code
                        && !c.IsUsed
                        && c.ExpiresAt > now)
            .OrderByDescending(c => c.ExpiresAt)
            .FirstOrDefaultAsync(ct);

        if (verification is null)
            return null;

        verification.IsUsed = true;

        var user = await context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, ct);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                PhoneNumber = phoneNumber,
                CreatedAt = now
            };
            context.Users.Add(user);
        }

        await context.SaveChangesAsync(ct);
        return user;
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        var token = jwtService.GenerateRefreshToken();

        await context.RefreshTokens.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpiryDays),
            IsRevoked = false
        }, ct);

        return token;
    }

    private string GenerateCode()
    {
        var length = _codeSettings.Length;
        return Random.Shared.Next(0, (int)Math.Pow(10, length))
                     .ToString()
                     .PadLeft(length, '0');
    }
}