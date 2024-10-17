using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Goa.Core;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Bootstraps a lambda function and handles the lifecycle with the AWS Lambda Runtime
/// </summary>
/// <typeparam name="TFunction">The function class that we want to instantiate</typeparam>
/// <typeparam name="TRequest">The type of request that the function handles</typeparam>
/// <typeparam name="TResponse">The type of response that the function returns</typeparam>
public sealed class LambdaBootstrapper<TFunction, TRequest, TResponse>
    where TFunction : FunctionBase<TRequest, TResponse>, new()
{
    private readonly IResponseSerializer<TResponse> _responseSerializer;
    private readonly ILogger _logger;
    private readonly ILambdaRuntimeClient _lambdaRuntimeClient;
    private readonly JsonTypeInfo<TRequest> _requestTypeInfo;

    /// <summary>
    ///     Constructs a new LambdaBootstrapper
    /// </summary>
    /// <param name="jsonSerializerContext">The JsonSerializerContext that knows about the request/response types</param>
    /// <param name="responseSerializer">The response serializer that sends the responses back to the lambda runtime</param>
    /// <param name="lambdaRuntimeClient">The implementation of the lambda runtime client</param>
    public LambdaBootstrapper(JsonSerializerContext jsonSerializerContext, IResponseSerializer<TResponse>? responseSerializer = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        var logLevel = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("GOA__LOG__LEVEL"), out var level) ? level : LogLevel.Information;

        _logger = new JsonLogger("LambdaBootstrapper", logLevel, LogScopeProvider.Instance, LoggingSerializationContext.Default);
        _lambdaRuntimeClient = lambdaRuntimeClient ?? new LambdaRuntimeClient(logLevel);
        _responseSerializer = responseSerializer ?? new JsonResponseSerializer<TResponse>(jsonSerializerContext);
        _requestTypeInfo = jsonSerializerContext.GetTypeInfo(typeof(TRequest)) as JsonTypeInfo<TRequest> ?? throw new Exception("Cannot find serialization information for the type: " + typeof(TRequest).FullName);
    }

    /// <summary>
    ///     Runs the Lambda request loop
    /// </summary>
    /// <param name="cancellationToken">The cancellation token that stops the processing of a given Lambda invocation</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.BootstrapStarted();

        TFunction func = await GetFunctionAsync(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            // Get the next event
            var invocationResult = await _lambdaRuntimeClient.GetNextInvocationAsync(cancellationToken);
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
            TimeSpan delay = targetTime > DateTimeOffset.UtcNow
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
                    await _lambdaRuntimeClient.ReportInvocationErrorAsync(invocation.RequestId, errorPayload, cancellationToken);
                    continue;
                }

                // Process the event and get the response
                var response = await func.HandleAsync(request, linkedCts.Token);

                // Serialize and send the response back to the runtime
                var responsePayload = _responseSerializer.Serialize(response);
                await _lambdaRuntimeClient.SendResponseAsync(invocation.RequestId, responsePayload, cancellationToken);
            }
            catch (Exception ex)
            {
                // Capture the error and send it to the Runtime API
                var errorPayload = new InvocationErrorPayload(ex.GetType().FullName ?? ex.GetType().Name, ex.Message, ex.StackTrace?.Split(Environment.NewLine) ?? Array.Empty<string>());
                await _lambdaRuntimeClient.ReportInvocationErrorAsync(invocation.RequestId, errorPayload, cancellationToken);
            }
        }
    }

    private async Task<TFunction> GetFunctionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return new TFunction();
        }
        catch (Exception e)
        {
            var errorPayload = new InitializationErrorPayload("InitializationError", "StartupException", e.ToString());
            await _lambdaRuntimeClient.ReportInitializationErrorAsync(errorPayload, cancellationToken);
            throw;
        }
    }
}
