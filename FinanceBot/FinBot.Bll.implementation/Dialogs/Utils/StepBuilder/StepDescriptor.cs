using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Domain.Models;

namespace FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;

public sealed class StepDescriptor(
    int id,
    Func<Func<DialogContext, int>, Func<DialogContext, int>, DataStep> factory)
{
    public int Id { get; } = id;
    internal Func<Func<DialogContext, int>, Func<DialogContext, int>, DataStep> Factory { get; } = factory;

    internal Func<DialogContext, int>? CustomPrev { get; set; }
    internal Func<DialogContext, int>? CustomNext { get; set; }
}