using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Lambda.Models;
using Goa.Clients.Lambda.Operations.Invoke;
using Goa.Clients.Lambda.Operations.InvokeAsync;
using Goa.Clients.Lambda.Operations.InvokeDryRun;
using Goa.Clients.Lambda.Serialization;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Goa.Clients.Lambda;

internal sealed class LambdaServiceClient : JsonAwsServiceClient<LambdaServiceClientConfiguration>, ILambdaClient
{
    public LambdaServiceClient(
        IHttpClientFactory httpClientFactory,
        LambdaServiceClientConfiguration configuration,
        ILogger<LambdaServiceClient> logger)
        : base(httpClientFactory, logger, configuration, LambdaJsonContext.Default)
    {
    }

    public async Task<ErrorOr<InvokeResponse>> InvokeSynchronousAsync(InvokeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FunctionName))
            return Error.Validation("InvokeRequest.FunctionName", "Function name is required.");

        try
        {
            var headers = new Dictionary<string, string>();

            // Add log type header
            if (request.LogType != LogType.None)
                headers["X-Amz-Log-Type"] = request.LogType.ToString();

            // Add client context header
            if (!string.IsNullOrWhiteSpace(request.ClientContext))
                headers["X-Amz-Client-Context"] = request.ClientContext;

            // Build the URI
            var functionName = Uri.EscapeDataString(request.FunctionName);
            var requestUri = $"/2015-03-31/functions/{functionName}/invocations";

            if (!string.IsNullOrWhiteSpace(request.Qualifier))
                requestUri += $"?Qualifier={Uri.EscapeDataString(request.Qualifier)}";

            var response = await SendAsync<InvokeRequest, string>(
                HttpMethod.Post,
                requestUri,
                request,
                "Invoke",
                cancellationToken,
                headers);

            if (!response.IsSuccess)
                return ConvertApiError(response.Error!);

            var invokeResponse = InvokeResponse.FromHttpResponse(
                200,
                response.Value,
                response.Headers);

            return invokeResponse;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to invoke Lambda function synchronously {FunctionName}", request.FunctionName);
            return Error.Failure("Lambda.InvokeSynchronous.Failed", $"Failed to invoke Lambda function synchronously {request.FunctionName}");
        }
    }

    public async Task<ErrorOr<InvokeAsyncResponse>> InvokeAsynchronousAsync(InvokeAsyncRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FunctionName))
            return Error.Validation("InvokeAsyncRequest.FunctionName", "Function name is required.");

        try
        {
            var headers = new Dictionary<string, string>
            {
                ["X-Amz-Invocation-Type"] = "Event"
            };

            // Add client context header
            if (!string.IsNullOrWhiteSpace(request.ClientContext))
                headers["X-Amz-Client-Context"] = request.ClientContext;

            // Build the URI
            var functionName = Uri.EscapeDataString(request.FunctionName);
            var requestUri = $"/2015-03-31/functions/{functionName}/invocations";

            if (!string.IsNullOrWhiteSpace(request.Qualifier))
                requestUri += $"?Qualifier={Uri.EscapeDataString(request.Qualifier)}";

            var response = await SendAsync<InvokeAsyncRequest, string>(
                HttpMethod.Post,
                requestUri,
                request,
                "Invoke",
                cancellationToken,
                headers);

            if (!response.IsSuccess)
                return ConvertApiError(response.Error!);

            return new InvokeAsyncResponse
            {
                StatusCode = (int)HttpStatusCode.Accepted
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to invoke Lambda function asynchronously {FunctionName}", request.FunctionName);
            return Error.Failure("Lambda.InvokeAsynchronous.Failed", $"Failed to invoke Lambda function asynchronously {request.FunctionName}");
        }
    }

    public async Task<ErrorOr<InvokeDryRunResponse>> InvokeDryRunAsync(InvokeDryRunRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.FunctionName))
            return Error.Validation("InvokeDryRunRequest.FunctionName", "Function name is required.");

        try
        {
            var headers = new Dictionary<string, string>
            {
                ["X-Amz-Invocation-Type"] = "DryRun"
            };

            // Add client context header
            if (!string.IsNullOrWhiteSpace(request.ClientContext))
                headers["X-Amz-Client-Context"] = request.ClientContext;

            // Build the URI
            var functionName = Uri.EscapeDataString(request.FunctionName);
            var requestUri = $"/2015-03-31/functions/{functionName}/invocations";

            if (!string.IsNullOrWhiteSpace(request.Qualifier))
                requestUri += $"?Qualifier={Uri.EscapeDataString(request.Qualifier)}";

            var response = await SendAsync<string, string>(
                HttpMethod.Post,
                requestUri,
                request.Payload ?? "{}",
                "Invoke",
                cancellationToken,
                headers);

            if (!response.IsSuccess)
                return ConvertApiError(response.Error!);

            return new InvokeDryRunResponse
            {
                StatusCode = (int)HttpStatusCode.NoContent
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to dry run Lambda function {FunctionName}", request.FunctionName);
            return Error.Failure("Lambda.InvokeDryRun.Failed", $"Failed to dry run Lambda function {request.FunctionName}");
        }
    }

    private static Error ConvertApiError(ApiError apiError)
    {
        return Error.Failure(
            code: $"Goa.Lambda.{apiError.Type ?? apiError.Code ?? "Unknown"}",
            description: apiError.Message ?? "An error occurred while processing the request.");
    }
}
