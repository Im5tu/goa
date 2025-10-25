using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the input event for a TOKEN type Lambda authorizer
/// </summary>
public class TokenAuthorizerEvent
{
    /// <summary>
    /// Gets or sets the authorizer type (always "TOKEN")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "TOKEN";

    /// <summary>
    /// Gets or sets the authorization token passed by the client
    /// Extracted from the custom header specified in the authorizer configuration
    /// </summary>
    [JsonPropertyName("authorizationToken")]
    public string? AuthorizationToken { get; set; }

    /// <summary>
    /// Gets or sets the ARN of the incoming method request
    /// Format: arn:aws:execute-api:{region}:{accountId}:{apiId}/{stage}/{httpVerb}/{resource}
    /// </summary>
    [JsonPropertyName("methodArn")]
    public string? MethodArn { get; set; }
}
