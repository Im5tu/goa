namespace Goa.Clients.Lambda.Errors;

/// <summary>
/// Common error codes returned by Lambda operations.
/// </summary>
public static class LambdaErrorCodes
{
    /// <summary>
    /// The request processing has failed because of an unknown error, exception or failure.
    /// </summary>
    public const string InternalFailure = "InternalFailure";

    /// <summary>
    /// The request was denied due to request throttling.
    /// </summary>
    public const string TooManyRequests = "TooManyRequestsException";

    /// <summary>
    /// The specified function could not be found.
    /// </summary>
    public const string ResourceNotFound = "ResourceNotFoundException";

    /// <summary>
    /// One of the parameters in the request is not valid.
    /// </summary>
    public const string InvalidParameterValue = "InvalidParameterValueException";

    /// <summary>
    /// The request body could not be parsed as JSON.
    /// </summary>
    public const string InvalidRequestContent = "InvalidRequestContentException";

    /// <summary>
    /// The request payload compressed size is greater than 262144 bytes.
    /// </summary>
    public const string RequestTooLarge = "RequestTooLargeException";

    /// <summary>
    /// The function is not in the Active state.
    /// </summary>
    public const string ResourceNotReady = "ResourceNotReadyException";

    /// <summary>
    /// Lambda was unable to decrypt the environment variables because KMS access was denied.
    /// </summary>
    public const string KMSAccessDenied = "KMSAccessDeniedException";

    /// <summary>
    /// The Security Group ID provided in the Lambda function VPC configuration is not valid.
    /// </summary>
    public const string InvalidSecurityGroupId = "InvalidSecurityGroupIDException";

    /// <summary>
    /// The Subnet ID provided in the Lambda function VPC configuration is not valid.
    /// </summary>
    public const string InvalidSubnetId = "InvalidSubnetIDException";

    /// <summary>
    /// Lambda could not unzip the deployment package.
    /// </summary>
    public const string InvalidZipFile = "InvalidZipFileException";
}