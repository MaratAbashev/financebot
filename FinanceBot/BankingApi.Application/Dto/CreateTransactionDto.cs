namespace BankingApi.Application.Dto;

public record CreateTransactionDto(
    Guid CategoryId,
    decimal Amount,
    string Type,
    string? Description);