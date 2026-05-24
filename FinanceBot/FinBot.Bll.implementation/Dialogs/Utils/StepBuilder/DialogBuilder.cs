using FinBot.Bll.Implementation.Dialogs.Steps;
using FinBot.Bll.Interfaces.Dialogs;
using FinBot.Domain.Models;

namespace FinBot.Bll.Implementation.Dialogs.Utils.StepBuilder;

public sealed class DialogBuilder(int startingId = 0)
{
    private readonly List<StepDescriptor> _steps = [];
    private int _nextId = startingId;

    public StepDescriptor AddStep(
        Func<Func<DialogContext, int>, Func<DialogContext, int>, DataStep> factory)
    {
        var d = new StepDescriptor(_nextId++, factory);
        _steps.Add(d);
        return d;
    }

    public Dictionary<int, IStep> Build()
    {
        var result = new Dictionary<int, IStep>();

        for (var i = 0; i < _steps.Count; i++)
        {
            var d = _steps[i];

            var i1 = i;
            var next = d.CustomNext
                       ?? (i < _steps.Count - 1
                           ? new Func<DialogContext, int>(_ => _steps[i1 + 1].Id)
                           : _ => -10);

            var i2 = i;
            var prev = d.CustomPrev
                       ?? (i > 0
                           ? new Func<DialogContext, int>(_ => _steps[i2 - 1].Id)
                           : _ => -10);

            result[d.Id] = d.Factory(next, prev);
        }

        return result;
    }
}