using BankingApi.Application.Dto;
using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces;

public interface IOAuthService
{
    Task<InitiateOAuthResult> InitiateAsync(
        string redirectUri,
        string state,
        CancellationToken ct = default);

    Task CompleteAsync(
        string authCode,
        User user,
        CancellationToken ct = default);
}