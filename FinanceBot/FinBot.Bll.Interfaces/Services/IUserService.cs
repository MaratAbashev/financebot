using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IUserService
{
    // Task<Result<User?>> GetUserByTgIdAsync(long id);
    // Task<Result<User?>> GetUserByGuidIdAsync(Guid id);
    Task<Result<User>> CreateUserAsync(long tgId, string displayName);
    Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName);
    Task<Result<decimal>> AddExpenseAsync(
        User user,
        Guid groupId,
        decimal amount,
        ExpenseCategory category);

    Task<Result<User?>> GetUserByGuidIdAsync(Guid id);
    Task<Result<User?>> GetUserByTgIdAsync(long id);
}