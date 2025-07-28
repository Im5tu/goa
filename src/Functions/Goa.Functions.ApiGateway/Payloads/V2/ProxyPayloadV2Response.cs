namespace Goa.Functions.ApiGateway.Payloads.V2;

/// <summary>
///     Represents the response payload for AWS API Gateway Proxy V2 Payload
/// </summary>
public class ProxyPayloadV2Response
{
    /// <summary>
    ///     Gets or sets the HTTP status code to return in the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    ///     Gets or sets the headers to include in the HTTP response.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the cookies to include in the response.
    /// </summary>
    public IEnumerable<string>? Cookies { get; set; }

    /// <summary>
    ///     Gets or sets the body content of the HTTP response.
    ///     This is typically a JSON string, but can also be plain text or other content types.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the body is Base64-encoded.
    ///     This is required when returning binary data in the response.
    /// </summary>
    public bool IsBase64Encoded { get; set; }
}
