using ErrorOr;

namespace Goa.Clients.Bedrock.Errors;

/// <summary>
/// Extension methods for ErrorOr types to provide friendly error checking for Bedrock operations.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Checks if the error is a Goa Bedrock-related error.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is Bedrock-related, false otherwise.</returns>
    public static bool IsBedrockError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e => e.Code.StartsWith("Goa.Bedrock.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Checks if the error is related to throttling or quota limits.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is throttling-related, false otherwise.</returns>
    public static bool IsBedrockThrottlingError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == BedrockErrorCodes.ThrottlingException ||
            e.Code == BedrockErrorCodes.ServiceQuotaExceededException);
    }

    /// <summary>
    /// Checks if the error is a validation error indicating malformed requests or invalid parameters.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is validation-related, false otherwise.</returns>
    public static bool IsBedrockValidationError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == BedrockErrorCodes.ValidationException ||
            e.Type == ErrorType.Validation);
    }

    /// <summary>
    /// Checks if the error is related to model issues (model error, not ready, or timeout).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is model-related, false otherwise.</returns>
    public static bool IsBedrockModelError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.Errors.Any(e =>
            e.Code == BedrockErrorCodes.ModelErrorException ||
            e.Code == BedrockErrorCodes.ModelNotReadyException ||
            e.Code == BedrockErrorCodes.ModelTimeoutException);
    }

    /// <summary>
    /// Checks if the error indicates a temporary failure that might succeed on retry.
    /// Includes throttling errors and model timeout errors.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="errorOr">The ErrorOr instance to check.</param>
    /// <returns>True if the error is retryable, false otherwise.</returns>
    public static bool IsBedrockRetryableError<T>(this ErrorOr<T> errorOr)
    {
        if (!errorOr.IsError) return false;
        return errorOr.IsBedrockThrottlingError() ||
               errorOr.Errors.Any(e => e.Code == BedrockErrorCodes.ModelTimeoutException);
    }
}
