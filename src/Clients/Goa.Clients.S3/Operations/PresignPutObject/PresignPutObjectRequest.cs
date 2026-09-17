namespace Goa.Clients.S3.Operations.PresignPutObject;

/// <summary>
/// Request to generate a pre-signed URL for uploading an object with a PUT request.
/// </summary>
public sealed class PresignPutObjectRequest
{
    /// <summary>
    /// The name of the bucket to upload the object to.
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    /// The key the object will be stored under.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// How long the generated URL remains valid. Defaults to 5 minutes; clamped to the SigV4 maximum of 7 days.
    /// </summary>
    public TimeSpan Expiry { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When set, signs a <c>content-length</c> header binding the upload to this exact byte count.
    /// The caller MUST send a matching <c>Content-Length</c> header, and S3 rejects a body of any
    /// other size, which prevents oversized uploads through the pre-signed URL.
    /// </summary>
    public long? ContentLength { get; init; }

    /// <summary>
    /// When set, signs a <c>content-type</c> header. The caller MUST send a matching
    /// <c>Content-Type</c> header when uploading.
    /// </summary>
    public string? ContentType { get; init; }
}
