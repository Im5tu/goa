namespace Goa.Functions.ApiGateway.Core.Payloads.V1;

/// <summary>
/// Represents the request payload for AWS API Gateway Proxy integration (V1).
/// </summary>
public class ProxyPayloadV1Request
{
    /// <summary>
    ///     Gets or sets the resource path of the API Gateway request.
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    ///     Gets or sets the full path of the request.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP method used in the request (e.g., GET, POST).
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    ///     Gets or sets the request headers as a dictionary.
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the request headers as a multi-value dictionary, allowing for headers that can have multiple values.
    /// </summary>
    public IDictionary<string, IList<string>>? MultiValueHeaders { get; set; }

    /// <summary>
    ///     Gets or sets the query string parameters as a dictionary of single values.
    /// </summary>
    public IDictionary<string, string>? QueryStringParameters { get; set; }

    /// <summary>
    ///     Gets or sets the query string parameters as a multi-value dictionary.
    /// </summary>
    public IDictionary<string, IList<string>>? MultiValueQueryStringParameters { get; set; }

    /// <summary>
    ///     Gets or sets the path parameters extracted from the request path.
    /// </summary>
    public IDictionary<string, string>? PathParameters { get; set; }

    /// <summary>
    ///     Gets or sets the stage variables defined for the API Gateway stage.
    /// </summary>
    public IDictionary<string, string>? StageVariables { get; set; }

    /// <summary>
    ///     Gets or sets the request context, which includes information such as request ID, authorizer, and other metadata.
    /// </summary>
    public ProxyPayloadV1RequestContext? RequestContext { get; set; }

    /// <summary>
    ///     Gets or sets the body of the request.
    ///     This is typically a JSON string, but can also be other content types like form data or plain text.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the body is Base64-encoded.
    ///     This is required when the body contains binary data.
    /// </summary>
    public bool IsBase64Encoded { get; set; }
}
