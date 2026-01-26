using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Enums;

/// <summary>
/// The latency mode for model inference.
/// </summary>
[JsonConverter(typeof(LatencyModeConverter))]
public enum LatencyMode
{
    /// <summary>
    /// Standard latency mode.
    /// </summary>
    Standard,

    /// <summary>
    /// Optimized for lower latency.
    /// </summary>
    Optimized
}
