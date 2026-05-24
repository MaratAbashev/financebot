using System.Diagnostics;
using FinBot.Observability.Constants;

namespace FinBot.Observability.Tracing;

public static class ActivitySources
{
    public static readonly ActivitySource FinBot = new(ObservabilityConstants.ActivitySourceName);
}