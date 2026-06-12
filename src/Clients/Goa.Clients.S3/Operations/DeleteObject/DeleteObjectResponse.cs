namespace Goa.Clients.S3.Operations.DeleteObject;

/// <summary>
/// Response from the DeleteObject operation.
/// </summary>
public sealed class DeleteObjectResponse
{
    // DeleteObject returns an empty response on success. S3 responds with 204 No Content
    // even when the specified key does not exist on an unversioned bucket.
}
