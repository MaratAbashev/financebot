using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IUserService
{
    Task<Result<User?>> GetUserAsync(Predicate<User> predicate);
    Task<Result<User>> CreateUserAsync(long tgId, string displayName);
    Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName);
}