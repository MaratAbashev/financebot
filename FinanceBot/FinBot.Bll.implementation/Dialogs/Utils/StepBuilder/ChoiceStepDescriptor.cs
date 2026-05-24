using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;

public sealed class ChoiceStepDescriptor<T>(
    string key,
    string promptTemplate,
    Func<DialogContext, IEnumerable<(string ButtonName, T ButtonValue)>> buttonMapper,
    DialogBuilder builder)
    where T : IConvertible
{
    private Func<DialogContext, Task<Result<IEnumerable<string>>>>? _dataLoader;
    private bool _isFirstStep;
    private Func<Result, long, Update, DialogContext, Task>? _onPromptFailed;

    public ChoiceStepDescriptor<T> WithDataLoader(
        Func<DialogContext, Task<Result<IEnumerable<string>>>> dataLoader)
    {
        _dataLoader = dataLoader;
        return this;
    }

    public ChoiceStepDescriptor<T> AsFirstStep()
    {
        _isFirstStep = true;
        return this;
    }

    public ChoiceStepDescriptor<T> OnPromptFailed(
        Func<Result, long, Update, DialogContext, Task> handler)
    {
        _onPromptFailed = handler;
        return this;
    }

    public StepDescriptor Commit()
        => builder.AddStep((next, prev) =>
            new ChoiceStep<T>(key, promptTemplate, next, prev,
                buttonMapper, _dataLoader, _isFirstStep, _onPromptFailed));
}