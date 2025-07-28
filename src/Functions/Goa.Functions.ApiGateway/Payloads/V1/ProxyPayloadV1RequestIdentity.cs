namespace Goa.Functions.ApiGateway.Payloads.V1;

/// <summary>
///     Represents the identity information for the request, which includes Cognito identity, API keys, and other user-related data.
/// </summary>
public class ProxyPayloadV1RequestIdentity
{
    /// <summary>
    ///     Gets or sets the Cognito identity pool ID associated with the request.
    /// </summary>
    public string? CognitoIdentityPoolId { get; set; }

    /// <summary>
    ///     Gets or sets the AWS account ID of the user making the request.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito identity ID associated with the request.
    /// </summary>
    public string? CognitoIdentityId { get; set; }

    /// <summary>
    ///     Gets or sets the caller information, typically the IAM user or role associated with the request.
    /// </summary>
    public string? Caller { get; set; }

    /// <summary>
    ///     Gets or sets the API key used for the request.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the API key used for the request.
    /// </summary>
    public string? ApiKeyId { get; set; }

    /// <summary>
    ///     Gets or sets the AWS access key used for the request.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    ///     Gets or sets the source IP address of the client making the request.
    /// </summary>
    public string? SourceIp { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito authentication type used in the request, if applicable.
    /// </summary>
    public string? CognitoAuthenticationType { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito authentication provider used in the request, if applicable.
    /// </summary>
    public string? CognitoAuthenticationProvider { get; set; }

    /// <summary>
    ///     Gets or sets the ARN of the authenticated user making the request.
    /// </summary>
    public string? UserArn { get; set; }

    /// <summary>
    ///     Gets or sets the user agent string of the client making the request.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    ///     Gets or sets the user name of the authenticated user making the request.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    ///     Gets or sets the client certificate information used in the request, if applicable.
    /// </summary>
    public ProxyRequestClientCert? ClientCert { get; set; }
}
