using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Domain.Models;
using FinBot.Domain.Utils;
using Telegram.Bot.Types;

namespace FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;

public sealed class TextStepDescriptor<T>(string key, string promptTemplate, DialogBuilder builder)
    where T : IConvertible
{
    private Func<DialogContext, Task<Result<IEnumerable<string>>>>? _dataLoader;
    private bool _isFirstStep;
    private Func<T, Result>? _validate;
    private Func<Result, long, Update, DialogContext, Task>? _onPromptFailed;

    public TextStepDescriptor<T> WithDataLoader(
        Func<DialogContext, Task<Result<IEnumerable<string>>>> dataLoader)
    {
        _dataLoader = dataLoader;
        return this;
    }

    public TextStepDescriptor<T> AsFirstStep()
    {
        _isFirstStep = true;
        return this;
    }

    public TextStepDescriptor<T> WithValidation(Func<T, Result> validate)
    {
        _validate = validate;
        return this;
    }

    public TextStepDescriptor<T> OnPromptFailed(
        Func<Result, long, Update, DialogContext, Task> handler)
    {
        _onPromptFailed = handler;
        return this;
    }

    public StepDescriptor Commit()
        => builder.AddStep((next, prev) =>
            new TextStep<T>(key, promptTemplate, next, prev,
                _dataLoader, _isFirstStep, _validate, _onPromptFailed));
}