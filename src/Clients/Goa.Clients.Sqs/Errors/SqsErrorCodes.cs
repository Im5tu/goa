namespace Goa.Clients.Sqs.Errors;

/// <summary>
/// Common error codes returned by SQS operations.
/// </summary>
public static class SqsErrorCodes
{
    /// <summary>
    /// The specified queue does not exist.
    /// </summary>
    public const string QueueDoesNotExist = "QueueDoesNotExist";

    /// <summary>
    /// The request was denied due to request throttling.
    /// </summary>
    public const string RequestThrottled = "RequestThrottled";

    /// <summary>
    /// The message contains invalid characters.
    /// </summary>
    public const string InvalidMessageContents = "InvalidMessageContents";

    /// <summary>
    /// The operation is not supported.
    /// </summary>
    public const string UnsupportedOperation = "UnsupportedOperation";

    /// <summary>
    /// The batch request contains more entries than permissible.
    /// </summary>
    public const string TooManyEntriesInBatchRequest = "TooManyEntriesInBatchRequest";

    /// <summary>
    /// The batch request doesn't contain any entries.
    /// </summary>
    public const string EmptyBatchRequest = "EmptyBatchRequest";

    /// <summary>
    /// Two or more batch entries in the request have the same ID.
    /// </summary>
    public const string BatchEntryIdsNotDistinct = "BatchEntryIdsNotDistinct";

    /// <summary>
    /// The length of all the messages put together is more than the limit.
    /// </summary>
    public const string BatchRequestTooLong = "BatchRequestTooLong";

    /// <summary>
    /// The Id of a batch entry in a batch request doesn't abide by the specification.
    /// </summary>
    public const string InvalidBatchEntryId = "InvalidBatchEntryId";

    /// <summary>
    /// The specified receipt handle isn't valid.
    /// </summary>
    public const string ReceiptHandleIsInvalid = "ReceiptHandleIsInvalid";

    /// <summary>
    /// The specified message isn't in flight.
    /// </summary>
    public const string MessageNotInflight = "MessageNotInflight";

    /// <summary>
    /// Access to the resource is denied.
    /// </summary>
    public const string AccessDenied = "AccessDenied";

    /// <summary>
    /// The action violates a limit.
    /// </summary>
    public const string OverLimit = "OverLimit";
}