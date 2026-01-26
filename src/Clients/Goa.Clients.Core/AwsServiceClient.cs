using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

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

        using var logContext = Logger.BeginScope(new Dictionary<string, object>
        {
            ["Client"] = _clientType,
            ["Region"] = Configuration.Region,
            ["Service"] = Configuration.Service,
            ["SigningService"] = Configuration.SigningService,
            ["Target"] = target,
            ["ApiVersion"] = Configuration.ApiVersion,
            ["Method"] = request.Method.ToString(),
            ["Uri"] = request.RequestUri?.ToString() ?? "Unknown"
        });

        try
        {
            Logger.RequestStart(Configuration.LogLevel);

            var sw = Stopwatch.StartNew();
            var response = await client.SendAsync(request, cancellationToken);

            // Ensure that we log the applicable response headers and status code
            var context = new Dictionary<string, object>
            {
                ["StatusCode"] = (int)response.StatusCode,
                ["ReasonPhrase"] = response.ReasonPhrase ?? response.StatusCode.ToString()
            };
            foreach (var header in response.Headers.Where(x => x.Key.StartsWith("x-amz", StringComparison.OrdinalIgnoreCase) || x.Key.StartsWith("x-amzn", StringComparison.OrdinalIgnoreCase)))
            {
                context[header.Key] = string.Join(", ", header.Value);
            }
            using var responseLogContext = Logger.BeginScope(context);

            Logger.RequestComplete(Configuration.LogLevel, sw.Elapsed.TotalMilliseconds);

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
        var uri = Configuration.ServiceUrl ?? $"https://{Configuration.Service.ToLower()}.{Configuration.Region}.amazonaws.com/";

        if (requestUri != "/")
        {
            requestUri = requestUri.TrimStart('/');
            if (!uri.EndsWith('/'))
            {
                uri = $"{uri}/{requestUri}";
            }
            else
            {
                uri = $"{uri}{requestUri}";
            }
        }

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(uri),
            Method = method
        };

        if (content != null && content.Length > 0 && method != HttpMethod.Get)
        {
            Debug.WriteLine("REQUEST: " + Encoding.UTF8.GetString(content));
            var byteContent = new ByteArrayContent(content);
            if (contentType != null)
                byteContent.Headers.ContentType = contentType;
            requestMessage.Content = byteContent;
            requestMessage.Options.Set(HttpOptions.Payload, content);
        }

        if (headers != null)
        {
            foreach (var header in headers)
                requestMessage.Headers.Add(header.Key, header.Value);
        }

        return requestMessage;
    }
}
