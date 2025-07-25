namespace Goa.Clients.Core.Credentials;

/// <summary>
/// Represents AWS credentials with optional expiration information.
/// </summary>
/// <param name="AccessKeyId">The AWS access key ID</param>
/// <param name="SecretAccessKey">The AWS secret access key</param>
/// <param name="SessionToken">Optional session token for temporary credentials</param>
/// <param name="Expiration">Optional expiration time for temporary credentials</param>
public record AwsCredentials(string AccessKeyId, string SecretAccessKey, string? SessionToken = null, DateTime? Expiration = null);