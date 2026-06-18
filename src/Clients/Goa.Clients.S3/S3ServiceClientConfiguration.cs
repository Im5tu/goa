using Goa.Clients.Core.Configuration;

namespace Goa.Clients.S3;

/// <summary>
/// Configuration class for S3 service providing S3-specific settings.
/// </summary>
public sealed class S3ServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether path-style addressing should be used
    /// (e.g. https://s3.us-east-1.amazonaws.com/bucket/key) instead of the default
    /// virtual-host style addressing (e.g. https://bucket.s3.us-east-1.amazonaws.com/key).
    /// Path-style addressing is always used when <see cref="AwsServiceConfiguration.ServiceUrl"/> is set.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// Initializes a new instance of the S3ServiceClientConfiguration class.
    /// </summary>
    public S3ServiceClientConfiguration() : base("s3")
    {
        ApiVersion = "2006-03-01";
    }
}
