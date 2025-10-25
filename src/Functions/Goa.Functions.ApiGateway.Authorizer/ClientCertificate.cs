using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents client certificate information for mutual TLS
/// </summary>
public class ClientCertificate
{
    /// <summary>
    /// Gets or sets the PEM-encoded client certificate
    /// </summary>
    [JsonPropertyName("clientCertPem")]
    public string? ClientCertPem { get; set; }

    /// <summary>
    /// Gets or sets the distinguished name of the subject
    /// </summary>
    [JsonPropertyName("subjectDN")]
    public string? SubjectDN { get; set; }

    /// <summary>
    /// Gets or sets the distinguished name of the issuer
    /// </summary>
    [JsonPropertyName("issuerDN")]
    public string? IssuerDN { get; set; }

    /// <summary>
    /// Gets or sets the serial number of the certificate
    /// </summary>
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets the validity period of the certificate
    /// </summary>
    [JsonPropertyName("validity")]
    public CertificateValidity? Validity { get; set; }
}