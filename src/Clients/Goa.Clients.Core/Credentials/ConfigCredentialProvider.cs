using ErrorOr;
using System.Text;

namespace Goa.Clients.Core.Credentials;

/// <summary>
/// Provides credentials from AWS config and credentials files (~/.aws/config and ~/.aws/credentials).
/// </summary>
internal sealed class ConfigCredentialProvider : ICredentialProvider
{
    private readonly string _profile;
    private readonly string _configPath;
    private readonly string _credentialsPath;

    public ConfigCredentialProvider()
    {
        _profile = Environment.GetEnvironmentVariable("AWS_PROFILE") ?? "default";
        _configPath = Environment.GetEnvironmentVariable("AWS_CONFIG_FILE") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "config");
        _credentialsPath = Environment.GetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aws", "credentials");
    }

    public async ValueTask<ErrorOr<AwsCredentials>> GetCredentialsAsync()
    {
        // Load credential properties from both files and merge them (credentials file takes precedence)
        var credentialsProperties = await TryLoadPropertiesFromFileAsync(_credentialsPath, _profile);
        
        // For config file, default profile uses "default", others use "profile {name}"
        var configSectionName = _profile == "default" ? "default" : $"profile {_profile}";
        var configProperties = await TryLoadPropertiesFromFileAsync(_configPath, configSectionName);
        
        // Merge properties from both files (credentials file takes precedence)
        var mergedProperties = new Dictionary<string, string>();
        
        // Start with config file properties
        if (!configProperties.IsError)
        {
            foreach (var kvp in configProperties.Value)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }
        
        // Override with credentials file properties (higher precedence)
        if (!credentialsProperties.IsError)
        {
            foreach (var kvp in credentialsProperties.Value)
            {
                mergedProperties[kvp.Key] = kvp.Value;
            }
        }
        
        // Extract required credentials from merged properties
        if (!mergedProperties.TryGetValue("aws_access_key_id", out var accessKeyId) || string.IsNullOrEmpty(accessKeyId))
            return Error.NotFound("AccessKeyId", $"aws_access_key_id not found for profile '{_profile}' in config files");
            
        if (!mergedProperties.TryGetValue("aws_secret_access_key", out var secretAccessKey) || string.IsNullOrEmpty(secretAccessKey))
            return Error.NotFound("SecretAccessKey", $"aws_secret_access_key not found for profile '{_profile}' in config files");
        
        mergedProperties.TryGetValue("aws_session_token", out var sessionToken);
        
        return new AwsCredentials(accessKeyId, secretAccessKey, sessionToken);
    }


    private static async ValueTask<ErrorOr<Dictionary<string, string>>> TryLoadPropertiesFromFileAsync(string filePath, string sectionName)
    {
        try
        {
            if (!File.Exists(filePath))
                return Error.NotFound("ConfigFile", $"Config file not found: {filePath}");

            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var inSection = false;
            var properties = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    var currentSection = trimmedLine[1..^1].Trim();
                    inSection = string.Equals(currentSection, sectionName, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inSection)
                    continue;

                var equalIndex = trimmedLine.IndexOf('=');
                if (equalIndex == -1)
                    continue;

                var key = trimmedLine[..equalIndex].Trim().ToLowerInvariant();
                var value = trimmedLine[(equalIndex + 1)..].Trim();

                // Store all properties, not just credential-related ones
                if (!string.IsNullOrEmpty(value))
                {
                    properties[key] = value;
                }
            }

            // Return the properties dictionary (could be empty if section not found)
            return properties;
        }
        catch (Exception ex)
        {
            return Error.Failure("ConfigFileRead", $"Failed to read config file {filePath}: {ex.Message}");
        }
    }
}
