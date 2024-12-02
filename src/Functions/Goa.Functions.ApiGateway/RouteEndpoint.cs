using System.Diagnostics.CodeAnalysis;

namespace Goa.Functions.ApiGateway;

internal sealed class RouteEndpoint
{
    public Dictionary<string, RouteEndpoint> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
    public RouteEndpoint? PlaceholderChild { get; private set; }
    public string PlaceholderName { get; private set; } = string.Empty;
    public bool IsTerminal { get; set; }

    private readonly Dictionary<string, List<Func<InvocationContext, Func<Task>, CancellationToken, Task>>> _pipelines = new(StringComparer.OrdinalIgnoreCase);

    public void AddPipeline(string httpVerb, List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> pipeline)
    {
        _pipelines[httpVerb] = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public bool TryGetPipeline(string httpVerb, [NotNullWhen(true)] out List<Func<InvocationContext, Func<Task>, CancellationToken, Task>>? pipeline)
    {
        return _pipelines.TryGetValue(httpVerb, out pipeline);
    }

    public RouteEndpoint GetOrAddChild(string segment)
    {
        if (segment.StartsWith('{') && segment.EndsWith('}'))
        {
            PlaceholderName = segment[1..^1];
            return PlaceholderChild ??= new RouteEndpoint();
        }

        if (!Children.TryGetValue(segment, out var child))
        {
            child = new RouteEndpoint();
            Children[segment] = child;
        }

        return child;
    }
}
