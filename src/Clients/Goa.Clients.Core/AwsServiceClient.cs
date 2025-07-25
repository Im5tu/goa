using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Clients.Core;

/// <summary>
/// Abstract base class for AWS service clients providing HTTP communication, request signing, and error handling.
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

    private static readonly ApiError DeserializationError = new("Failed to deserialize response", "DeserializationError");
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
    /// Sends an HTTP request asynchronously and deserializes the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the expected response.</typeparam>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="responseTypeInfo">JSON serialization metadata for the response type.</param>
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An API response containing either the deserialized response or error information.</returns>
    protected async Task<ApiResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, JsonTypeInfo<TResponse> responseTypeInfo, string target, CancellationToken cancellationToken)
        where TResponse : class
    {
        var client = _httpClientFactory.CreateClient(_clientType);

        request.Options.Set(HttpOptions.Region, Configuration.Region);
        request.Options.Set(HttpOptions.Service, Configuration.Service);
        request.Options.Set(HttpOptions.Target, target);
        request.Options.Set(HttpOptions.ApiVersion, Configuration.ApiVersion);

        using var logContext = Logger.BeginScope(new Dictionary<string, object>
        {
            ["Client"] = _clientType,
            ["Region"] = Configuration.Region,
            ["Service"] = Configuration.Service,
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

            if (!response.IsSuccessStatusCode)
            {
                var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(errorPayload))
                {
                    Logger.RequestFailed("No payload present");
                    return new ApiResponse<TResponse>(new ApiError("Request not successful.") { StatusCode = response.StatusCode });
                }

                var error = JsonSerializer.Deserialize(errorPayload, AwsServiceClientSerializationContext.Default.ApiError);
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
                    error = new ApiError("Failed to send request and unable to deserialize payload")  { StatusCode = response.StatusCode };
                }

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

                Logger.RequestFailed($"Type: {error?.Type ?? "Unknown"}, Message: {error?.Message ?? "Unknown"}, Code: {error?.Code ?? "Unknown"}");
                return new ApiResponse<TResponse>(error ?? DeserializationError);
            }

            var contentPayload = await response.Content.ReadAsStringAsync(cancellationToken);

            // Capture response headers
            var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);

            Debug.WriteLine("RESPONSE: " + contentPayload);

            if (typeof(TResponse) == typeof(string))
            {
                return new ApiResponse<TResponse>(contentPayload as TResponse, headers);
            }

            if (string.IsNullOrWhiteSpace(contentPayload))
            {
                return new ApiResponse<TResponse>(default(TResponse), headers);
            }

            var result = JsonSerializer.Deserialize(contentPayload, responseTypeInfo);
            return result is null ? new ApiResponse<TResponse>(DeserializationError) : new ApiResponse<TResponse>(result, headers);
        }
        catch (Exception e)
        {
            Logger.RequestFailed(e);
            throw;
        }
    }

    /// <summary>
    /// Creates and sends an HTTP request with a serialized request body asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response.</typeparam>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The API endpoint to target.</param>
    /// <param name="request">The request object to serialize and send.</param>
    /// <param name="requestTypeInfo">JSON serialization metadata for the request type.</param>
    /// <param name="responseTypeInfo">JSON serialization metadata for the response type.</param>
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="headers">Additional headers to add to the request</param>
    /// <returns>An API response containing either the deserialized response or error information.</returns>
    protected Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string requestUri, TRequest request, JsonTypeInfo<TRequest> requestTypeInfo, JsonTypeInfo<TResponse> responseTypeInfo, string target, CancellationToken cancellationToken, Dictionary<string, string>? headers = null)
        where TResponse : class
    {
        var requestMessage = CreateRequestMessage(method, requestUri, headers);

        if (method != HttpMethod.Get)
        {
            if (request is not string payload || !(payload.StartsWith('{') || payload.StartsWith('[')))
            {
                payload = JsonSerializer.Serialize(request, requestTypeInfo);
            }

            Debug.WriteLine("REQUEST: " + payload);
            requestMessage.Content = new StringContent(payload, Encoding.UTF8, new MediaTypeHeaderValue("application/x-amz-json-1.0", null));
            requestMessage.Options.Set(HttpOptions.Payload, payload);
        }

        return SendAsync(requestMessage, responseTypeInfo, target, cancellationToken);
    }

    /// <summary>
    /// Creates a HTTPRequestMessage for the given URI / Method
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="requestUri">The API endpoint to target.</param>
    /// <param name="headers">Additional headers to add to the request</param>
    /// <returns>A correctly formatted HTTPRequestMessage ready to accept content</returns>
    protected HttpRequestMessage CreateRequestMessage(HttpMethod method, string requestUri, Dictionary<string, string>? headers = null)
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

        if (headers != null)
            foreach(var header in  headers)
                requestMessage.Headers.Add(header.Key, header.Value);

        return requestMessage;
    }

    // TODO :: SendAsync -> SendJsonAsync
    // TODO :: new SendXML
    // TODO :: Refactor send methods so that they don't take jsontypeinfo
}
