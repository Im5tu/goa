using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the identity information in an authorizer request context
/// </summary>
public class AuthorizerIdentity
{
    /// <summary>
    /// Gets or sets the API key used for the request
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the source IP address of the request
    /// </summary>
    [JsonPropertyName("sourceIp")]
    public string? SourceIp { get; set; }

    /// <summary>
    /// Gets or sets the client certificate information (if using mutual TLS)
    /// </summary>
    [JsonPropertyName("clientCert")]
    public ClientCertificate? ClientCert { get; set; }
}