using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the response from an API Gateway Lambda authorizer
/// </summary>
public class AuthorizerResponse
{
    /// <summary>
    /// Gets or sets the principal identifier for the user
    /// </summary>
    [JsonPropertyName("principalId")]
    public string PrincipalId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the IAM policy document specifying allowed/denied resources
    /// </summary>
    [JsonPropertyName("policyDocument")]
    public PolicyDocument PolicyDocument { get; set; } = new();

    /// <summary>
    /// Gets or sets additional context data to pass to the backend
    /// All values must be strings, numbers, or booleans (as strings)
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object>? Context { get; set; }

    /// <summary>
    /// Gets or sets the usage identifier key when using API Gateway usage plans
    /// Should be set to one of the usage plan's API keys
    /// </summary>
    [JsonPropertyName("usageIdentifierKey")]
    public string? UsageIdentifierKey { get; set; }
}
