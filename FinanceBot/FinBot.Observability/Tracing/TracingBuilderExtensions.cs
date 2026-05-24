using FinBot.Observability.Constants;
using FinBot.Observability.Sampling;
using OpenTelemetry.Trace;

namespace FinBot.Observability.Tracing;

internal static class TracingBuilderExtensions
{
    public static TracerProviderBuilder AddFinBotTracing(
        this TracerProviderBuilder builder,
        ObservabilityOptions options)
    {
        builder
            .AddSource(ObservabilityConstants.ActivitySourceName)
            .AddSource("Confluent.Kafka.Extensions.OpenTelemetry")
            .SetSampler(new ExcludedPathsSampler(
                inner: new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)),
                excludedPaths: options.ExcludedHttpPaths))
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                o.Filter = ctx => !IsExcluded(ctx.Request.Path.Value, options.ExcludedHttpPaths);
            })
            .AddHttpClientInstrumentation(o => { o.RecordException = true; })
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation();

        builder.AddOtlpExporter(exporter =>
        {
            exporter.Endpoint = new Uri(options.OtlpEndpoint);
            exporter.Protocol = options.OtlpProtocol == OtlpProtocol.Grpc
                ? OpenTelemetry.Exporter.OtlpExportProtocol.Grpc
                : OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
        });

        return builder;
    }

    private static bool IsExcluded(string? path, string[] excludedPaths)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        foreach (var excluded in excludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}