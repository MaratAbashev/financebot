using FinBot.Bll.Interfaces.Services;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Requests;
using FinBot.Domain.Utils;

namespace FinBot.WebApi.HttpClients;

public class HttpExpenseService(HttpClient httpClient) : BaseHttpService(httpClient, "/Expenses"), IExpenseService
{
    public Task<Result<decimal>> AddExpenseAsync(
        long userTgId,
        Guid groupId,
        decimal amount,
        ExpenseCategory category,
        CancellationToken cancellationToken = default) =>
        PostAsync<decimal, AddExpenseRequest>(
            $"/Add?userTgId={userTgId}",
            new AddExpenseRequest(groupId, amount, category),
            cancellationToken);

    public Task<Result<IEnumerable<Expense>>> GetPendingExpensesAsync(
        long userTgId,
        CancellationToken cancellationToken = default) =>
        GetAsync<IEnumerable<Expense>>($"/Pending?userTgId={userTgId}", cancellationToken);

    public Task<Result> DistributeExpensesAsync(long userTgId, int expenseId, Guid? groupId, ExpenseCategory? category,
        bool reject = false, CancellationToken cancellationToken = default)
    {
        var body = new DistributeExpenseRequest(expenseId, groupId, category, reject);

        return PostAsync(
            $"/Pending/Distribute?userTgId={userTgId}",
            body,
            cancellationToken);
    }
}