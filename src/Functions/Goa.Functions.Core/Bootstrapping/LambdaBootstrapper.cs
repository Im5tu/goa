using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Goa.Core;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.Core.Bootstrapping;

public sealed class LambdaBootstrapper<TFunction, TRequest, TResponse>
    where TFunction : FunctionBase<TRequest, TResponse>, new()
{
    private readonly IResponseSerializer<TResponse> _responseSerializer;
    private readonly ILogger _logger;
    private readonly ILambdaRuntimeClient _lambdaRuntimeClient;
    private readonly JsonTypeInfo<TRequest> _requestTypeInfo;

    public LambdaBootstrapper(JsonSerializerContext jsonSerializerContext, IResponseSerializer<TResponse>? responseSerializer = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        var logLevel = Enum.TryParse<LogLevel>(Environment.GetEnvironmentVariable("GOA__LOG__LEVEL"), out var level) ? level : LogLevel.Information;

        _logger = new JsonLogger("LambdaBootstrapper", logLevel, LogScopeProvider.Instance, LoggingSerializationContext.Default);
        _lambdaRuntimeClient = lambdaRuntimeClient ?? new LambdaRuntimeClient(logLevel);
        _responseSerializer = responseSerializer ?? new JsonResponseSerializer<TResponse>(jsonSerializerContext);
        _requestTypeInfo = jsonSerializerContext.GetTypeInfo(typeof(TRequest)) as JsonTypeInfo<TRequest> ?? throw new Exception("Cannot find serialization information for the type: " + typeof(TRequest).FullName);
    }

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

            var targetTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(invocationResult.Data.DeadlineMs ?? "6000"));
            TimeSpan delay = targetTime - DateTimeOffset.UtcNow;
            using var cts = new CancellationTokenSource(delay.Add(TimeSpan.FromMilliseconds(-100)));
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
