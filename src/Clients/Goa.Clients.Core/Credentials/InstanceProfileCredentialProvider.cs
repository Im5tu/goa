using ErrorOr;
using System.Text.Json;

namespace Goa.Clients.Core.Credentials;

/// <summary>
/// Provides credentials from EC2 instance metadata service (IMDS).
/// </summary>
internal sealed class InstanceProfileCredentialProvider : ICredentialProvider, IDisposable
{
    private const string MetadataEndpoint = "http://169.254.169.254";
    private const string TokenPath = "/latest/api/token";
    private const string RolePath = "/latest/meta-data/iam/security-credentials/";
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(1);

    private readonly HttpClient _httpClient;

    public InstanceProfileCredentialProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = HttpTimeout };
    }

    public async ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync()
    {
        return await FetchCredentialsFromImdsAsync();
    }


    private async Task<ErrorOr<AwsCredentials>> FetchCredentialsFromImdsAsync()
    {
        try
        {
            // Get IMDSv2 token
            var tokenResult = await GetImdsTokenAsync();
            if (tokenResult.IsError)
                return tokenResult.Errors;

            var token = tokenResult.Value;

            // Get the role name
            var roleResult = await GetRoleNameAsync(token);
            if (roleResult.IsError)
                return roleResult.Errors;

            var roleName = roleResult.Value;

            // Get credentials for the role
            var credentialsResult = await GetRoleCredentialsAsync(token, roleName);
            if (credentialsResult.IsError)
                return credentialsResult.Errors;

            return credentialsResult.Value;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Error.Failure("ImdsUnavailable", "Unable to connect to EC2 instance metadata service");
        }
    }

    private async Task<ErrorOr<string>> GetImdsTokenAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{MetadataEndpoint}{TokenPath}");
        request.Headers.Add("X-aws-ec2-metadata-token-ttl-seconds", "21600");

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return Error.Failure("ImdsToken", $"Failed to get IMDS token: {response.StatusCode}");

        var token = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(token))
            return Error.Failure("ImdsToken", "Received empty IMDS token");

        return token;
    }

    private async Task<ErrorOr<string>> GetRoleNameAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{MetadataEndpoint}{RolePath}");
        request.Headers.Add("X-aws-ec2-metadata-token", token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return Error.Failure("ImdsRole", $"Failed to get IAM role name: {response.StatusCode}");

        var roleName = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(roleName))
            return Error.Failure("ImdsRole", "No IAM role found for this instance");

        return roleName.Trim();
    }

    private async Task<ErrorOr<AwsCredentials>> GetRoleCredentialsAsync(string token, string roleName)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{MetadataEndpoint}{RolePath}{roleName}");
        request.Headers.Add("X-aws-ec2-metadata-token", token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return Error.Failure("ImdsCredentials", $"Failed to get credentials for role {roleName}: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var accessKeyId = root.GetProperty("AccessKeyId").GetString();
            var secretAccessKey = root.GetProperty("SecretAccessKey").GetString();
            var sessionToken = root.GetProperty("Token").GetString();
            
            DateTime? expiration = null;
            if (root.TryGetProperty("Expiration", out var expirationElement))
            {
                if (DateTime.TryParse(expirationElement.GetString(), out var parsedExpiration))
                    expiration = parsedExpiration;
            }

            if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey) || string.IsNullOrEmpty(sessionToken))
                return Error.Failure("ImdsCredentials", "Invalid credentials received from IMDS");

            return new AwsCredentials(accessKeyId, secretAccessKey, sessionToken, expiration);
        }
        catch (JsonException ex)
        {
            return Error.Failure("ImdsCredentials", $"Failed to parse credentials JSON: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
