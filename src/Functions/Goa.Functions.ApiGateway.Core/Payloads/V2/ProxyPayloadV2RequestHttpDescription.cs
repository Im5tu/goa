namespace Goa.Functions.ApiGateway.Core.Payloads.V2;

/// <summary>
///     Represents the HTTP request details for an AWS API Gateway Proxy (V2) request, including the method, path, protocol, source IP, and user agent.
/// </summary>
public class ProxyPayloadV2RequestHttpDescription
{
    /// <summary>
    ///     Gets or sets the HTTP method used in the request (e.g., GET, POST).
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    ///     Gets or sets the full path of the request.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    ///     Gets or sets the HTTP protocol used in the request (e.g., HTTP/1.1, HTTP/2).
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    ///     Gets or sets the source IP address of the client making the request.
    /// </summary>
    public string? SourceIp { get; set; }

    /// <summary>
    ///     Gets or sets the user agent string of the client making the request.
    /// </summary>
    public string? UserAgent { get; set; }
}

