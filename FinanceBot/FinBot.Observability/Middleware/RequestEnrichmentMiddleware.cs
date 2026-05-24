using System.Diagnostics;
using FinBot.Observability.Constants;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace FinBot.Observability.Middleware;

internal sealed class RequestEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public RequestEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint()?.DisplayName
                       ?? context.Request.Path.Value
                       ?? string.Empty;

        var traceId = Activity.Current?.TraceId.ToString();

        using (LogContext.PushProperty(ObservabilityConstants.LogProperties.Endpoint, endpoint))
        using (LogContext.PushProperty(ObservabilityConstants.LogProperties.TraceId, traceId))
        {
            await _next(context);
        }
    }
}