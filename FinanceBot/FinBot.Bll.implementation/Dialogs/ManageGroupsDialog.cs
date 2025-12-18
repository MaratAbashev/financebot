using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Interfaces;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Bll.Interfaces.Services;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Telegram.Bot.Types;
using User = FinBot.Domain.Models.User;

namespace FinBot.Bll.Implementation.Dialogs;

public class ManageGroupsDialog(
    IUserService userService,
    IGenericRepository<User, Guid, PDbContext> userRepository) : IDialogDefinition
{
    public string DialogName { get; } = "ManageGroupsDialog";

    public IReadOnlyDictionary<int, IStep> Steps { get; } = new Dictionary<int, IStep>
    {
        {
            0, 
            new ChoiceStep<string>(
                "choiceGroup", 
                "Выберите группу", 
                _ => 1, 
                _ => -1,
                async ctx=>
                {
                    var user = await userService.GetUserByTgIdAsync(ctx.UserId);
                    
                    ctx.DialogStorage!["time"] = DateTime.Now.ToShortTimeString();
                    return Result<IEnumerable<string>>.Success(["time"]);
                },
                true)
        },
    };
    public async Task OnCompletedAsync(long chatId, DialogContext dialogContext, Update update, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}