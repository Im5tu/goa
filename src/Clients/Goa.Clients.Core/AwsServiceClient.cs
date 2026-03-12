using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Goa.Clients.Core;

/// <summary>
/// Abstract base class for AWS service clients providing HTTP communication, request signing, and AWS-specific handling.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class AwsServiceClient<T> where T : AwsServiceConfiguration
{
    /// <summary>
    /// HTTP header name for Amazon error messages.
    /// </summary>
    public const string XAmznErrorMessage = "x-amzn-error-message";

    /// <summary>
    /// HTTP header name for Amazon error types.
    /// </summary>
    public const string XAmzErrorType = "x-amzn-ErrorType";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientType;
    private Uri? _cachedBaseUri;

    /// <summary>
    /// Gets the logger instance for this service client.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets the configuration for this AWS service client.
    /// </summary>
    protected T Configuration { get; }

    /// <summary>
    /// Initializes a new instance of the AwsServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the AWS service.</param>
    protected AwsServiceClient(IHttpClientFactory httpClientFactory, ILogger logger, T configuration)
    {
        _clientType = GetType().Name;
        _httpClientFactory = httpClientFactory;
        Logger = logger;
        Configuration = configuration;
    }

    /// <summary>
    /// Sends an HTTP request asynchronously and returns the raw HTTP response.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The HTTP response message.</returns>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, string target, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(_clientType);

        request.Options.Set(HttpOptions.Region, Configuration.Region);
        request.Options.Set(HttpOptions.Service, Configuration.SigningService);
        request.Options.Set(HttpOptions.Target, target);
        request.Options.Set(HttpOptions.ApiVersion, Configuration.ApiVersion);

        using var logContext = Logger.IsEnabled(Configuration.LogLevel)
            ? Logger.BeginScope(new LogScope8(
                new("Client", _clientType),
                new("Region", Configuration.Region),
                new("Service", Configuration.Service),
                new("SigningService", Configuration.SigningService),
                new("Target", target),
                new("ApiVersion", Configuration.ApiVersion),
                new("Method", request.Method.Method),
                new("Uri", request.RequestUri?.AbsoluteUri ?? "Unknown")
            ))
            : null;

        try
        {
            Logger.RequestStart(Configuration.LogLevel);

            var start = Stopwatch.GetTimestamp();
            var response = await client.SendAsync(request, cancellationToken);

            // Log fixed response fields with zero-allocation scope
            using var responseLogContext = Logger.IsEnabled(Configuration.LogLevel)
                ? Logger.BeginScope(new LogScope2(
                    new("StatusCode", ((int)response.StatusCode).ToString()),
                    new("ReasonPhrase", response.ReasonPhrase ?? response.StatusCode.ToString())
                ))
                : null;

            // Log x-amz response headers separately with capacity hint
            Dictionary<string, object>? amzHeaders = null;
            foreach (var header in response.Headers)
            {
                if (header.Key.StartsWith("x-amz", StringComparison.OrdinalIgnoreCase))
                {
                    amzHeaders ??= new Dictionary<string, object>(4);
                    amzHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }
            using var amzLogContext = amzHeaders is not null ? Logger.BeginScope(amzHeaders) : null;

            Logger.RequestComplete(Configuration.LogLevel, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            return response;
        }
        catch (Exception e)
        {
            Logger.RequestFailed(e);
            throw;
        }
    }

    /// <summary>
    /// Applies AWS-specific error header processing to the error object.
    /// </summary>
    protected static ApiError ProcessAwsErrorHeaders(HttpResponseMessage response, ApiError error)
    {
        if (response.Headers.TryGetValues(XAmznErrorMessage, out var messages))
        {
            error = error with { Message = string.Join(", ", messages) };
        }

        if (string.IsNullOrWhiteSpace(error.Type) && response.Headers.TryGetValues(XAmzErrorType, out var types))
        {
            error = error with { Type = string.Join(", ", types) };
        }

        var infoSeparator = error.Type?.LastIndexOf(':') ?? -1;
        if (infoSeparator > 0)
        {
            error = error with { Type = error.Type![..infoSeparator] };
        }

        var typeSeparator = error.Type?.LastIndexOf('#') ?? -1;
        if (typeSeparator > 0)
        {
            error = error with { Type = error.Type![(typeSeparator + 1)..] };
        }

        return error;
    }

    /// <summary>
    /// Creates an HTTP request message with the specified content.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The API endpoint to target.</param>
    /// <param name="content">The request content to send as UTF-8 bytes.</param>
    /// <param name="contentType">The content type for the request.</param>
    /// <param name="headers">Additional headers to add to the request.</param>
    /// <returns>A configured HTTP request message.</returns>
    protected HttpRequestMessage CreateRequestMessage(HttpMethod method, string requestUri, byte[]? content = null, MediaTypeHeaderValue? contentType = null, Dictionary<string, string>? headers = null)
    {
        var baseUri = _cachedBaseUri ??= new Uri(
            Configuration.ServiceUrl ?? $"https://{Configuration.Service.ToLower()}.{Configuration.Region}.amazonaws.com/");

        Uri finalUri;
        if (requestUri == "/")
        {
            finalUri = baseUri;
        }
        else
        {
            requestUri = requestUri.TrimStart('/');
            finalUri = new Uri(baseUri, requestUri);
        }

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = finalUri,
            Method = method
        };

        if (content != null && content.Length > 0 && method != HttpMethod.Get)
        {
            var byteContent = new ByteArrayContent(content);
            if (contentType != null)
                byteContent.Headers.ContentType = contentType;
            requestMessage.Content = byteContent;
            requestMessage.Options.Set(HttpOptions.Payload, content.AsMemory());
        }

        if (headers != null)
        {
            foreach (var header in headers)
                requestMessage.Headers.Add(header.Key, header.Value);
        }

        return requestMessage;
    }

    /// <summary>
    /// Creates an HTTP request message using a <see cref="PooledBufferWriter"/> for zero-copy content.
    /// The <see cref="ReadOnlyMemoryContent"/> borrows the buffer writer's memory; the caller must
    /// ensure the buffer writer outlives the request message.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The API endpoint to target.</param>
    /// <param name="bufferWriter">The pooled buffer writer containing the serialized request body.</param>
    /// <param name="contentType">The content type for the request.</param>
    /// <param name="headers">Additional headers to add to the request.</param>
    /// <returns>A configured HTTP request message.</returns>
    protected HttpRequestMessage CreateRequestMessage(HttpMethod method, string requestUri, PooledBufferWriter bufferWriter, MediaTypeHeaderValue? contentType = null, Dictionary<string, string>? headers = null)
    {
        var baseUri = _cachedBaseUri ??= new Uri(
            Configuration.ServiceUrl ?? $"https://{Configuration.Service.ToLower()}.{Configuration.Region}.amazonaws.com/");

        Uri finalUri;
        if (requestUri == "/")
        {
            finalUri = baseUri;
        }
        else
        {
            requestUri = requestUri.TrimStart('/');
            finalUri = new Uri(baseUri, requestUri);
        }

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = finalUri,
            Method = method
        };

        var memory = bufferWriter.WrittenMemory;
        if (memory.Length > 0 && method != HttpMethod.Get)
        {
            var content = new ReadOnlyMemoryContent(memory);
            if (contentType != null)
                content.Headers.ContentType = contentType;
            requestMessage.Content = content;
            requestMessage.Options.Set(HttpOptions.Payload, memory);
        }

        if (headers != null)
        {
            foreach (var header in headers)
                requestMessage.Headers.Add(header.Key, header.Value);
        }

        return requestMessage;
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
                Throw.InvalidOperation($"Response Content-Length {headerLength} exceeds maximum {MaxResponseSize} bytes.");

            var contentLength = (int)headerLength;
            var knownBuffer = ArrayPool<byte>.Shared.Rent(contentLength);
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
                ArrayPool<byte>.Shared.Return(knownBuffer);
                throw;
            }
        }

        const int initialSize = 4096;
        var buffer = ArrayPool<byte>.Shared.Rent(initialSize);
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

                    var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
                    buffer.AsSpan(0, totalRead).CopyTo(newBuffer);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                var read = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead), cancellationToken);
                if (read == 0) break;
                totalRead += read;

                if (totalRead > MaxResponseSize)
                    Throw.InvalidOperation($"Response size exceeds maximum {MaxResponseSize} bytes.");
            }

            if (totalRead == 0)
            {
                ArrayPool<byte>.Shared.Return(buffer);
                return default;
            }

            return new PooledBuffer(buffer, totalRead);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
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
                ArrayPool<byte>.Shared.Return(buf);
            }
        }
    }
}
