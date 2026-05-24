namespace BankingApi.Application.Dto;

public record TransactionDto(
    Guid Id,
    decimal Amount,
    string Type,
    string CategoryName,
    string? Description,
    DateTime CreatedAt);