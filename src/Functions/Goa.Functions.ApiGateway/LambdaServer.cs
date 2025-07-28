using Goa.Core;
using Goa.Functions.Core.Bootstrapping;
using Goa.Functions.Core.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.ApiGateway;

/// <summary>
/// Represents a generic Lambda-based HTTP server implementation compatible with ASP.NET Core abstractions.
/// </summary>
/// <typeparam name="TRequest">The type of the incoming request object.</typeparam>
/// <typeparam name="TResponse">The type of the response object to be returned.</typeparam>
public abstract class LambdaServer<TRequest, TResponse> : IServer, IAsyncDisposable
{
    private readonly JsonLogger _jsonLogger = new("LambdaServer", LogLevel.Information, jsonSerializerContext: LoggingSerializationContext.Default);
    private readonly LambdaBootstrapper<TRequest, TResponse> _bootstrapper;
    private Task? _runningTask;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Gets the server-wide feature collection. This implementation does not use any global features.
    /// </summary>
    public IFeatureCollection Features { get; } = new FeatureCollection();

    /// <summary>
    /// Initializes a new instance of the <see cref="LambdaServer{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="bootstrapper">The Lambda bootstrapper to drive the request pipeline.</param>
    protected LambdaServer(LambdaBootstrapper<TRequest, TResponse> bootstrapper)
    {
        _bootstrapper = bootstrapper;
    }

    /// <summary>
    /// Starts the Lambda server using the provided HTTP application.
    /// </summary>
    /// <typeparam name="TContext">The context type for the application.</typeparam>
    /// <param name="application">The HTTP application to run.</param>
    /// <param name="cancellationToken">A token to signal cancellation.</param>
    /// <returns>A task representing the server start operation.</returns>
    public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull
    {
        _cts ??= new CancellationTokenSource();
        _runningTask ??= EnsureBootstrapper(application).RunAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the Lambda server.
    /// </summary>
    /// <param name="cancellationToken">A token to signal cancellation.</param>
    /// <returns>A task representing the stop operation.</returns>
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

    /// <summary>
    /// Disposes resources synchronously.
    /// </summary>
    public void Dispose()
    {
        _runningTask?.Dispose();
        _cts?.Dispose();
    }

    /// <summary>
    /// Disposes resources asynchronously.
    /// </summary>
    /// <returns>A ValueTask that completes when disposal is done.</returns>
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

    /// <summary>
    /// Provides request-specific features used to process the current Lambda invocation.
    /// </summary>
    /// <param name="request">The deserialized Lambda request payload.</param>
    /// <param name="invocationRequest">Metadata for the Lambda invocation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="IFeatureCollection"/> for the request.</returns>
    protected abstract IFeatureCollection GetPerRequestFeatureCollection(TRequest request, InvocationRequest invocationRequest, CancellationToken cancellationToken);

    /// <summary>
    /// Transforms the request's feature collection into the final Lambda response.
    /// </summary>
    /// <param name="features">The request's feature collection.</param>
    /// <returns>The response object to be returned from the Lambda function.</returns>
    protected abstract TResponse ProcessResponse(IFeatureCollection features);

    /// <summary>
    /// Reads a response body from a stream and determines whether it's binary.
    /// </summary>
    /// <param name="stream">The stream containing the response body.</param>
    /// <returns>A tuple with the raw content and a flag indicating if it's binary.</returns>
    protected static (byte[] Content, bool IsBase64Encoded) ReadResponseBody(Stream stream)
    {
        if (stream is MemoryStream memoryStream)
        {
            var content = memoryStream.TryGetBuffer(out var buffer)
                ? buffer.Array!.AsSpan(buffer.Offset, buffer.Count).ToArray()
                : memoryStream.ToArray();

            return (content, IsBinary(content));
        }

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var fallbackContent = memory.ToArray();
        return (fallbackContent, IsBinary(fallbackContent));
    }

    private LambdaBootstrapper<TRequest, TResponse> EnsureBootstrapper<TContext>(IHttpApplication<TContext> application) where TContext : notnull
    {
        _bootstrapper.OnNext((request, ir, ct) => OnNextAsync(request, application, ir, ct));
        return _bootstrapper;
    }

    private async Task<TResponse> OnNextAsync<TContext>(TRequest request, IHttpApplication<TContext> application, InvocationRequest invocationRequest, CancellationToken cancellationToken) where TContext : notnull
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
        }
        finally
        {
            response = ProcessResponse(features);

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
        foreach (var b in data)
        {
            if (b < 32 && b != 9 && b != 10 && b != 13)
            {
                return true;
            }
        }

        return false;
    }
}
