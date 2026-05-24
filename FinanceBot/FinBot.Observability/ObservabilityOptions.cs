namespace FinBot.Observability;

public sealed class ObservabilityOptions
{
    public string ServiceName { get; set; } = "unknown-service";
    public string? ServiceVersion { get; set; }
    public string OtlpEndpoint { get; set; } = "http://otel-collector:4317";
    public OtlpProtocol OtlpProtocol { get; set; } = OtlpProtocol.Grpc;
    public bool EnableTracing { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableLogs { get; set; } = true;
    public bool EnableConsoleLog { get; set; } = true;
    public bool EnableSeqLog { get; set; } = true;
    public string? SeqServerUrl { get; set; }
    public bool ExposePrometheusEndpoint { get; set; } = true;
    public int? PrometheusListenerPort { get; set; }
    public double TraceSamplingRatio { get; set; } = 1.0;

    public string[] ExcludedHttpPaths { get; set; } =
        ["/hf", "/health", "/health/ready", "/health/live", "/metrics"];
}

public enum OtlpProtocol
{
    Grpc,
    HttpProtobuf
}