namespace Goa.Clients.Core.Http;

/// <summary>
/// Defines HTTP request option keys used for AWS service client configuration.
/// </summary>
public static class HttpOptions
{
    /// <summary>
    /// HTTP request option key for specifying the AWS region.
    /// </summary>
    public static HttpRequestOptionsKey<string> Region = new(nameof(Region));
    
    /// <summary>
    /// HTTP request option key for specifying the AWS service name.
    /// </summary>
    public static HttpRequestOptionsKey<string> Service = new(nameof(Service));
    
    /// <summary>
    /// HTTP request option key for specifying the operation target.
    /// </summary>
    public static HttpRequestOptionsKey<string> Target = new(nameof(Target));
    
    /// <summary>
    /// HTTP request option key for specifying the API version.
    /// </summary>
    public static HttpRequestOptionsKey<string> ApiVersion = new(nameof(ApiVersion));
    
    /// <summary>
    /// HTTP request option key for specifying the request payload.
    /// </summary>
    public static HttpRequestOptionsKey<string> Payload = new(nameof(Payload));
}