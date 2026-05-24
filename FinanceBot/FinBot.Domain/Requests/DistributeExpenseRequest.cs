using FinBot.Domain.Models.Enums;

namespace FinBot.Domain.Requests;

public record DistributeExpenseRequest(int ExpenseId, Guid? GroupId, ExpenseCategory? Category, bool Reject = false);