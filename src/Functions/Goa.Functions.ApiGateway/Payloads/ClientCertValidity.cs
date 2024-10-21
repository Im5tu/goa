namespace Goa.Functions.ApiGateway.Payloads;

/// <summary>
///     Represents the validity period of a client certificate, specifying the time frame during which the certificate is valid.
/// </summary>
public class ClientCertValidity
{
    /// <summary>
    ///     Gets or sets the start time from which the client certificate is valid.
    /// </summary>
    public string? NotBefore { get; set; }

    /// <summary>
    ///     Gets or sets the end time after which the client certificate is no longer valid.
    /// </summary>
    public string? NotAfter { get; set; }
}
