namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A cache checkpoint for prompt caching in Bedrock.
/// </summary>
public sealed class CachePoint
{
    /// <summary>
    /// The type of cache point. Currently only "default" is supported.
    /// </summary>
    public string Type { get; set; } = "default";
}
