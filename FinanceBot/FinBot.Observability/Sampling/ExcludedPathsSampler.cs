using OpenTelemetry.Trace;

namespace FinBot.Observability.Sampling;

internal sealed class ExcludedPathsSampler : Sampler
{
    private const string HttpRouteKey = "http.route";
    private const string UrlPathKey = "url.path";
    private const string HttpTargetKey = "http.target";

    private readonly Sampler _inner;
    private readonly string[] _excludedPaths;

    public ExcludedPathsSampler(Sampler inner, string[] excludedPaths)
    {
        _inner = inner;
        _excludedPaths = excludedPaths;
        Description = $"ExcludedPathsSampler({inner.Description})";
    }

    public override SamplingResult ShouldSample(in SamplingParameters parameters)
    {
        foreach (var tag in parameters.Tags ?? [])
        {
            if (tag.Key is not (HttpRouteKey or UrlPathKey or HttpTargetKey))
            {
                continue;
            }

            if (tag.Value is not string path)
            {
                continue;
            }

            foreach (var excluded in _excludedPaths)
            {
                if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                {
                    return new SamplingResult(SamplingDecision.Drop);
                }
            }
        }

        return _inner.ShouldSample(parameters);
    }
}