using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
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

        using var logContext = Logger.BeginScope(new LogScope8(
            new("Client", _clientType),
            new("Region", Configuration.Region),
            new("Service", Configuration.Service),
            new("SigningService", Configuration.SigningService),
            new("Target", target),
            new("ApiVersion", Configuration.ApiVersion),
            new("Method", request.Method.Method),
            new("Uri", request.RequestUri?.AbsoluteUri ?? "Unknown")
        ));

        try
        {
            Logger.RequestStart(Configuration.LogLevel);

            var start = Stopwatch.GetTimestamp();
            var response = await client.SendAsync(request, cancellationToken);

            // Log fixed response fields with zero-allocation scope
            using var responseLogContext = Logger.BeginScope(new LogScope2(
                new("StatusCode", ((int)response.StatusCode).ToString()),
                new("ReasonPhrase", response.ReasonPhrase ?? response.StatusCode.ToString())
            ));

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
}
