using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the request context in a REQUEST type authorizer event
/// </summary>
public class AuthorizerRequestContext
{
    /// <summary>
    /// Gets or sets the resource path
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the AWS account ID
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the resource ID
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the API Gateway stage name
    /// </summary>
    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    /// <summary>
    /// Gets or sets the unique request ID
    /// </summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the identity information
    /// </summary>
    [JsonPropertyName("identity")]
    public AuthorizerIdentity? Identity { get; set; }

    /// <summary>
    /// Gets or sets the resource path template
    /// </summary>
    [JsonPropertyName("resourcePath")]
    public string? ResourcePath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the API Gateway API ID
    /// </summary>
    [JsonPropertyName("apiId")]
    public string? ApiId { get; set; }
}
