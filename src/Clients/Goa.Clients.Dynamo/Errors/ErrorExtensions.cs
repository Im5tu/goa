using ErrorOr;

namespace Goa.Clients.Dynamo.Errors;

/// <summary>
/// Extension methods for ErrorOr types to provide friendly error checking for DynamoDB operations.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Checks if the error is a Goa DynamoDB-related error.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is DynamoDB-related, false otherwise.</returns>
    public static bool IsDynamoError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e => e.Code.StartsWith("Goa.DynamoDb.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if the error is related to concurrency or conditional check failures.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is concurrency-related, false otherwise.</returns>
    public static bool IsDynamoConcurrencyError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.ConditionalCheckFailedException ||
            e.Code == DynamoErrorCodes.ConditionalCheckFailed ||
            e.Code == DynamoErrorCodes.TransactionConflictException ||
            e.Code == DynamoErrorCodes.TransactionConflict ||
            e.Code == DynamoErrorCodes.ReplicatedWriteConflictException ||
            (e.Code == DynamoErrorCodes.ValidationException && e.Description.Contains("condition", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Checks if the error indicates an item or resource was not found.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error indicates not found, false otherwise.</returns>
    public static bool IsDynamoItemNotFoundError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.ResourceNotFoundException ||
            e.Code == DynamoErrorCodes.ItemNotFound ||
            e.Code == DynamoErrorCodes.NotFound ||
            e.Code == DynamoErrorCodes.TableNotFoundException ||
            e.Code == DynamoErrorCodes.TableNotFound ||
            e.Code == DynamoErrorCodes.IndexNotFoundException ||
            e.Code == DynamoErrorCodes.IndexNotFound ||
            e.Type == ErrorType.NotFound);
    }

    /// <summary>
    /// Checks if the error is related to throttling or capacity limits.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is throttling-related, false otherwise.</returns>
    public static bool IsDynamoThrottlingError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.ProvisionedThroughputExceededException ||
            e.Code == DynamoErrorCodes.ProvisionedThroughputExceeded ||
            e.Code == DynamoErrorCodes.RequestLimitExceeded ||
            e.Code == DynamoErrorCodes.ThrottlingException ||
            e.Code == DynamoErrorCodes.TooManyRequestsException ||
            e.Code == DynamoErrorCodes.TooManyRequests);
    }

    /// <summary>
    /// Checks if the error is a validation error indicating malformed requests or invalid parameters.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is validation-related, false otherwise.</returns>
    public static bool IsDynamoValidationError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.ValidationException ||
            e.Code == DynamoErrorCodes.InvalidParameterValueException ||
            e.Code == DynamoErrorCodes.InvalidParameterValue ||
            e.Code == DynamoErrorCodes.MissingParameterException ||
            e.Code == DynamoErrorCodes.MissingParameter ||
            e.Type == ErrorType.Validation);
    }

    /// <summary>
    /// Checks if the error is related to authentication or authorization.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is authentication/authorization-related, false otherwise.</returns>
    public static bool IsDynamoAuthError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.UnauthorizedException ||
            e.Code == DynamoErrorCodes.Unauthorized ||
            e.Code == DynamoErrorCodes.AccessDeniedException ||
            e.Code == DynamoErrorCodes.AccessDenied ||
            e.Code == DynamoErrorCodes.InvalidUserPoolConfigurationException ||
            e.Code == DynamoErrorCodes.InvalidUserPoolConfiguration ||
            e.Code == DynamoErrorCodes.NotAuthorizedException ||
            e.Code == DynamoErrorCodes.NotAuthorized ||
            e.Type == ErrorType.Unauthorized);
    }

    /// <summary>
    /// Checks if the error indicates a temporary failure that might succeed on retry.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is retryable, false otherwise.</returns>
    public static bool IsDynamoRetryableError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.IsDynamoThrottlingError() ||
               errorOr.Errors.Any(e =>
                   e.Code == DynamoErrorCodes.InternalServerError ||
                   e.Code == DynamoErrorCodes.ServiceUnavailable ||
                   e.Code == DynamoErrorCodes.RequestTimeoutException ||
                   e.Code == DynamoErrorCodes.RequestTimeout ||
                   (e.Type == ErrorType.Failure && !e.Description.Contains("permanent", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Checks if the error is related to resource state issues (resource in use).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is resource state-related, false otherwise.</returns>
    public static bool IsDynamoResourceStateError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == DynamoErrorCodes.ResourceInUseException ||
            e.Code == DynamoErrorCodes.ResourceInUse);
    }

    /// <summary>
    /// Gets the DynamoDB error code from the first error, if available.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>The DynamoDB error code, or null if no error found.</returns>
    public static string? GetErrorCode<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return null;
        return errorOr.FirstError.Code;
    }

    /// <summary>
    /// Gets the error description from the first error, if available.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>The error description, or null if no error found.</returns>
    public static string? GetErrorDescription<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return null;
        return errorOr.FirstError.Description;
    }
}
