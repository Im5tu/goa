namespace Goa.Clients.S3.Operations.HeadObject;

/// <summary>
/// Request for the HeadObject operation.
/// </summary>
public sealed class HeadObjectRequest
{
    /// <summary>
    /// The name of the bucket containing the object.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// The key of the object to inspect.
    /// </summary>
    public required string Key { get; set; }
}
