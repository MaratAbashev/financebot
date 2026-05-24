using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SerilogOtlpProtocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol;

namespace FinBot.Observability.Logging;

internal static class SerilogConfiguration
{
    public static void Configure(
        LoggerConfiguration loggerConfiguration,
        IConfiguration appConfiguration,
        IServiceProvider services,
        ObservabilityOptions options)
    {
        loggerConfiguration
            .ReadFrom.Configuration(appConfiguration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.With(new FacilityEnricher(options.ServiceName));

        var endpointEnricher = services.GetService<EndpointEnricher>();
        if (endpointEnricher is not null)
        {
            loggerConfiguration.Enrich.With(endpointEnricher);
        }

        loggerConfiguration.Filter.ByExcluding(logEvent =>
            logEvent.Properties.TryGetValue("RequestPath", out var requestPath)
            && requestPath.ToString().Contains("/hf"));

        var formatter = new FinBotLogFormatter();

        if (options.EnableConsoleLog)
        {
            loggerConfiguration.WriteTo.Console(formatter);
        }

        if (options.EnableSeqLog && !string.IsNullOrWhiteSpace(options.SeqServerUrl))
        {
            loggerConfiguration.WriteTo.Seq(options.SeqServerUrl);
        }

        if (options.EnableLogs)
        {
            loggerConfiguration.WriteTo.OpenTelemetry(otel =>
            {
                otel.Endpoint = options.OtlpEndpoint;
                otel.Protocol = options.OtlpProtocol == OtlpProtocol.Grpc
                    ? SerilogOtlpProtocol.Grpc
                    : SerilogOtlpProtocol.HttpProtobuf;
                otel.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = options.ServiceName,
                    ["service.version"] = options.ServiceVersion ?? "0.0.0"
                };
            });
        }
    }
}