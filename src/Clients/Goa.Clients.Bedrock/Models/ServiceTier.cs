using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// The service tier for request processing.
/// </summary>
[JsonConverter(typeof(ServiceTierConverter))]
public enum ServiceTier
{
    /// <summary>
    /// Default service tier.
    /// </summary>
    Default,

    /// <summary>
    /// Priority service tier for faster processing.
    /// </summary>
    Priority,

    /// <summary>
    /// Flexible service tier for cost optimization.
    /// </summary>
    Flex
}
