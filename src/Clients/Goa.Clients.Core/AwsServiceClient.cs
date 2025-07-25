using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Serialization;

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
    /// Gets the content type for requests.
    /// </summary>
    protected abstract MediaTypeHeaderValue GetContentType();

    /// <summary>
    /// Serializes a request object to a string.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <param name="request">The request object to serialize.</param>
    /// <returns>The serialized request as a string.</returns>
    protected abstract string SerializeRequest<TRequest>(TRequest request);

    /// <summary>
    /// Deserializes response content to the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize to.</typeparam>
    /// <param name="content">The response content to deserialize.</param>
    /// <returns>The deserialized response object.</returns>
    protected abstract TResponse? DeserializeResponse<TResponse>(string content) where TResponse : class;

    /// <summary>
    /// Deserializes error response content to an ApiError.
    /// </summary>
    /// <param name="content">The error response content to deserialize.</param>
    /// <returns>The deserialized error object.</returns>
    protected abstract ApiError? DeserializeError(string content);

    /// <summary>
    /// Determines if the given string is already in the expected serialized format.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the string is already serialized, false otherwise.</returns>
    protected abstract bool IsAlreadySerialized(string input);

    /// <summary>
    /// Sends an HTTP request asynchronously and deserializes the response.
    /// </summary>
    /// <typeparam name="TResponse">The type of the expected response.</typeparam>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An API response containing either the deserialized response or error information.</returns>
    protected async Task<ApiResponse<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, string target, CancellationToken cancellationToken)
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

                var error = DeserializeError(errorPayload);
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

            var result = DeserializeResponse<TResponse>(contentPayload);
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
    /// <param name="target">The target operation name.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <param name="headers">Additional headers to add to the request</param>
    /// <returns>An API response containing either the deserialized response or error information.</returns>
    protected Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string requestUri, TRequest request, string target, CancellationToken cancellationToken, Dictionary<string, string>? headers = null)
        where TResponse : class
    {
        var requestMessage = CreateRequestMessage(method, requestUri, headers);

        if (method != HttpMethod.Get)
        {
            string payload;
            if (request is string stringPayload && IsAlreadySerialized(stringPayload))
            {
                payload = stringPayload;
            }
            else
            {
                payload = SerializeRequest(request);
            }

            Debug.WriteLine("REQUEST: " + payload);
            requestMessage.Content = new StringContent(payload, Encoding.UTF8, GetContentType());
            requestMessage.Options.Set(HttpOptions.Payload, payload);
        }

        return SendAsync<TResponse>(requestMessage, target, cancellationToken);
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
}

/// <summary>
/// AWS service client that handles JSON serialization/deserialization.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class JsonAwsServiceClient<T> : AwsServiceClient<T> where T : AwsServiceConfiguration
{
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
    /// Gets the content type for JSON requests.
    /// </summary>
    protected override MediaTypeHeaderValue GetContentType()
    {
        return new MediaTypeHeaderValue("application/x-amz-json-1.0", null);
    }

    /// <summary>
    /// Serializes a request object to JSON.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <param name="request">The request object to serialize.</param>
    /// <returns>The serialized request as JSON string.</returns>
    protected override string SerializeRequest<TRequest>(TRequest request)
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(TRequest)) as JsonTypeInfo<TRequest>;
        if (typeInfo is null)
            throw new Exception($"Cannot find type {typeof(TRequest).Name} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Serialize(request, typeInfo);
    }

    /// <summary>
    /// Deserializes JSON response content to the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize to.</typeparam>
    /// <param name="content">The JSON response content to deserialize.</param>
    /// <returns>The deserialized response object.</returns>
    protected override TResponse? DeserializeResponse<TResponse>(string content) where TResponse : class
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(TResponse)) as JsonTypeInfo<TResponse>;
        if (typeInfo is null)
            throw new Exception($"Cannot find type {typeof(TResponse).Name} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Deserialize<TResponse>(content, typeInfo);
    }

    /// <summary>
    /// Deserializes JSON error response content to an ApiError.
    /// </summary>
    /// <param name="content">The JSON error response content to deserialize.</param>
    /// <returns>The deserialized error object.</returns>
    protected override ApiError? DeserializeError(string content)
    {
        var typeInfo = _jsonSerializerContext.GetTypeInfo(typeof(ApiError)) as JsonTypeInfo<ApiError>;
        if (typeInfo is null)
            throw new Exception($"Cannot find type {nameof(ApiError)} in serialization context {_jsonSerializerContext.GetType().FullName}");

        return JsonSerializer.Deserialize<ApiError>(content, typeInfo);
    }

    /// <summary>
    /// Determines if the given string is already in JSON format.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the string starts with '{' or '[', false otherwise.</returns>
    protected override bool IsAlreadySerialized(string input)
    {
        var trimmed = input.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }
}

#pragma warning disable IL2026
#pragma warning disable IL3050

/// <summary>
/// AWS service client that handles XML serialization/deserialization.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class XmlAwsServiceClient<T> : AwsServiceClient<T> where T : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the XmlAwsServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the AWS service.</param>
    protected XmlAwsServiceClient(IHttpClientFactory httpClientFactory, ILogger logger, T configuration)
        : base(httpClientFactory, logger, configuration)
    {
    }

    /// <summary>
    /// Gets the content type for XML requests.
    /// </summary>
    protected override MediaTypeHeaderValue GetContentType()
    {
        return new MediaTypeHeaderValue("application/xml", null);
    }

    /// <summary>
    /// Serializes a request object to XML.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request object.</typeparam>
    /// <param name="request">The request object to serialize.</param>
    /// <returns>The serialized request as XML string.</returns>
    protected override string SerializeRequest<TRequest>(TRequest request)
    {
        var xmlSerializer = new XmlSerializer(typeof(TRequest));
        using var writer = new StringWriter();
        xmlSerializer.Serialize(writer, request);
        return writer.ToString();
    }

    /// <summary>
    /// Deserializes XML response content to the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type to deserialize to.</typeparam>
    /// <param name="content">The XML response content to deserialize.</param>
    /// <returns>The deserialized response object.</returns>
    protected override TResponse? DeserializeResponse<TResponse>(string content) where TResponse : class
    {
        var xmlSerializer = new XmlSerializer(typeof(TResponse));
        using var reader = new StringReader(content);
        return (TResponse?)xmlSerializer.Deserialize(reader);
    }

    /// <summary>
    /// Deserializes XML error response content to an ApiError.
    /// </summary>
    /// <param name="content">The XML error response content to deserialize.</param>
    /// <returns>The deserialized error object.</returns>
    protected override ApiError? DeserializeError(string content)
    {
        try
        {
            var xmlSerializer = new XmlSerializer(typeof(ApiError));
            using var reader = new StringReader(content);
            return (ApiError?)xmlSerializer.Deserialize(reader);
        }
        catch (Exception ex)
        {
            // For pure XML services, if XML deserialization fails,
            // it's likely a parsing issue, not a different format
            Logger.LogWarning(ex, "Failed to deserialize XML error response: {Content}", content);
            return null;
        }
    }

    /// <summary>
    /// Determines if the given string is already in XML format.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the string starts with '&lt;', false otherwise.</returns>
    protected override bool IsAlreadySerialized(string input)
    {
        return input.TrimStart().StartsWith('<');
    }
}
