using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class UserService(
    IGenericRepository<User, Guid, PDbContext> userRepository,
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<User?>> GetUserAsync(Predicate<User> predicate)
    {
        try
        {
            return Result<User?>.Success(await userRepository.FirstOrDefaultAsync(u => predicate(u)));
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during get user: {errorMessage}", ex.Message);
            return Result<User?>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> CreateUserAsync(long tgId, string displayName)
    {
        try
        {
            var isExist = await userRepository.AnyAsync(u => u.TelegramId == tgId);
            if (!isExist)
            {
                return Result<User>.Failure("User with such id already exists", ErrorType.Conflict);
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = tgId,
                DisplayName = displayName
            };
            
            await userRepository.AddAsync(newUser);
            await userRepository.SaveChangesAsync();

            return Result<User>.Success(newUser);
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during create user: {errorMessage}", ex.Message);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> GetOrCreateUserAsync(long tgId, string displayName)
    {
        try
        {
            var existedUser = await userRepository.FirstOrDefaultAsync(u => tgId == u.TelegramId);
            if (existedUser != null)
            {
                return Result<User>.Success(existedUser);
            }
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                TelegramId = tgId,
                DisplayName = displayName
            };
            
            await userRepository.AddAsync(newUser);
            await userRepository.SaveChangesAsync();

            return Result<User>.Success(newUser);
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during get or create user: {errorMessage}", ex.Message);
            return Result<User>.Failure(ex.Message);
        }
    }
}