namespace Goa.Functions.ApiGateway;

/// <summary>
/// Represents the request payload for AWS API Gateway Proxy integration (V2).
/// </summary>
public class ProxyPayloadV2Request
{
    /// <summary>
    ///     Gets or sets the route key, which defines the route in the API Gateway that matched the request.
    /// </summary>
    public string? RouteKey { get; set; }

    /// <summary>
    ///     Gets or sets the raw path of the request.
    /// </summary>
    public string? RawPath { get; set; }

    /// <summary>
    ///     Gets or sets the raw query string from the request URL.
    /// </summary>
    public string? RawQueryString { get; set; }

    /// <summary>
    ///     Gets or sets the cookies included in the request.
    /// </summary>
    public IEnumerable<string>? Cookies { get; set; }

    /// <summary>
    ///     Gets or sets the headers included in the request.
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the query string parameters as a dictionary of single values.
    /// </summary>
    public IDictionary<string, string>? QueryStringParameters { get; set; }

    /// <summary>
    ///     Gets or sets the request context, which includes information such as request ID, authorizer, and other metadata.
    /// </summary>
    public ProxyPayloadV2RequestContext? RequestContext { get; set; }

    /// <summary>
    ///     Gets or sets the body of the request.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    ///     Gets or sets the path parameters extracted from the request path.
    /// </summary>
    public IDictionary<string, string>? PathParameters { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the body is Base64-encoded.
    ///     This is required when the body contains binary data.
    /// </summary>
    public bool IsBase64Encoded { get; set; }

    /// <summary>
    ///     Gets or sets the stage variables defined for the API Gateway stage.
    /// </summary>
    public IDictionary<string, string>? StageVariables { get; set; }
}

