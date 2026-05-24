using FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;
using FinBot.Domain.Models;

namespace FinBot.Bll.Implementation.Dialogs.Utils;

public static class DialogBuilderExtensions
{
    extension(DialogBuilder builder)
    {
        public TextStepDescriptor<T> AddTextStep<T>(string key, string promptTemplate)
            where T : IConvertible
            => new(key, promptTemplate, builder);

        public ChoiceStepDescriptor<T> AddChoiceStep<T>(
            string key,
            string promptTemplate,
            Func<DialogContext, IEnumerable<(string ButtonName, T ButtonValue)>> buttonMapper)
            where T : IConvertible
            => new(key, promptTemplate, buttonMapper, builder);
    }

    extension(StepDescriptor descriptor)
    {
        public StepDescriptor Back(Func<DialogContext, int> prevStepId)
        {
            descriptor.CustomPrev = prevStepId;
            return descriptor;
        }

        public StepDescriptor Back(StepDescriptor target)
            => descriptor.Back(_ => target.Id);

        public StepDescriptor GoTo(Func<DialogContext, int> nextStepId)
        {
            descriptor.CustomNext = nextStepId;
            return descriptor;
        }

        public StepDescriptor GoTo(StepDescriptor target)
            => descriptor.GoTo(_ => target.Id);
    }
}