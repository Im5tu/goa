namespace Goa.Functions.ApiGateway.Payloads.V2;

/// <summary>
///     Represents the IAM (Identity and Access Management) description in a V2 API Gateway request.
/// </summary>
public class ProxyPayloadV2IAMDescription
{
    /// <summary>
    ///     Gets or sets the AWS access key used for the request.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    ///     Gets or sets the AWS account ID associated with the IAM principal making the request.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    ///     Gets or sets the caller ID, which represents the entity making the request, such as an IAM user or role.
    /// </summary>
    public string? CallerId { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito identity associated with the request, if applicable.
    /// </summary>
    public CognitoIdentityDescription? CognitoIdentity { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the organization (Org ID) associated with the IAM principal making the request.
    /// </summary>
    public string? PrincipalOrgId { get; set; }

    /// <summary>
    ///     Gets or sets the ARN (Amazon Resource Name) of the user or role making the request.
    /// </summary>
    public string? UserARN { get; set; }

    /// <summary>
    ///     Gets or sets the user ID of the IAM principal making the request.
    /// </summary>
    public string? UserId { get; set; }
}
