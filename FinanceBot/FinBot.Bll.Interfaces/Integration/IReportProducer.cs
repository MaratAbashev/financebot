using FinBot.Domain.Events;
using FinBot.Domain.Utils;

namespace FinBot.Bll.Interfaces.Integration;

public interface IReportProducer
{
    Task<Result> QueueReportGenerationAsync(ReportGenerationEvent reportEvent);
}