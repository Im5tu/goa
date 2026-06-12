namespace Goa.Clients.S3.Errors;

/// <summary>
/// Common error codes returned by S3 operations.
/// </summary>
public static class S3ErrorCodes
{
    /// <summary>
    /// The specified key does not exist.
    /// </summary>
    public const string NoSuchKey = "NoSuchKey";

    /// <summary>
    /// The specified bucket does not exist.
    /// </summary>
    public const string NoSuchBucket = "NoSuchBucket";

    /// <summary>
    /// Access to the resource is denied.
    /// </summary>
    public const string AccessDenied = "AccessDenied";

    /// <summary>
    /// The requested range is not satisfiable.
    /// </summary>
    public const string InvalidRange = "InvalidRange";

    /// <summary>
    /// The proposed upload exceeds the maximum allowed object size.
    /// </summary>
    public const string EntityTooLarge = "EntityTooLarge";

    /// <summary>
    /// At least one of the preconditions specified did not hold.
    /// </summary>
    public const string PreconditionFailed = "PreconditionFailed";

    /// <summary>
    /// The operation is not valid for the current state of the object.
    /// </summary>
    public const string InvalidObjectState = "InvalidObjectState";

    /// <summary>
    /// Reduce your request rate.
    /// </summary>
    public const string SlowDown = "SlowDown";

    /// <summary>
    /// The supplied object key is not valid (empty, contains dot-segments, or control characters).
    /// </summary>
    public const string InvalidKey = "S3.InvalidKey";

    /// <summary>
    /// The supplied bucket name does not satisfy S3 bucket naming rules.
    /// </summary>
    public const string InvalidBucketName = "S3.InvalidBucketName";
}
