namespace Goa.Clients.S3.Operations.HeadObject;

/// <summary>
/// Response from the HeadObject operation.
/// </summary>
public sealed class HeadObjectResponse
{
    /// <summary>
    /// The MIME content type of the object.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// The size of the object in bytes.
    /// </summary>
    public long ContentLength { get; set; }

    /// <summary>
    /// The entity tag of the object, as returned by S3 (including surrounding quotes).
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    /// The date and time the object was last modified.
    /// </summary>
    public DateTimeOffset? LastModified { get; set; }

    /// <summary>
    /// User-defined metadata stored with the object ("x-amz-meta-*" headers, prefix stripped), if any.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}
