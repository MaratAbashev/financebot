using System.Net.Http.Headers;
using FinBot.Domain.Utils;

namespace FinBot.BankService.BankingApi;

public class BankingApiClient(HttpClient http) : IBankingApiClient
{
    public async Task<Result<string>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("/api/auth/refresh",
            new { refreshToken }, ct);

        if (!response.IsSuccessStatusCode)
            return Result<string>.Failure("Не удалось обновить токен", ErrorType.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(ct);
        return Result<string>.Success(result!.AccessToken);
    }

    public async Task<Result<List<TransactionDto>>> GetTransactionsAsync(
        Guid accountId, string accessToken, CancellationToken ct = default)
    {
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.GetAsync($"/api/accounts/{accountId}/transactions", ct);

        if (!response.IsSuccessStatusCode)
            return Result<List<TransactionDto>>.Failure("Ошибка получения транзакций");

        var transactions = await response.Content
            .ReadFromJsonAsync<List<TransactionDto>>(ct);

        return Result<List<TransactionDto>>.Success(transactions!);
    }

    public async Task<Result<List<AccountDto>>> GetAccountsAsync(
        string accessToken, CancellationToken ct = default)
    {
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.GetAsync("/api/accounts", ct);

        if (!response.IsSuccessStatusCode)
            return Result<List<AccountDto>>.Failure("Ошибка получения счетов");

        var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>(ct);
        return Result<List<AccountDto>>.Success(accounts!);
    }
}

public record RefreshTokenResponse(string AccessToken, string RefreshToken);
public record TransactionDto(Guid Id, decimal Amount, string Type, string CategoryName, string? Description, DateTime CreatedAt);
public record AccountDto(Guid Id, string Name, decimal Balance, string Currency, DateTime CreatedAt);