using FinBot.Observability.Constants;
using Serilog.Core;
using Serilog.Events;

namespace FinBot.Observability.Logging;

internal sealed class FacilityEnricher : ILogEventEnricher
{
    private readonly LogEventProperty _facility;
    private readonly LogEventProperty _machineName;
    private readonly LogEventProperty _uniqueIdFacility;

    public FacilityEnricher(string serviceName)
    {
        _facility = new LogEventProperty(
            ObservabilityConstants.LogProperties.Facility,
            new ScalarValue(serviceName));

        _machineName = new LogEventProperty(
            ObservabilityConstants.LogProperties.MachineName,
            new ScalarValue(Environment.MachineName));

        var instanceId = Environment.GetEnvironmentVariable("HOSTNAME")
                         ?? Environment.GetEnvironmentVariable("POD_NAME")
                         ?? Environment.MachineName;
        _uniqueIdFacility = new LogEventProperty(
            ObservabilityConstants.LogProperties.UniqueIdFacility,
            new ScalarValue(instanceId));
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(_facility);
        logEvent.AddPropertyIfAbsent(_machineName);
        logEvent.AddPropertyIfAbsent(_uniqueIdFacility);
    }
}