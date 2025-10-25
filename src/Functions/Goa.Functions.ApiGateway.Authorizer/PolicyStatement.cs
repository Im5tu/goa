using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents an individual statement within an IAM policy document
/// </summary>
public class PolicyStatement
{
    /// <summary>
    /// Gets or sets the action to be performed (typically "execute-api:Invoke")
    /// </summary>
    [JsonPropertyName("Action")]
    public string Action { get; set; } = "execute-api:Invoke";

    /// <summary>
    /// Gets or sets the effect of the policy statement (Allow or Deny)
    /// </summary>
    [JsonPropertyName("Effect")]
    public Effect Effect { get; set; }

    /// <summary>
    /// Gets or sets the resource ARN(s) to which this statement applies
    /// Can be a single string or an array of strings
    /// </summary>
    [JsonPropertyName("Resource")]
    public object Resource { get; set; } = null!;
}
