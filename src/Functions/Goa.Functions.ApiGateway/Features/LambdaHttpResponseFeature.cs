using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Goa.Functions.ApiGateway.Features;

internal sealed class LambdaHttpResponseFeature : IHttpResponseFeature
{
    public int StatusCode { get; set; } = 200;
    public string? ReasonPhrase { get; set; }
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public Stream Body { get; set; } = new MemoryStream();
    public bool HasStarted { get; private set; }

    private readonly List<(Func<object, Task>, object)> _onStartingCallbacks = new();
    private readonly List<(Func<object, Task>, object)> _onCompletedCallbacks = new();

    public void OnStarting(Func<object, Task> callback, object state)
    {
        if (HasStarted)
            throw new InvalidOperationException("Response has already started.");
        _onStartingCallbacks.Add((callback, state));
    }

    public void OnCompleted(Func<object, Task> callback, object state)
    {
        _onCompletedCallbacks.Add((callback, state));
    }

    public async Task ExecuteOnStartingCallbacksAsync()
    {
        foreach (var (callback, state) in _onStartingCallbacks)
            await callback(state);
        HasStarted = true;
    }

    public async Task ExecuteOnCompletedCallbacksAsync()
    {
        foreach (var (callback, state) in _onCompletedCallbacks)
            await callback(state);
    }
}
