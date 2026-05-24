using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.ExcelService.Repositories;

public interface IExpenseRepository
{
    Task<Result<List<Expense>>> GetForUserInGroupAsync(long userTgId, Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);

    Task<Result<List<Expense>>> GetForGroupAsync(Guid groupId, DateTime from, DateTime to,
        CancellationToken cancellationToken = default);
}
