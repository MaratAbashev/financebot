using System.Diagnostics.Metrics;
using FinBot.Observability.Constants;

namespace FinBot.Observability.Metrics;

public static class FinBotMeter
{
    public static readonly Meter Instance = new(ObservabilityConstants.MeterName);
}