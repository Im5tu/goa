namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Represents HTTP methods/verbs for API Gateway authorizer policies
/// </summary>
public enum HttpVerb
{
    /// <summary>
    /// HTTP GET method
    /// </summary>
    GET,

    /// <summary>
    /// HTTP POST method
    /// </summary>
    POST,

    /// <summary>
    /// HTTP PUT method
    /// </summary>
    PUT,

    /// <summary>
    /// HTTP PATCH method
    /// </summary>
    PATCH,

    /// <summary>
    /// HTTP DELETE method
    /// </summary>
    DELETE,

    /// <summary>
    /// HTTP HEAD method
    /// </summary>
    HEAD,

    /// <summary>
    /// HTTP OPTIONS method
    /// </summary>
    OPTIONS,

    /// <summary>
    /// All HTTP methods (wildcard)
    /// </summary>
    ALL
}
