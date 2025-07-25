using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Clients.Core;

/// <summary>
/// AWS service client that handles JSON serialization/deserialization.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class JsonAwsServiceClient<T> : AwsServiceClient<T> where T : AwsServiceConfiguration
{
    private static readonly ApiError DeserializationError = new("Failed to deserialize response", "DeserializationError");
    private readonly JsonSerializerContext _jsonSerializerContext;

    /// <summary>
    /// Initializes a new instance of the JsonAwsServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the AWS service.</param>
    /// <param name="jsonSerializerContext">JSON serializer context for source-generated serialization.</param>
    protected JsonAwsServiceClient(IHttpClientFactory httpClientFactory, ILogger logger, T configuration, JsonSerializerContext jsonSerializerContext)
        : base(httpClientFactory, logger, configuration)
    {
        _jsonSerializerContext = jsonSerializerContext;
    }

    /// <summary>
    /// Sends a JSON request and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response.</typeparam>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The API endpoint to target.</param>
    /// <param name="request">The request object to serialize and send (optional).</param>
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="headers">Additional headers to add to the request.</param>
    /// <returns>An API response containing either the deserialized response or error information.</returns>
    protected async Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string requestUri, TRequest request, string target, CancellationToken cancellationToken, Dictionary<string, string>? headers = null)
        where TResponse : class
    {
        string? content = null;
        if (request != null && method != HttpMethod.Get)
        {
            if (request is string stringPayload && IsJsonSerialized(stringPayload))
            {
                content = stringPayload;
            }
            else
            {
                content = SerializeToJson(request);
            }
        }

        var requestMessage = CreateRequestMessage(method, requestUri, content, new MediaTypeHeaderValue("application/x-amz-json-1.0"), headers);
        var response = await SendAsync(requestMessage, target, cancellationToken);

        return await ProcessJsonResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// Processes an HTTP response and converts it to an API response with JSON deserialization.
    /// </summary>
    private async Task<ApiResponse<TResponse>> ProcessJsonResponseAsync<TResponse>(HttpResponseMessage response) where TResponse : class
    {
        var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);

        if (!response.IsSuccessStatusCode)
        {
            var errorPayload = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(errorPayload))
            {
                Logger.RequestFailed("No payload present");
                return new ApiResponse<TResponse>(new ApiError("Request not successful.") { StatusCode = response.StatusCode });
            }

            var error = DeserializeJsonError(errorPayload);
            if (error is not null)
            {
                error = error with
                {
                    Payload = errorPayload,
                    StatusCode = response.StatusCode
                };
            }
            else
            {
                error = new ApiError("Failed to send request and unable to deserialize payload") { StatusCode = response.StatusCode };
            }

            // Apply AWS-specific error header processing
            error = ProcessAwsErrorHeaders(response, error);

            Logger.RequestFailed($"Type: {error?.Type ?? "Unknown"}, Message: {error?.Message ?? "Unknown"}, Code: {error?.Code ?? "Unknown"}");
            return new ApiResponse<TResponse>(error ?? DeserializationError);
        }

        var contentPayload = await response.Content.ReadAsStringAsync();
        Debug.WriteLine("RESPONSE: " + contentPayload);

        // Handle string responses specially (no constraints needed!)
        if (typeof(TResponse) == typeof(string))
        {
            return new ApiResponse<TResponse>(contentPayload as TResponse, headers);
        }

        if (string.IsNullOrWhiteSpace(contentPayload))
        {
            return new ApiResponse<TResponse>(default(TResponse), headers);
        }

        var result = DeserializeFromJson<TResponse>(contentPayload);
        return result is null ? new ApiResponse<TResponse>(DeserializationError) : new ApiResponse<TResponse>(result, headers);
    }

    /// <summary>
    /// Applies AWS-specific error header processing to the error object.
    /// </summary>
    private ApiError ProcessAwsErrorHeaders(HttpResponseMessage response, ApiError error)
    {
        // Logic: https://github.com/aws/aws-sdk-net/blob/a9aa4e78a927e1021114b6531e21fc25f87e0dd9/sdk/src/Core/Amazon.Runtime/Internal/Transform/JsonErrorResponseUnmarshaller.cs#L79
        if (response.Headers.TryGetValues(XAmznErrorMessage, out var messages))
        {
            error = error with { Message = string.Join(", ", messages) };
        }

        // Logic: https://github.com/aws/aws-sdk-net/blob/a9aa4e78a927e1021114b6531e21fc25f87e0dd9/sdk/src/Core/Amazon.Runtime/Internal/Transform/JsonErrorResponseUnmarshaller.cs#L60
        if (string.IsNullOrWhiteSpace(error.Type) && response.Headers.TryGetValues(XAmzErrorType, out var types))
        {
            error = error with { Type = string.Join(", ", types) };
        }

        // Logic: https://github.com/aws/aws-sdk-net/blob/a9aa4e78a927e1021114b6531e21fc25f87e0dd9/sdk/src/Core/Amazon.Runtime/Internal/Transform/JsonErrorResponseUnmarshaller.cs#L68
        var infoSeparator = error.Type?.LastIndexOf(':') ?? -1;
        if (infoSeparator > 0)
        {
            error = error with { Type = error.Type![..infoSeparator] };
        }

        // Logic: https://github.com/aws/aws-sdk-net/blob/a9aa4e78a927e1021114b6531e21fc25f87e0dd9/sdk/src/Core/Amazon.Runtime/Internal/Transform/JsonErrorResponseUnmarshaller.cs#L95
        var typeSeparator = error.Type?.LastIndexOf('#') ?? -1;
        if (typeSeparator > 0)
        {
            error = error with { Type = error.Type![(typeSeparator + 1)..] };
        }

        return error;
    }

    /// <summary>
    /// Serializes a request object to JSON using the configured serialization context.
    /// </summary>
    private string SerializeToJson<TRequest>(TRequest request)
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(TRequest)) as JsonTypeInfo<TRequest>;
        if (typeInfo is null)
            throw new InvalidOperationException($"Cannot find type {typeof(TRequest).Name} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Serialize(request, typeInfo);
    }

    /// <summary>
    /// Deserializes JSON content to the specified type using the configured serialization context.
    /// </summary>
    private TResponse? DeserializeFromJson<TResponse>(string content) where TResponse : class
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(TResponse)) as JsonTypeInfo<TResponse>;
        if (typeInfo is null)
            throw new InvalidOperationException($"Cannot find type {typeof(TResponse).Name} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Deserialize<TResponse>(content, typeInfo);
    }

    /// <summary>
    /// Deserializes JSON error response content to an ApiError.
    /// </summary>
    private ApiError? DeserializeJsonError(string content)
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(ApiError)) as JsonTypeInfo<ApiError>;
        if (typeInfo is null)
            throw new InvalidOperationException($"Cannot find type {nameof(ApiError)} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Deserialize<ApiError>(content, typeInfo);
    }

    /// <summary>
    /// Determines if the given string is already in JSON format.
    /// </summary>
    private static bool IsJsonSerialized(string input)
    {
        var trimmed = input.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }
}