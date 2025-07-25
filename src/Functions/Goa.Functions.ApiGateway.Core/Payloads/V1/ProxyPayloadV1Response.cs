namespace Goa.Functions.ApiGateway.Core.Payloads.V1;

/// <summary>
///     Represents the response payload for AWS API Gateway Proxy integration (V1).
/// </summary>
public class ProxyPayloadV1Response
{
    /// <summary>
    ///     Gets or sets the HTTP status code to return in the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    ///     Gets or sets the headers to include in the HTTP response.
    /// </summary>
    public IDictionary<string, string>? Headers { get; set; }

    /// <summary>
    ///     Gets or sets the headers as a multi-value dictionary, allowing for headers with multiple values.
    /// </summary>
    public IDictionary<string, IList<string>>? MultiValueHeaders { get; set; }

    /// <summary>
    ///     Gets or sets the body content of the HTTP response.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the body is Base64-encoded.
    /// </summary>
    public bool IsBase64Encoded { get; set; }
}
