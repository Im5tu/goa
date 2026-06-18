namespace Goa.Clients.S3.Operations.PutObject;

/// <summary>
/// Response from the PutObject operation.
/// </summary>
public sealed class PutObjectResponse
{
    /// <summary>
    /// The entity tag of the uploaded object, as returned by S3 (including surrounding quotes).
    /// </summary>
    public string? ETag { get; set; }
}
