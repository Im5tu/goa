namespace Goa.Clients.Sns.Errors;

/// <summary>
/// Common error codes returned by SNS operations.
/// </summary>
public static class SnsErrorCodes
{
    /// <summary>
    /// Indicates that the requested resource does not exist.
    /// </summary>
    public const string NotFound = "NotFoundException";

    /// <summary>
    /// Indicates an invalid parameter was used in the request.
    /// </summary>
    public const string InvalidParameter = "InvalidParameterException";

    /// <summary>
    /// Indicates an invalid parameter value was used in the request.
    /// </summary>
    public const string InvalidParameterValue = "InvalidParameterValueException";

    /// <summary>
    /// Indicates an internal service error.
    /// </summary>
    public const string InternalError = "InternalErrorException";

    /// <summary>
    /// Indicates that the endpoint is disabled.
    /// </summary>
    public const string EndpointDisabled = "EndpointDisabledException";

    /// <summary>
    /// Indicates that the platform application is disabled.
    /// </summary>
    public const string PlatformApplicationDisabled = "PlatformApplicationDisabledException";

    /// <summary>
    /// Indicates an authorization error.
    /// </summary>
    public const string AuthorizationError = "AuthorizationErrorException";

    /// <summary>
    /// Indicates that KMS is disabled for the topic.
    /// </summary>
    public const string KMSDisabled = "KMSDisabledException";

    /// <summary>
    /// Indicates an invalid KMS key state.
    /// </summary>
    public const string KMSInvalidState = "KMSInvalidStateException";

    /// <summary>
    /// Indicates that the KMS key was not found.
    /// </summary>
    public const string KMSNotFound = "KMSNotFoundException";

    /// <summary>
    /// Indicates that KMS key usage is opt-in required.
    /// </summary>
    public const string KMSOptInRequired = "KMSOptInRequiredException";

    /// <summary>
    /// Indicates that access to KMS is denied.
    /// </summary>
    public const string KMSAccessDenied = "KMSAccessDeniedException";

    /// <summary>
    /// Indicates that KMS key usage is throttled.
    /// </summary>
    public const string KMSThrottling = "KMSThrottlingException";

    /// <summary>
    /// Indicates that the request was throttled.
    /// </summary>
    public const string Throttled = "ThrottledException";

    /// <summary>
    /// Two or more batch entries in the request have the same ID.
    /// </summary>
    public const string BatchEntryIdsNotDistinct = "BatchEntryIdsNotDistinctException";

    /// <summary>
    /// The batch request contains more entries than permissible.
    /// </summary>
    public const string TooManyEntriesInBatchRequest = "TooManyEntriesInBatchRequestException";

    /// <summary>
    /// The batch request doesn't contain any entries.
    /// </summary>
    public const string EmptyBatchRequest = "EmptyBatchRequestException";

    /// <summary>
    /// The Id of a batch entry in a batch request doesn't abide by the specification.
    /// </summary>
    public const string InvalidBatchEntryId = "InvalidBatchEntryIdException";

    /// <summary>
    /// The length of all the batch messages put together is more than the limit.
    /// </summary>
    public const string BatchRequestTooLong = "BatchRequestTooLongException";
}