using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Utils;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class UserService(
    IGenericRepository<User, Guid, PDbContext> userRepository,
    IGenericRepository<Account, int, PDbContext> accountRepository,
    IGenericRepository<Expense, int, PDbContext> expensesRepository,
    
    ILogger<UserService> logger) : IUserService
{
    public async Task<Result<User?>> GetUserByTgIdAsync(long id)
    {
        try
        {
            return Result<User?>.Success(await userRepository.FirstOrDefaultAsync(u => u.TelegramId == id));
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during get user: {errorMessage}\nErrorStack{errorStack}", ex.Message, ex.StackTrace);
            return Result<User?>.Failure(ex.Message);
        }
    }

    public async Task<Result<User?>> GetUserByGuidIdAsync(Guid id)
    {
        try
        {
            return Result<User?>.Success(await userRepository.FirstOrDefaultAsync(u => u.Id == id));
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during get user: {errorMessage}\nErrorStack{errorStack}", ex.Message, ex.StackTrace);
            return Result<User?>.Failure(ex.Message);
        }
    }

    public async Task<Result<User>> CreateUserAsync(long tgId, string displayName)
    {
        try
        {
            var isExist = await userRepository.AnyAsync(u => u.TelegramId == tgId);
            if (isExist)
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
            logger.LogError("Something went wrong during create user: {errorMessage}\nErrorStack{errorStack}", ex.Message, ex.StackTrace);
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
            logger.LogError("Something went wrong during get or create user: {errorMessage}\nErrorStack{errorStack}", ex.Message, ex.StackTrace);
            return Result<User>.Failure(ex.Message);
        }
    }

    public async Task<Result<decimal>> AddExpenseAsync(User user, Guid groupId, decimal amount, ExpenseCategory category)
    {
        try
        {
            var account = user.Accounts.First(a => a.GroupId == groupId);
            var newExpense = new Expense
            {
                Category = category,
                Amount = amount,
                Date = DateTime.Now.ToUniversalTime(),
                AccountId = account.Id,
            };

            account.Balance -= amount;
            account.Expenses.Add(newExpense);

            await expensesRepository.AddAsync(newExpense);
            accountRepository.Update(account);
            await accountRepository.SaveChangesAsync();
            
            return Result<decimal>.Success(account.Balance);
        }
        catch (Exception ex)
        {
            logger.LogError("Something went wrong during add expense: {errorMessage}\nErrorStack{errorStack}", ex.Message, ex.StackTrace);
            return Result<decimal>.Failure(ex.Message);
        }
        
    }
}