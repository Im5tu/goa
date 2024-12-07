using Goa.Core;
using Goa.Functions.Core.Bootstrapping;
using Goa.Functions.Core.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.ApiGateway.AspNetCore;

#pragma warning disable CS1591
public abstract class LambdaServer<TRequest, TResponse> : IServer, IAsyncDisposable
{
    private readonly JsonLogger _jsonLogger = new("LambdaServer", LogLevel.Information, jsonSerializerContext: LoggingSerializationContext.Default);
    private readonly LambdaBootstrapper<TRequest, TResponse> _bootstrapper;
    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    public IFeatureCollection Features { get; } = new FeatureCollection(); // We don't have any global features like IHttpUpgradeFeature so we can leave an empty set here

    protected LambdaServer(LambdaBootstrapper<TRequest, TResponse> bootstrapper)
    {
        _bootstrapper = bootstrapper;
    }

    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        _cts ??= new CancellationTokenSource();
        _runningTask ??= EnsureBootstrapper(application).RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    protected abstract IFeatureCollection GetPerRequestFeatureCollection(TRequest request, InvocationRequest invocationRequest, CancellationToken cancellationToken);
    protected abstract TResponse ProcessResponse(IFeatureCollection features);

    protected static (byte[] Content, bool IsBase64Encoded) ReadResponseBody(Stream stream)
    {
        // Optimize for MemoryStream
        if (stream is MemoryStream memoryStream)
        {
            var content = memoryStream.TryGetBuffer(out var buffer)
                ? buffer.Array!.AsSpan(buffer.Offset, buffer.Count).ToArray()
                : memoryStream.ToArray();

            return (content, IsBinary(content));
        }

        // Fallback for non-MemoryStream
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var fallbackContent = memory.ToArray();
        return (fallbackContent, IsBinary(fallbackContent));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = Interlocked.Exchange(ref _cts, null);
            using var task = Interlocked.Exchange(ref _runningTask, null);

            await (cts?.CancelAsync() ?? Task.CompletedTask);
            task?.WaitAsync(cancellationToken);
        }
        catch
        {
            // TODO :: Consider logging
        }
    }

    private LambdaBootstrapper<TRequest, TResponse> EnsureBootstrapper<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        _bootstrapper.OnNext((request, ir, ct) => OnNextAsync(request, application, ir, ct));
        return _bootstrapper;
    }

    private async Task<TResponse> OnNextAsync<TContext>(TRequest request, IHttpApplication<TContext> application, InvocationRequest invocationRequest, CancellationToken cancellationToken)  where TContext : notnull
    {
        var features = GetPerRequestFeatureCollection(request, invocationRequest, cancellationToken);
        var context = application.CreateContext(features);
        TResponse? response;
        Exception? exception = null;
        try
        {
            await application.ProcessRequestAsync(context);
        }
        catch (Exception ex)
        {
            exception = ex;
            //_jsonLogger.LogError(ex.Message);
        }
        finally
        {
            response = ProcessResponse(features);
            // Dispose our features if needed
            foreach (var feature in features)
            {
                if (feature.Value is IDisposable disposable)
                    disposable.Dispose();
            }
            application.DisposeContext(context, exception);

            if (exception is not null)
                _jsonLogger.LogException(exception);
        }

        return response;
    }

    private static bool IsBinary(byte[] data)
    {
        // Optimized binary detection: Fast loop with a few checks
        foreach (var b in data)
        {
            if (b < 32 && b != 9 && b != 10 && b != 13) // Check for control characters
            {
                return true; // Non-printable character detected
            }
        }

        return false;
    }

    public void Dispose()
    {
        _runningTask?.Dispose();
        _cts?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_runningTask != null) await CastAndDisposeAsync(_runningTask);
        if (_cts != null) await CastAndDisposeAsync(_cts);

        return;

        static async ValueTask CastAndDisposeAsync(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}
