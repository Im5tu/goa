using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the validity period of a client certificate
/// </summary>
public class CertificateValidity
{
    /// <summary>
    /// Gets or sets the start date of the validity period
    /// </summary>
    [JsonPropertyName("notBefore")]
    public string? NotBefore { get; set; }

    /// <summary>
    /// Gets or sets the end date of the validity period
    /// </summary>
    [JsonPropertyName("notAfter")]
    public string? NotAfter { get; set; }
}