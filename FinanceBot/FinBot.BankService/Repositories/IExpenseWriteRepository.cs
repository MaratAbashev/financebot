using FinBot.Domain.Models.Enums;

namespace FinBot.BankService.Repositories;

public interface IExpenseWriteRepository
{
    Task AddAsync(Guid userId, decimal amount, ExpenseCategory category, DateTime date,
        CancellationToken ct = default);
}