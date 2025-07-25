using ErrorOr;

namespace Goa.Clients.Core.Credentials;

internal sealed class EnvironmentCredentialProvider : ICredentialProvider
{
    public ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync()
    {
        var accessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
        var secretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
        
        if (string.IsNullOrEmpty(accessKeyId))
            return new ValueTask<ErrorOr<AwsCredentials>>(Error.NotFound("AWS_ACCESS_KEY_ID", "Environment variable AWS_ACCESS_KEY_ID is not set"));
            
        if (string.IsNullOrEmpty(secretAccessKey))
            return new ValueTask<ErrorOr<AwsCredentials>>(Error.NotFound("AWS_SECRET_ACCESS_KEY", "Environment variable AWS_SECRET_ACCESS_KEY is not set"));

        var sessionToken = Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");
        return new ValueTask<ErrorOr<AwsCredentials>>(new AwsCredentials(accessKeyId, secretAccessKey, sessionToken));
    }

}