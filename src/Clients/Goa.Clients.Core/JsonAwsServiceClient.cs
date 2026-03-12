using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Clients.Core;

/// <summary>
/// AWS service client that handles JSON serialization/deserialization.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class JsonAwsServiceClient<T> : AwsServiceClient<T> where T : AwsServiceConfiguration
{
    /// <summary>
    /// Standard error returned when response deserialization fails.
    /// </summary>
    protected static readonly ApiError DeserializationError = new("Failed to deserialize response", "DeserializationError");

    /// <summary>
    /// Standard AWS JSON content type.
    /// </summary>
    protected static readonly MediaTypeHeaderValue JsonContentType = new("application/x-amz-json-1.0");

    /// <summary>
    /// Initializes a new instance of the JsonAwsServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the AWS service.</param>
    protected JsonAwsServiceClient(IHttpClientFactory httpClientFactory, ILogger logger, T configuration)
        : base(httpClientFactory, logger, configuration)
    {
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
        if (request is null || method == HttpMethod.Get)
        {
            using var requestMessage = CreateRequestMessage(method, requestUri, content: null, JsonContentType, headers);
            using var response = await SendAsync(requestMessage, target, cancellationToken);
            return await ProcessJsonResponseAsync<TResponse>(response, cancellationToken);
        }

        using var bufferWriter = new PooledBufferWriter(1024);
        if (request is string stringPayload && IsJsonSerialized(stringPayload))
        {
            var byteCount = Encoding.UTF8.GetByteCount(stringPayload);
            Encoding.UTF8.GetBytes(stringPayload, bufferWriter.GetSpan(byteCount));
            bufferWriter.Advance(byteCount);
        }
        else
        {
            var typeInfo = ResolveJsonTypeInfo<TRequest>();
            using var writer = new Utf8JsonWriter(bufferWriter);
            JsonSerializer.Serialize(writer, request, typeInfo);
        }

        using var msg = CreateRequestMessage(method, requestUri, bufferWriter, JsonContentType, headers);
        using var resp = await SendAsync(msg, target, cancellationToken);
        return await ProcessJsonResponseAsync<TResponse>(resp, cancellationToken);
    }

    /// <summary>
    /// Processes an HTTP response and converts it to an API response with JSON deserialization.
    /// Uses pooled byte buffers for zero-copy deserialization on the success path
    /// and delegates type resolution to <see cref="ResolveJsonTypeInfo{TValue}"/>.
    /// </summary>
    protected async Task<ApiResponse<TResponse>> ProcessJsonResponseAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken) where TResponse : class
    {
        if (!response.IsSuccessStatusCode)
        {
            using var errorBuffer = await ReadResponseBytesAsync(response, cancellationToken);
            if (errorBuffer.Length == 0)
            {
                Logger.RequestFailed("No payload present");
                return new ApiResponse<TResponse>(new ApiError("Request not successful.") { StatusCode = response.StatusCode });
            }

            var error = ApiErrorReader.ReadApiError(errorBuffer.Span);
            var errorPayload = Encoding.UTF8.GetString(errorBuffer.Span);
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

            Logger.RequestFailed($"Type: {error.Type ?? "Unknown"}, Message: {error.Message ?? "Unknown"}, Code: {error.Code ?? "Unknown"}");
            return new ApiResponse<TResponse>(error);
        }

        var headers = ResponseHeaders.FromHttpResponse(response.Headers, response.Content.Headers);
        using var pooledBuffer = await ReadResponseBytesAsync(response, cancellationToken);

        if (typeof(TResponse) == typeof(string))
        {
            var str = pooledBuffer.Length > 0 ? Encoding.UTF8.GetString(pooledBuffer.Span) : string.Empty;
            return new ApiResponse<TResponse>(str as TResponse, headers);
        }

        if (pooledBuffer.Length == 0)
            return new ApiResponse<TResponse>(default(TResponse), headers);

        var typeInfo = ResolveJsonTypeInfo<TResponse>();
        var jsonReader = new Utf8JsonReader(pooledBuffer.Span);
        var result = JsonSerializer.Deserialize(ref jsonReader, typeInfo);
        return new ApiResponse<TResponse>(result, headers);
    }

    /// <summary>
    /// Resolves the <see cref="JsonTypeInfo{T}"/> for the specified type from the service-specific JSON context.
    /// Each service client implements this to return type info from its generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>.
    /// </summary>
    /// <typeparam name="TValue">The type to resolve serialization metadata for.</typeparam>
    /// <returns>The resolved JSON type info.</returns>
    protected abstract JsonTypeInfo<TValue> ResolveJsonTypeInfo<TValue>();

    /// <summary>
    /// Deserializes JSON error response content to an ApiError.
    /// </summary>
    protected ApiError? DeserializeJsonError(string content) => ApiErrorReader.ReadApiError(content);

    /// <summary>
    /// Determines if the given string is already in JSON format.
    /// </summary>
    private static bool IsJsonSerialized(string input)
    {
        var trimmed = input.AsSpan().TrimStart();
        return trimmed.StartsWith("{") || trimmed.StartsWith("[");
    }
}
