using ErrorOr;

namespace Goa.Clients.Core.Credentials;

internal sealed class StaticCredentialProvider : ICredentialProvider
{
    private readonly string _accessKeyId;
    private readonly string _secretAccessKey;
    private readonly string? _sessionToken;

    public StaticCredentialProvider(string accessKeyId, string secretAccessKey, string? sessionToken)
    {
        _accessKeyId = accessKeyId;
        _secretAccessKey = secretAccessKey;
        _sessionToken = sessionToken;
    }

    public ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync()
    {
        return new ValueTask<ErrorOr<AwsCredentials>>(new AwsCredentials(_accessKeyId, _secretAccessKey, _sessionToken));
    }

}