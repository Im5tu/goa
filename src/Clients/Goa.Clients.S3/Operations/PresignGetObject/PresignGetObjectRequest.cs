namespace Goa.Clients.S3.Operations.PresignGetObject;

/// <summary>
/// Request to generate a pre-signed URL for downloading an object with a GET request.
/// </summary>
public sealed class PresignGetObjectRequest
{
    /// <summary>
    /// The name of the bucket containing the object.
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    /// The key of the object to download.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// How long the generated URL remains valid. Defaults to 5 minutes; clamped to the SigV4 maximum of 7 days.
    /// </summary>
    public TimeSpan Expiry { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When set, adds a signed <c>response-content-type</c> override so S3 returns this
    /// <c>Content-Type</c> header on the download.
    /// </summary>
    public string? ResponseContentType { get; init; }

    /// <summary>
    /// When set, adds a signed <c>response-content-disposition</c> override so S3 returns this
    /// <c>Content-Disposition</c> header on the download (e.g. to force a filename).
    /// </summary>
    public string? ResponseContentDisposition { get; init; }
}
