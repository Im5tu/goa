namespace Goa.Clients.Bedrock.Errors;

/// <summary>
/// Static class containing all Bedrock error codes with the Goa.Bedrock prefix for performance and consistency.
/// </summary>
public static class BedrockErrorCodes
{
    // Authorization Errors
    internal static readonly string AccessDeniedException = "Goa.Bedrock.AccessDeniedException";

    // Model Errors
    internal static readonly string ModelErrorException = "Goa.Bedrock.ModelErrorException";
    internal static readonly string ModelNotReadyException = "Goa.Bedrock.ModelNotReadyException";
    internal static readonly string ModelTimeoutException = "Goa.Bedrock.ModelTimeoutException";

    // Resource Errors
    internal static readonly string ResourceNotFoundException = "Goa.Bedrock.ResourceNotFoundException";

    // Throttling and Quota Errors
    internal static readonly string ServiceQuotaExceededException = "Goa.Bedrock.ServiceQuotaExceededException";
    internal static readonly string ThrottlingException = "Goa.Bedrock.ThrottlingException";

    // Validation Errors
    internal static readonly string ValidationException = "Goa.Bedrock.ValidationException";
}
