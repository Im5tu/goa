namespace Goa.Clients.EventBridge.Errors;

/// <summary>
/// Common error codes returned by EventBridge operations.
/// </summary>
public static class EventBridgeErrorCodes
{
    /// <summary>
    /// You do not have sufficient access to perform this action.
    /// </summary>
    public const string AccessDenied = "AccessDeniedException";

    /// <summary>
    /// The request processing has failed because of an unknown error, exception or failure.
    /// </summary>
    public const string InternalFailure = "InternalFailure";

    /// <summary>
    /// The request was denied due to request throttling.
    /// </summary>
    public const string Throttling = "ThrottlingException";

    /// <summary>
    /// The account ID provided is not valid.
    /// </summary>
    public const string InvalidAccountId = "InvalidAccountIdException";

    /// <summary>
    /// A specified parameter is not valid.
    /// </summary>
    public const string InvalidArgument = "InvalidArgument";

    /// <summary>
    /// The JSON provided is not valid.
    /// </summary>
    public const string MalformedDetail = "MalformedDetail";

    /// <summary>
    /// Redacting the CloudTrail event failed.
    /// </summary>
    public const string RedactionFailure = "RedactionFailure";

    /// <summary>
    /// You do not have permissions to publish events with this source onto this event bus.
    /// </summary>
    public const string NotAuthorizedForSource = "NotAuthorizedForSourceException";

    /// <summary>
    /// You do not have permissions to publish events with this detail type onto this event bus.
    /// </summary>
    public const string NotAuthorizedForDetailType = "NotAuthorizedForDetailTypeException";
}