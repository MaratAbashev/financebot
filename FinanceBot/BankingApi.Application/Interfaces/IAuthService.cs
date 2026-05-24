using BankingApi.Application.Dto;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces;

public interface IAuthService
{
    Task SendVerificationCodeAsync(string phoneNumber, CancellationToken ct = default);
    Task<AuthResult> VerifyCodeAsync(string phoneNumber, string code, CancellationToken ct = default);
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<User?> GetOrCreateUserAsync(string phoneNumber, string code, CancellationToken ct = default);
}