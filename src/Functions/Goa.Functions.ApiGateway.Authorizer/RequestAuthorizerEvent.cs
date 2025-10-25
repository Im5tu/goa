using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents the input event for a REQUEST type Lambda authorizer
/// </summary>
public class RequestAuthorizerEvent
{
    /// <summary>
    /// Gets or sets the authorizer type (always "REQUEST")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "REQUEST";

    /// <summary>
    /// Gets or sets the ARN of the incoming method request
    /// Format: arn:aws:execute-api:{region}:{accountId}:{apiId}/{stage}/{httpVerb}/{resource}
    /// </summary>
    [JsonPropertyName("methodArn")]
    public string? MethodArn { get; set; }

    /// <summary>
    /// Gets or sets the resource path template
    /// </summary>
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    /// <summary>
    /// Gets or sets the request path
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method
    /// </summary>
    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the request headers
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets the query string parameters
    /// </summary>
    [JsonPropertyName("queryStringParameters")]
    public Dictionary<string, string>? QueryStringParameters { get; set; }

    /// <summary>
    /// Gets or sets the path parameters
    /// </summary>
    [JsonPropertyName("pathParameters")]
    public Dictionary<string, string>? PathParameters { get; set; }

    /// <summary>
    /// Gets or sets the stage variables
    /// </summary>
    [JsonPropertyName("stageVariables")]
    public Dictionary<string, string>? StageVariables { get; set; }

    /// <summary>
    /// Gets or sets the request context
    /// </summary>
    [JsonPropertyName("requestContext")]
    public AuthorizerRequestContext? RequestContext { get; set; }
}
