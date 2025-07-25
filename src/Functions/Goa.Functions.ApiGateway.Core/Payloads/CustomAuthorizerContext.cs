using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Core.Payloads;

/// <summary>
/// Represents the custom authorizer context, including claims and scopes provided by the Lambda authorizer.
/// </summary>
public class CustomAuthorizerContext
{
    /// <summary>
    ///     Gets or sets the claims extracted from the authorizer context, provided by a Lambda authorizer.
    /// </summary>
    public Dictionary<string, string>? Claims { get; set; }

    /// <summary>
    ///     Gets or sets the scopes from the authorizer context, typically related to OAuth 2.0 authorization.
    /// </summary>
    [JsonConverter(typeof(SpaceDelimitedStringConverter))]
    public List<string>? Scopes { get; set; }
}
