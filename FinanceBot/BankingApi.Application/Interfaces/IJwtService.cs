using BankingApi.Domain.Entities;

namespace BankingApi.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}