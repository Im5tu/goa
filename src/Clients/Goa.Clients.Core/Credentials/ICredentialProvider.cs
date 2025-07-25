using ErrorOr;

namespace Goa.Clients.Core.Credentials;

/// <summary>
/// Provides AWS credentials for authenticating API requests.
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// Asynchronously retrieves AWS credentials.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains either:
    /// - Success: A tuple with accessKeyId, secretAccessKey, and optional sessionToken
    /// - Error: Information about why credentials could not be retrieved
    /// </returns>
    ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync();
}