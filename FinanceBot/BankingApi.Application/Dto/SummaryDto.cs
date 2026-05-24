namespace BankingApi.Application.Dto;

public record SummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    IEnumerable<CategorySummaryDto> ByCategory);

public record CategorySummaryDto(
    string CategoryName,
    decimal Total);