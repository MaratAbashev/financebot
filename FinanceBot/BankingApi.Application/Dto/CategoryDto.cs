namespace BankingApi.Application.Dto;

public record CategoryDto(
    Guid Id,
    string Name,
    string Type);