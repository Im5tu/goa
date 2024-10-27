namespace Goa.Functions.ApiGateway.Payloads;

/// <summary>
///     Represents the Cognito identity details associated with a request, including authentication methods and identity information.
/// </summary>
public class CognitoIdentityDescription
{
    /// <summary>
    ///     Gets or sets the authentication methods reference (AMR) for the Cognito identity, typically a list of methods used for authentication.
    /// </summary>
    public IList<string>? AMR { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito identity ID associated with the request.
    /// </summary>
    public string? IdentityId { get; set; }

    /// <summary>
    ///     Gets or sets the Cognito identity pool ID associated with the request.
    /// </summary>
    public string? IdentityPoolId { get; set; }
}
