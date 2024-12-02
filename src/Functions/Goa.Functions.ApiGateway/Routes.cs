namespace Goa.Functions.ApiGateway;

internal sealed class Routes
{
    private readonly RouteEndpoint _root = new();

    public void Add(string template, string httpVerb, List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> pipeline)
    {
        if (string.IsNullOrWhiteSpace(template))
            throw new ArgumentException("Route template cannot be null or whitespace.", nameof(template));
        if (string.IsNullOrWhiteSpace(httpVerb))
            throw new ArgumentException("HTTP verb cannot be null or whitespace.", nameof(httpVerb));
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        var segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = _root;

        foreach (var segment in segments)
        {
            current = current.GetOrAddChild(segment);
        }

        current.IsTerminal = true;
        current.AddPipeline(httpVerb, pipeline);
    }

    public bool TryMatch(string requestPath, string httpVerb, out IReadOnlyDictionary<string, string> routeValues, out List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> pipeline)
    {
        routeValues = new Dictionary<string, string>();
        pipeline = new List<Func<InvocationContext, Func<Task>, CancellationToken, Task>>();

        if (string.IsNullOrWhiteSpace(requestPath) || string.IsNullOrWhiteSpace(httpVerb))
            return false;

        var segments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var current = _root;

        foreach (var segment in segments)
        {
            if (current.Children.TryGetValue(segment, out var next))
            {
                current = next; // Exact match
            }
            else if (current.PlaceholderChild != null)
            {
                // Match wildcard
                ((Dictionary<string, string>)routeValues)[current.PlaceholderName] = segment;
                current = current.PlaceholderChild;
            }
            else
            {
                return false; // No match
            }
        }

        if (current.IsTerminal && current.TryGetPipeline(httpVerb, out var matchedPipeline))
        {
            pipeline = matchedPipeline;
            return true;
        }

        return false;
    }
}
