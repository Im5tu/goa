namespace Goa.Functions.ApiGateway.Payloads;

/// <summary>
///     Represents the client certificate details for a request, including the certificate itself and its validity period.
/// </summary>
public class ProxyRequestClientCert
{
    /// <summary>
    ///     Gets or sets the PEM-encoded client certificate.
    /// </summary>
    public string? ClientCertPem { get; set; }

    /// <summary>
    ///     Gets or sets the subject distinguished name (DN) of the client certificate.
    /// </summary>
    public string? SubjectDN { get; set; }

    /// <summary>
    ///     Gets or sets the issuer distinguished name (DN) of the client certificate.
    /// </summary>
    public string? IssuerDN { get; set; }

    /// <summary>
    ///     Gets or sets the serial number of the client certificate.
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    ///     Gets or sets the validity period of the client certificate, including the start and end times.
    /// </summary>
    public ClientCertValidity? Validity { get; set; }
}
