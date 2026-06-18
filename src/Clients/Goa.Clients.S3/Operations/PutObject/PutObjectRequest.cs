namespace Goa.Clients.S3.Operations.PutObject;

/// <summary>
/// Request for the PutObject operation.
/// </summary>
public sealed class PutObjectRequest
{
    /// <summary>
    /// The name of the bucket to which the object is uploaded.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// The key of the object to upload.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The object content to upload.
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; set; }

    /// <summary>
    /// The MIME content type of the object (e.g. "application/json").
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The server-side encryption algorithm to use when storing the object (e.g. "aws:kms").
    /// </summary>
    public string? ServerSideEncryption { get; set; }

    /// <summary>
    /// The AWS KMS key ID to use for object encryption when <see cref="ServerSideEncryption"/> is "aws:kms".
    /// </summary>
    public string? SseKmsKeyId { get; set; }

    /// <summary>
    /// User-defined metadata to store with the object. Each entry is sent as an "x-amz-meta-{key}" header.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
