using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinBot.Bll.Implementation.Services;

public class InternalBankService(
    PDbContext dbContext,
    ILogger<GroupService> logger) : IInternalBankService
{
    public async Task<Result<bool>> IsBankConnectedAsync(long userTgId, CancellationToken ct)
    {
        try
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.TelegramId == userTgId, cancellationToken: ct);
            if (user is null)
            {
                logger.LogError("User with tgId {userTgId} does not exist", userTgId);
                return Result<bool>.Failure("User not exist", ErrorType.NotFound);
            }

            return dbContext.BankConnections
                .FirstOrDefault(x => x.UserId == user.Id && x.IsActive == true) is not null
                ? Result<bool>.Success(true)
                : Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Something went wrong during check for user connection: {errorMessage}\nErrorStack{errorStack}",
                ex.Message, ex.StackTrace);
            return Result<bool>.Failure("Failed to check for user");
        }
    }
}