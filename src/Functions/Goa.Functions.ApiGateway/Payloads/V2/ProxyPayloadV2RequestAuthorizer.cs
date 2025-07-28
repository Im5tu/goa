namespace Goa.Functions.ApiGateway.Payloads.V2;

/// <summary>
///     Represents the authorizer information for an AWS API Gateway Proxy (V2) request, including JWT, Lambda, and IAM details used for authorization.
/// </summary>
public class ProxyPayloadV2RequestAuthorizer
{
    /// <summary>
    ///     Gets or sets the JWT (JSON Web Token) description, which includes claims and scopes.
    /// </summary>
    public JwtDescription? Jwt { get; set; }

    /// <summary>
    ///     Gets or sets additional authorizer context provided by Lambda authorizers.
    /// </summary>
    public IDictionary<string, object>? Lambda { get; set; }

    /// <summary>
    ///     Gets or sets the IAM (Identity and Access Management) description, including access keys, account IDs, and user details.
    /// </summary>
    public ProxyPayloadV2IAMDescription? IAM { get; set; }
}
