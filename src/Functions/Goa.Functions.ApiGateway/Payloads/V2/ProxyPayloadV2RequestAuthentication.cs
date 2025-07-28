namespace Goa.Functions.ApiGateway.Payloads.V2;

/// <summary>
/// Represents the authentication details in an AWS API Gateway Proxy (V2) request, including the client certificate used for the request.
/// </summary>
public class ProxyPayloadV2RequestAuthentication
{
    /// <summary>
    ///     Gets or sets the client certificate information used in the request, if applicable.
    /// </summary>
    public ProxyRequestClientCert? ClientCert { get; set; }
}
