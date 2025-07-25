namespace Goa.Clients.Dynamo.Errors;

/// <summary>
/// Static class containing all DynamoDB error codes with the Goa.DynamoDb prefix for performance and consistency.
/// </summary>
public static class DynamoErrorCodes
{
    // AWS DynamoDB Service Errors
    internal static readonly string ConditionalCheckFailed = "Goa.DynamoDb.ConditionalCheckFailed";
    internal static readonly string ConditionalCheckFailedException = "Goa.DynamoDb.ConditionalCheckFailedException";
    internal static readonly string ProvisionedThroughputExceeded = "Goa.DynamoDb.ProvisionedThroughputExceeded";
    internal static readonly string ProvisionedThroughputExceededException = "Goa.DynamoDb.ProvisionedThroughputExceededException";
    internal static readonly string ResourceNotFoundException = "Goa.DynamoDb.ResourceNotFoundException";
    internal static readonly string ItemNotFound = "Goa.DynamoDb.ItemNotFound";
    internal static readonly string NotFound = "Goa.DynamoDb.NotFound";
    internal static readonly string RequestLimitExceeded = "Goa.DynamoDb.RequestLimitExceeded";
    internal static readonly string ThrottlingException = "Goa.DynamoDb.ThrottlingException";
    internal static readonly string TooManyRequests = "Goa.DynamoDb.TooManyRequests";
    internal static readonly string TooManyRequestsException = "Goa.DynamoDb.TooManyRequestsException";

    // Transaction Errors
    internal static readonly string TransactionConflict = "Goa.DynamoDb.TransactionConflict";
    internal static readonly string TransactionConflictException = "Goa.DynamoDb.TransactionConflictException";
    internal static readonly string TransactionCanceledException = "Goa.DynamoDb.TransactionCanceledException";
    internal static readonly string TransactionInProgressException = "Goa.DynamoDb.TransactionInProgressException";

    // Replication Errors
    internal static readonly string ReplicatedWriteConflictException = "Goa.DynamoDb.ReplicatedWriteConflictException";

    // Validation Errors
    internal static readonly string ValidationException = "Goa.DynamoDb.ValidationException";
    internal static readonly string InvalidParameterValue = "Goa.DynamoDb.InvalidParameterValue";
    internal static readonly string InvalidParameterValueException = "Goa.DynamoDb.InvalidParameterValueException";
    internal static readonly string MissingParameter = "Goa.DynamoDb.MissingParameter";
    internal static readonly string MissingParameterException = "Goa.DynamoDb.MissingParameterException";

    // Authorization Errors
    internal static readonly string Unauthorized = "Goa.DynamoDb.Unauthorized";
    internal static readonly string UnauthorizedException = "Goa.DynamoDb.UnauthorizedException";
    internal static readonly string AccessDenied = "Goa.DynamoDb.AccessDenied";
    internal static readonly string AccessDeniedException = "Goa.DynamoDb.AccessDeniedException";
    internal static readonly string NotAuthorized = "Goa.DynamoDb.NotAuthorized";
    internal static readonly string NotAuthorizedException = "Goa.DynamoDb.NotAuthorizedException";
    internal static readonly string InvalidUserPoolConfiguration = "Goa.DynamoDb.InvalidUserPoolConfiguration";
    internal static readonly string InvalidUserPoolConfigurationException = "Goa.DynamoDb.InvalidUserPoolConfigurationException";

    // Server Errors
    internal static readonly string InternalServerError = "Goa.DynamoDb.InternalServerError";
    internal static readonly string ServiceUnavailable = "Goa.DynamoDb.ServiceUnavailable";
    internal static readonly string RequestTimeout = "Goa.DynamoDb.RequestTimeout";
    internal static readonly string RequestTimeoutException = "Goa.DynamoDb.RequestTimeoutException";

    // Resource State Errors
    internal static readonly string ResourceInUse = "Goa.DynamoDb.ResourceInUse";
    internal static readonly string ResourceInUseException = "Goa.DynamoDb.ResourceInUseException";
    internal static readonly string TableNotFound = "Goa.DynamoDb.TableNotFound";
    internal static readonly string TableNotFoundException = "Goa.DynamoDb.TableNotFoundException";
    internal static readonly string IndexNotFound = "Goa.DynamoDb.IndexNotFound";
    internal static readonly string IndexNotFoundException = "Goa.DynamoDb.IndexNotFoundException";

    // Collection and Size Limit Errors
    internal static readonly string ItemCollectionSizeLimitExceededException = "Goa.DynamoDb.ItemCollectionSizeLimitExceededException";
}
