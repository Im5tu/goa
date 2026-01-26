namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Performance metrics for a conversation request.
/// </summary>
public class ConverseMetrics
{
    /// <summary>
    /// The latency of the request in milliseconds.
    /// </summary>
    public long LatencyMs { get; set; }
}
