using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents an IAM policy document for API Gateway authorizer responses
/// </summary>
public class PolicyDocument
{
    /// <summary>
    /// Gets or sets the policy document version (typically "2012-10-17")
    /// </summary>
    [JsonPropertyName("Version")]
    public string Version { get; set; } = "2012-10-17";

    /// <summary>
    /// Gets or sets the list of policy statements
    /// </summary>
    [JsonPropertyName("Statement")]
    public IList<PolicyStatement> Statement { get; set; } = new List<PolicyStatement>();
}
