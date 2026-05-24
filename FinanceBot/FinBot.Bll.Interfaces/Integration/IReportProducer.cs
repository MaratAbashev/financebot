using FinBot.Domain.Events;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

[Obsolete("Будет выпилено и заменено")]
public interface IReportProducer
{
    Task<Result> QueueReportGenerationAsync(ReportGenerationEvent reportEvent);
}