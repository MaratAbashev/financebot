using FinBot.Observability.Constants;
using OpenTelemetry.Metrics;

namespace FinBot.Observability.Metrics;

internal static class MetricsBuilderExtensions
{
    public static MeterProviderBuilder AddFinBotMetrics(
        this MeterProviderBuilder builder,
        ObservabilityOptions options)
    {
        builder
            .AddMeter(ObservabilityConstants.MeterName)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation();

        if (options.ExposePrometheusEndpoint)
        {
            builder.AddPrometheusExporter();
        }

        builder.AddOtlpExporter((exporter, reader) =>
        {
            exporter.Endpoint = new Uri(options.OtlpEndpoint);
            exporter.Protocol = options.OtlpProtocol == OtlpProtocol.Grpc
                ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10_000;
        });

        return builder;
    }
}