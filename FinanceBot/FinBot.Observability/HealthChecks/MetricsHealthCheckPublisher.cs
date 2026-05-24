using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FinBot.Observability.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinBot.Observability.HealthChecks;

internal sealed class MetricsHealthCheckPublisher : IHealthCheckPublisher
{
    private const string OverallCheck = "overall";

    private readonly ConcurrentDictionary<string, double> _statuses = new();

    public MetricsHealthCheckPublisher()
    {
        FinBotMeter.Instance.CreateObservableGauge(
            name: "finbot.health.status",
            observeValues: Observe,
            unit: "{status}",
            description: "Статус health-чеков: 1=healthy, 0.5=degraded, 0=unhealthy. Тег check.");
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _statuses[OverallCheck] = ToValue(report.Status);

        foreach (var entry in report.Entries)
        {
            _statuses[entry.Key] = ToValue(entry.Value.Status);
        }

        return Task.CompletedTask;
    }

    private IEnumerable<Measurement<double>> Observe()
    {
        foreach (var status in _statuses)
        {
            yield return new Measurement<double>(
                status.Value,
                new KeyValuePair<string, object?>("check", status.Key));
        }
    }

    private static double ToValue(HealthStatus status) => status switch
    {
        HealthStatus.Healthy => 1,
        HealthStatus.Degraded => 0.5,
        _ => 0
    };
}
