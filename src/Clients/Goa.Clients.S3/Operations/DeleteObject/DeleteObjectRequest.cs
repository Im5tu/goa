namespace Goa.Clients.S3.Operations.DeleteObject;

/// <summary>
/// Request for the DeleteObject operation.
/// </summary>
public sealed class DeleteObjectRequest
{
    /// <summary>
    /// The name of the bucket containing the object.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// The key of the object to delete.
    /// </summary>
    public required string Key { get; set; }
}
