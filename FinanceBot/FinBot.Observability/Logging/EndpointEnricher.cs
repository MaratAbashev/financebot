using FinBot.Observability.Constants;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace FinBot.Observability.Logging;

internal sealed class EndpointEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EndpointEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var endpoint = _httpContextAccessor.HttpContext?.GetEndpoint()?.DisplayName;

        if (string.IsNullOrEmpty(endpoint))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(new LogEventProperty(
            ObservabilityConstants.LogProperties.Endpoint,
            new ScalarValue(endpoint)));
    }
}