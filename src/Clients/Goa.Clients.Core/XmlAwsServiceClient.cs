using Goa.Clients.Core.Configuration;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;

namespace Goa.Clients.Core;

/// <summary>
/// AWS service client that handles XML serialization/deserialization.
/// </summary>
/// <typeparam name="T">The configuration type that extends AwsServiceConfiguration.</typeparam>
public abstract class XmlAwsServiceClient<T> : AwsServiceClient<T> where T : AwsServiceConfiguration
{
    private readonly ApiError _deserializationError = new("Failed to deserialize response", "DeserializationError");

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
    /// Sends an XML request and deserializes the XML response.
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
    protected async Task<ApiResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpMethod method, string requestUri, object? request, string target, CancellationToken cancellationToken, Dictionary<string, string>? headers = null)
        where TRequest : class, ISerializeToXml
        where TResponse : class, IDeserializeFromXml, new()
    {
        byte[]? content = null;
        if (request != null && method != HttpMethod.Get)
        {
            if (request is string stringPayload && IsXmlSerialized(stringPayload))
            {
                content = Encoding.UTF8.GetBytes(stringPayload);
            }
            else
            {
                content = SerializeToXmlBytes(request);
            }
        }

        var requestMessage = CreateRequestMessage(method, requestUri + $"?Action={UrlEncoder.Default.Encode(target)}", content, new MediaTypeHeaderValue("application/xml"), headers);
        var response = await SendAsync(requestMessage, target, cancellationToken);

        return await ProcessXmlResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// Processes an HTTP response and converts it to an API response with XML deserialization.
    /// </summary>
    private async Task<ApiResponse<TResponse>> ProcessXmlResponseAsync<TResponse>(HttpResponseMessage response)
        where TResponse : class, IDeserializeFromXml, new()
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

            var error = DeserializeXmlError(errorPayload);
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
            return new ApiResponse<TResponse>(error ?? _deserializationError);
        }

        var contentPayload = await response.Content.ReadAsStringAsync();
        Debug.WriteLine("RESPONSE: " + contentPayload);

        // Handle string responses specially
        if (typeof(TResponse) == typeof(string))
        {
            return new ApiResponse<TResponse>(contentPayload as TResponse, headers);
        }

        if (string.IsNullOrWhiteSpace(contentPayload))
        {
            return new ApiResponse<TResponse>(default(TResponse), headers);
        }

        var result = new TResponse();
        result.DeserializeFromXml(contentPayload);
        return new ApiResponse<TResponse>(result, headers);
    }

    /// <summary>
    /// Applies AWS-specific error header processing to the error object.
    /// </summary>
    private ApiError ProcessAwsErrorHeaders(HttpResponseMessage response, ApiError error)
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
    /// Serializes a request object to UTF-8 bytes using the ISerializeToXml interface.
    /// </summary>
    private static byte[] SerializeToXmlBytes(object request)
    {
        if (request is ISerializeToXml xmlSerializable)
        {
            return Encoding.UTF8.GetBytes(xmlSerializable.SerializeToXml());
        }

        throw new InvalidOperationException($"Cannot serialize request of type {request.GetType().Name}. Please implement ISerializeToXml interface.");
    }

    /// <summary>
    /// Deserializes XML error response content to an ApiError.
    /// </summary>
    private ApiError? DeserializeXmlError(string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            var xmlError = new XmlApiError();
            xmlError.DeserializeFromXml(content);
            return xmlError.ToApiError();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to deserialize XML error response: {Content}", content);
            return null;
        }
    }

    /// <summary>
    /// Determines if the given string is already in XML format.
    /// </summary>
    private static bool IsXmlSerialized(string input)
    {
        return input.TrimStart().StartsWith('<');
    }
}