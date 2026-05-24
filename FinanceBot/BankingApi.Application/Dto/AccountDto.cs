namespace BankingApi.Application.Dto;

public record AccountDto(
    Guid Id,
    string Name,
    decimal Balance,
    string Currency,
    DateTime CreatedAt);