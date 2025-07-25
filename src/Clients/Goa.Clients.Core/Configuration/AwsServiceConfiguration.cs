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
    /// Initializes a new instance of the AwsServiceConfiguration class.
    /// </summary>
    /// <param name="service">The AWS service name (e.g., "dynamodb", "s3").</param>
    protected AwsServiceConfiguration(string service)
    {
        Service = service;
    }
}