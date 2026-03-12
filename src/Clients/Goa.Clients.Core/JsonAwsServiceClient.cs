using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Buffers;

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

            var errorPayload = Encoding.UTF8.GetString(errorBuffer.Span);
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
        var trimmed = input.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    /// <summary>
    /// Maximum allowed response size in bytes (4 MB).
    /// </summary>
    protected const int MaxResponseSize = 4 * 1024 * 1024;

    /// <summary>
    /// Reads the HTTP response body into a pooled byte buffer for zero-copy deserialization.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A pooled buffer containing the response bytes. Must be disposed after use.</returns>
    protected static async Task<PooledBuffer> ReadResponseBytesAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var headerLength = response.Content.Headers.ContentLength;
        if (headerLength is 0)
            return default;

        if (headerLength is not null)
        {
            if (headerLength < 0 || headerLength > MaxResponseSize)
                throw new InvalidOperationException($"Response Content-Length {headerLength} exceeds maximum {MaxResponseSize} bytes.");

            var contentLength = (int)headerLength;
            var knownBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(contentLength);
            try
            {
                var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var totalRead = 0;
                while (totalRead < contentLength)
                {
                    var read = await stream.ReadAsync(knownBuffer.AsMemory(totalRead, contentLength - totalRead), cancellationToken);
                    if (read == 0) break;
                    totalRead += read;
                }
                return new PooledBuffer(knownBuffer, totalRead);
            }
            catch
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(knownBuffer);
                throw;
            }
        }

        const int initialSize = 4096;
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(initialSize);
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var totalRead = 0;
            while (true)
            {
                if (totalRead == buffer.Length)
                {
                    var newSize = buffer.Length * 2;
                    if (newSize > MaxResponseSize)
                        newSize = MaxResponseSize + 1; // allow one more read to detect overflow

                    var newBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(newSize);
                    buffer.AsSpan(0, totalRead).CopyTo(newBuffer);
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
                if (read == 0) break;
                totalRead += read;

                if (totalRead > MaxResponseSize)
                    throw new InvalidOperationException($"Response size exceeds maximum {MaxResponseSize} bytes.");
            }

            if (totalRead == 0)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                return default;
            }

            return new PooledBuffer(buffer, totalRead);
        }
        catch
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// A disposable buffer backed by ArrayPool for zero-allocation response reading.
    /// </summary>
    protected struct PooledBuffer : IDisposable
    {
        private byte[]? _buffer;

        /// <summary>The number of valid bytes in the buffer.</summary>
        public readonly int Length;

        /// <summary>Creates a new PooledBuffer wrapping the given rented array.</summary>
        public PooledBuffer(byte[] buffer, int length)
        {
            _buffer = buffer;
            Length = length;
        }

        /// <summary>Gets a read-only span over the valid portion of the buffer.</summary>
        public readonly ReadOnlySpan<byte> Span => _buffer is null ? default : _buffer.AsSpan(0, Length);

        /// <summary>Returns the rented buffer to the shared ArrayPool.</summary>
        public void Dispose()
        {
            var buf = _buffer;
            if (buf is not null)
            {
                _buffer = null;
                System.Buffers.ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}
