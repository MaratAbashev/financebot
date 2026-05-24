using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Requests;

public record AddExpenseRequest(Guid GroupId, decimal Amount, ExpenseCategory Category);