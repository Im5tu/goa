namespace Goa.Functions.ApiGateway.Payloads;

/// <summary>
///     Represents the JWT (JSON Web Token) description, including claims and scopes.
/// </summary>
public class JwtDescription
{
    /// <summary>
    ///     Gets or sets the claims contained in the JWT.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Claims { get; set; }

    /// <summary>
    ///     Gets or sets the scopes associated with the JWT.
    /// </summary>
    public IEnumerable<string>? Scopes { get; set; }
}
