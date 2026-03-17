using Goa.Clients.Bedrock.Enums;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Configuration for performance optimization.
/// </summary>
public sealed class PerformanceConfiguration
{
    /// <summary>
    /// The latency mode for model inference.
    /// </summary>
    public LatencyMode Latency { get; set; }
}
