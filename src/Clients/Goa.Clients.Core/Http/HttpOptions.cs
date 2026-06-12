namespace Goa.Clients.Core.Http;

/// <summary>
/// Defines HTTP request option keys used for AWS service client configuration.
/// </summary>
public static class HttpOptions
{
    /// <summary>
    /// HTTP request option key for specifying the AWS region.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> Region = new(nameof(Region));

    /// <summary>
    /// HTTP request option key for specifying the AWS service name.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> Service = new(nameof(Service));

    /// <summary>
    /// HTTP request option key for specifying the operation target.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> Target = new(nameof(Target));

    /// <summary>
    /// HTTP request option key for specifying the API version.
    /// </summary>
    public static readonly HttpRequestOptionsKey<string> ApiVersion = new(nameof(ApiVersion));

    /// <summary>
    /// HTTP request option key for specifying the request payload as UTF-8 bytes.
    /// </summary>
    public static readonly HttpRequestOptionsKey<ReadOnlyMemory<byte>> Payload = new(nameof(Payload));

    /// <summary>
    /// HTTP request option key indicating that the request path is already URI-encoded and must be
    /// used as-is as the SigV4 canonical URI (S3-style single encoding) instead of being re-encoded.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> UseSingleUriEncoding = new(nameof(UseSingleUriEncoding));
}