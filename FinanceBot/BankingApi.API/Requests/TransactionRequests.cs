namespace BankingApi.API.Requests;

public record CreateTransactionRequest(
    Guid CategoryId,
    decimal Amount,
    string Type,
    string? Description);