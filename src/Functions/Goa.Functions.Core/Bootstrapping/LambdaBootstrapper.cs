using Goa.Core;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Bootstraps a lambda function and handles the lifecycle with the AWS Lambda Runtime
/// </summary>
/// <typeparam name="TRequest">The type of request that the function handles</typeparam>
/// <typeparam name="TResponse">The type of response that the function returns</typeparam>
public class LambdaBootstrapper<TRequest, TResponse>
{
    private readonly IResponseSerializer<TResponse> _responseSerializer;
    private readonly ILogger _logger;
    private readonly JsonTypeInfo<TRequest> _requestTypeInfo;
    private Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>>? _onNext;

    /// <summary>
    ///     The current Lambda runtime client
    /// </summary>
    protected ILambdaRuntimeClient LambdaRuntimeClient { get; }

    /// <summary>
    ///     Constructs a new LambdaBootstrapper
    /// </summary>
    /// <param name="jsonSerializerContext">The JsonSerializerContext that knows about the request/response types</param>
    /// <param name="onNext">The method that gets called when there is a new lambda invocation</param>
    /// <param name="responseSerializer">The response serializer that sends the responses back to the lambda runtime</param>
    /// <param name="lambdaRuntimeClient">The implementation of the lambda runtime client</param>
    public LambdaBootstrapper(JsonSerializerContext jsonSerializerContext, Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>>? onNext = null, IResponseSerializer<TResponse>? responseSerializer = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        var logLevel = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("GOA__LOG__LEVEL"), out var level) ? level : LogLevel.Information;

        _logger = new JsonLogger("LambdaBootstrapper", logLevel, LogScopeProvider.Instance, LoggingSerializationContext.Default);
        LambdaRuntimeClient = lambdaRuntimeClient ?? new LambdaRuntimeClient(logLevel);
        _responseSerializer = responseSerializer ?? new JsonResponseSerializer<TResponse>(jsonSerializerContext);
        _requestTypeInfo = jsonSerializerContext.GetTypeInfo(typeof(TRequest)) as JsonTypeInfo<TRequest> ?? throw new Exception("Cannot find serialization information for the type: " + typeof(TRequest).FullName);
        _onNext = onNext;
    }

    /// <summary>
    ///     Sets the callback for onNext when available
    /// </summary>
    /// <param name="onNext">The callback to invoke when a new lambda request comes in</param>
    public void OnNext(Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>>? onNext) => _onNext = onNext;

    /// <summary>
    ///     Runs the Lambda request loop
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that stops the processing of a given Lambda invocation</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Work around for AspNetCore
        await Task.Yield();

        _logger.BootstrapStarted();

        if (_onNext is null)
            throw new Exception("No callback has been configured for the lambda, so we're unable to process the request.");

        while (!cancellationToken.IsCancellationRequested)
        {
            // Get the next event
            var invocationResult = await LambdaRuntimeClient.GetNextInvocationAsync(cancellationToken);
            if (!invocationResult.IsSuccess || invocationResult.Data is null)
            {
                // Skip to the next invocation if fetching failed
                continue;
            }

            // Parsing the deadline with a safer fallback in case the DeadlineMs is null or not present
            var targetTime = DateTimeOffset.FromUnixTimeMilliseconds(long.TryParse(invocationResult.Data.DeadlineMs, out var deadlineMsValue)
                ? deadlineMsValue
                : DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds());

            // Calculate the delay to the deadline
            var delay = targetTime > DateTimeOffset.UtcNow
                ? targetTime - DateTimeOffset.UtcNow
                : TimeSpan.Zero;

            // Adjust the delay to trigger the cancellation slightly before the deadline, with a guard against negative values
            var adjustedDelay = (delay.TotalMilliseconds > 100)
                ? delay.Add(TimeSpan.FromMilliseconds(-100))
                : TimeSpan.Zero;

            // Create the CancellationTokenSource with the adjusted delay
            using var cts = new CancellationTokenSource(adjustedDelay);

            // Combine the external cancellation token with the deadline-based token
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var invocation = invocationResult.Data;

            using var context = _logger.WithContext("AwsRequestId", invocation.RequestId);

            // Deserialize the request payload
            try
            {
                _logger.BootstrapInvocationRequestDeserializationStart();

                var request = JsonSerializer.Deserialize(invocation.Payload, _requestTypeInfo);
                if (request == null)
                {
                    _logger.BootstrapInvocationRequestDeserializationFailed();
                    var errorPayload = new InvocationErrorPayload("DeserializationError", "Failed to deserialize request payload. Null payload returned.", Array.Empty<string>());
                    await LambdaRuntimeClient.ReportInvocationErrorAsync(invocation.RequestId, errorPayload, cancellationToken);
                    continue;
                }

                // Process the event and get the response
                var response = await _onNext(request, invocation, linkedCts.Token);

                // Serialize and send the response back to the runtime
                var responsePayload = _responseSerializer.Serialize(response);
                await LambdaRuntimeClient.SendResponseAsync(invocation.RequestId, responsePayload, cancellationToken);
            }
            catch (Exception ex)
            {
                // Capture the error and send it to the Runtime API
                var errorPayload = new InvocationErrorPayload(ex.GetType().FullName ?? ex.GetType().Name, ex.Message, ex.StackTrace?.Split(Environment.NewLine) ?? Array.Empty<string>());
                await LambdaRuntimeClient.ReportInvocationErrorAsync(invocation.RequestId, errorPayload, cancellationToken);
            }
        }
    }
}

/// <summary>
///     Bootstraps a lambda function and handles the lifecycle with the AWS Lambda Runtime
/// </summary>
/// <typeparam name="TFunction">The function class that we want to instantiate</typeparam>
/// <typeparam name="TRequest">The type of request that the function handles</typeparam>
/// <typeparam name="TResponse">The type of response that the function returns</typeparam>
public sealed class LambdaBootstrapper<TFunction, TRequest, TResponse> : LambdaBootstrapper<TRequest, TResponse>
    where TFunction : ILambdaFunction<TRequest, TResponse>
{
    private readonly Func<TFunction>? _functionFactory;

    /// <summary>
    ///     Constructs a new LambdaBootstrapper
    /// </summary>
    /// <param name="jsonSerializerContext">The JsonSerializerContext that knows about the request/response types</param>
    /// <param name="functionFactory">The factory that creates the function</param>
    /// <param name="responseSerializer">The response serializer that sends the responses back to the lambda runtime</param>
    /// <param name="lambdaRuntimeClient">The implementation of the lambda runtime client</param>
    public LambdaBootstrapper(JsonSerializerContext jsonSerializerContext, Func<TFunction> functionFactory, IResponseSerializer<TResponse>? responseSerializer = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
        : base(jsonSerializerContext, null, responseSerializer, lambdaRuntimeClient)
    {
        _functionFactory = functionFactory;
    }

    /// <summary>
    ///     Constructs a new LambdaBootstrapper
    /// </summary>
    /// <param name="jsonSerializerContext">The JsonSerializerContext that knows about the request/response types</param>
    /// <param name="lambdaFunction">The lambda function that will be invoked</param>
    /// <param name="responseSerializer">The response serializer that sends the responses back to the lambda runtime</param>
    /// <param name="lambdaRuntimeClient">The implementation of the lambda runtime client</param>
    public LambdaBootstrapper(JsonSerializerContext jsonSerializerContext, TFunction lambdaFunction, IResponseSerializer<TResponse>? responseSerializer = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
        : base(jsonSerializerContext, lambdaFunction.InvokeAsync, responseSerializer, lambdaRuntimeClient)
    {
        _functionFactory = null;
    }

    /// <summary>
    ///     Runs the Lambda request loop with initialization error handling
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that stops the processing of a given Lambda invocation</param>
    public new async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_functionFactory is not null)
        {
            try
            {
                var function = _functionFactory();
                OnNext(function.InvokeAsync);
            }
            catch (Exception ex)
            {
                var errorPayload = new InitializationErrorPayload(ex.GetType().FullName ?? ex.GetType().Name, "Function Factory Initialization Failure", ex.ToString());
                await LambdaRuntimeClient.ReportInitializationErrorAsync(errorPayload, cancellationToken);
                throw;
            }
        }

        await base.RunAsync(cancellationToken);
    }
}
