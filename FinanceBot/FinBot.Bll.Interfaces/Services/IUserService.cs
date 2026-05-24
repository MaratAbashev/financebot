using FinBot.Domain.Models;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Services;

public interface IUserService
{
    Task<Result<User>> CreateUserAsync(long tgId, string displayName);
    Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName);
    Task<Result<User>> GetUserByGuidIdAsync(Guid userId);
    Task<Result<User>> GetUserByTgIdAsync(long userId);
}