using Microsoft.Extensions.Logging;

namespace Goa.Clients.Core.Configuration;

/// <summary>
/// Base configuration class for AWS services providing common settings like region, API version, and logging.
/// </summary>
public class AwsServiceConfiguration
{
    private static readonly string DefaultRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";
    
    /// <summary>
    /// Gets or sets the API version to use for service requests. Defaults to "2012-08-10".
    /// </summary>
    public string ApiVersion { get; set; } = "2012-08-10";
    
    /// <summary>
    /// Gets or sets the minimum log level for service operations. Defaults to Information.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Gets or sets the AWS region for service operations. Defaults to us-east-1 or the AWS_REGION environment variable.
    /// </summary>
    public string Region { get; set; } = DefaultRegion;
    
    /// <summary>
    /// Gets or sets a custom service URL to use instead of the default AWS endpoint.
    /// </summary>
    public string? ServiceUrl { get; set; }
    internal string Service { get; init; }

    /// <summary>
    /// Gets or sets the AWS service name used for SigV4 signing. Defaults to Service if not set.
    /// Some AWS services use a different signing service name than their endpoint prefix.
    /// </summary>
    internal string SigningService { get; init; }

    /// <summary>
    /// Initializes a new instance of the AwsServiceConfiguration class.
    /// </summary>
    /// <param name="service">The AWS service name (e.g., "dynamodb", "s3").</param>
    /// <param name="signingService">The AWS service name used for SigV4 signing. Defaults to service if not specified.</param>
    protected AwsServiceConfiguration(string service, string? signingService = null)
    {
        Service = service;
        SigningService = signingService ?? service;
    }
}