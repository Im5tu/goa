namespace Goa.Clients.S3.Operations.GetObject;

/// <summary>
/// Request for the GetObject operation.
/// </summary>
public sealed class GetObjectRequest
{
    /// <summary>
    /// The name of the bucket containing the object.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// The key of the object to retrieve.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The byte range of the object to retrieve (e.g. "bytes=0-63").
    /// </summary>
    public string? Range { get; set; }
}
