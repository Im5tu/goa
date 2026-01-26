using ErrorOr;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Errors;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Operations.InvokeModel;
using Goa.Clients.Bedrock.Serialization;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Bedrock;

/// <summary>
/// High-performance Bedrock service client that implements IBedrockClient using AWS service client infrastructure.
/// Provides strongly-typed Bedrock operations with built-in error handling, logging, and AWS authentication.
/// </summary>
public class BedrockServiceClient : JsonAwsServiceClient<BedrockServiceClientConfiguration>, IBedrockClient
{
    /// <summary>
    /// Initializes a new instance of the BedrockServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the Bedrock service.</param>
    public BedrockServiceClient(IHttpClientFactory httpClientFactory, ILogger<BedrockServiceClient> logger, BedrockServiceClientConfiguration configuration)
        : base(httpClientFactory, logger, configuration, BedrockJsonContext.Default)
    {
    }

    /// <summary>
    /// Sends a conversation request to a Bedrock model using the Converse API.
    /// </summary>
    /// <param name="request">The converse request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The converse response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<ConverseResponse>> ConverseAsync(ConverseRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ConverseRequest, ConverseResponse>(
            HttpMethod.Post,
            $"/model/{Uri.EscapeDataString(request.ModelId)}/converse",
            request,
            "bedrock:InvokeModel",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Invokes a Bedrock model with a raw JSON payload using the InvokeModel API.
    /// </summary>
    /// <param name="request">The invoke model request containing the raw JSON body.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The invoke model response containing the raw model output, or an error if the operation failed.</returns>
    public async Task<ErrorOr<InvokeModelResponse>> InvokeModelAsync(InvokeModelRequest request, CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = request.ContentType,
            ["Accept"] = request.Accept
        };

        if (!string.IsNullOrEmpty(request.GuardrailIdentifier))
        {
            headers["X-Amzn-Bedrock-GuardrailIdentifier"] = request.GuardrailIdentifier;
        }

        if (!string.IsNullOrEmpty(request.GuardrailVersion))
        {
            headers["X-Amzn-Bedrock-GuardrailVersion"] = request.GuardrailVersion;
        }

        if (request.PerformanceConfigLatency.HasValue)
        {
            headers["X-Amzn-Bedrock-PerformanceConfig-Latency"] = request.PerformanceConfigLatency.Value.ToString().ToLowerInvariant();
        }

        if (request.ServiceTier.HasValue)
        {
            headers["X-Amzn-Bedrock-ServiceTier"] = request.ServiceTier.Value.ToString().ToLowerInvariant();
        }

        var response = await SendAsync<string, string>(
            HttpMethod.Post,
            $"/model/{Uri.EscapeDataString(request.ModelId)}/invoke",
            request.Body,
            "bedrock:InvokeModel",
            cancellationToken,
            headers);

        if (!response.IsSuccess)
        {
            var error = response.Error!;
            var bedrockError = Error.Failure(
                code: MapErrorCodeToBedrock(error.Type ?? error.Code ?? "Unknown"),
                description: error.Message ?? "An error occurred while processing the request.");

            return bedrockError;
        }

        string? contentType = null;
        if (response.Headers != null && response.Headers.TryGetValue("Content-Type", out var contentTypeValues))
        {
            contentType = string.Join(", ", contentTypeValues);
        }

        return new InvokeModelResponse
        {
            Body = response.Value ?? string.Empty,
            ContentType = contentType
        };
    }

    /// <summary>
    /// Applies a guardrail to content using the ApplyGuardrail API.
    /// </summary>
    /// <param name="request">The apply guardrail request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The apply guardrail response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<ApplyGuardrailResponse>> ApplyGuardrailAsync(ApplyGuardrailRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ApplyGuardrailRequest, ApplyGuardrailResponse>(
            HttpMethod.Post,
            $"/guardrail/{Uri.EscapeDataString(request.GuardrailIdentifier)}/version/{Uri.EscapeDataString(request.GuardrailVersion)}/apply",
            request,
            "bedrock:ApplyGuardrail",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Counts the number of tokens in a request using the CountTokens API.
    /// </summary>
    /// <param name="request">The count tokens request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The count tokens response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<CountTokensResponse>> CountTokensAsync(CountTokensRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<CountTokensRequest, CountTokensResponse>(
            HttpMethod.Post,
            $"/model/{Uri.EscapeDataString(request.ModelId)}/count-tokens",
            request,
            "bedrock:CountTokens",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Converts an ApiResponse to an ErrorOr result, mapping AWS-specific error codes to Bedrock error codes.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="response">The API response to convert.</param>
    /// <returns>An ErrorOr containing either the response data or mapped errors.</returns>
    private static ErrorOr<T> ConvertApiResponse<T>(ApiResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Value!;
        }

        var error = response.Error!;
        var bedrockError = Error.Failure(
            code: MapErrorCodeToBedrock(error.Type ?? error.Code ?? "Unknown"),
            description: error.Message ?? "An error occurred while processing the request.");

        return bedrockError;
    }

    /// <summary>
    /// Maps AWS Bedrock error types to Goa.Bedrock prefixed error codes.
    /// </summary>
    /// <param name="awsErrorType">The AWS error type or code.</param>
    /// <returns>A Goa.Bedrock prefixed error code.</returns>
    private static string MapErrorCodeToBedrock(string awsErrorType)
    {
        return awsErrorType switch
        {
            "AccessDeniedException" => BedrockErrorCodes.AccessDeniedException,
            "ModelErrorException" => BedrockErrorCodes.ModelErrorException,
            "ModelNotReadyException" => BedrockErrorCodes.ModelNotReadyException,
            "ModelTimeoutException" => BedrockErrorCodes.ModelTimeoutException,
            "ResourceNotFoundException" => BedrockErrorCodes.ResourceNotFoundException,
            "ServiceQuotaExceededException" => BedrockErrorCodes.ServiceQuotaExceededException,
            "ThrottlingException" => BedrockErrorCodes.ThrottlingException,
            "ValidationException" => BedrockErrorCodes.ValidationException,
            _ => $"Goa.Bedrock.{awsErrorType}"
        };
    }
}
