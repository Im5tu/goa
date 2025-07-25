using Goa.Clients.Core.Configuration;

namespace Goa.Clients.EventBridge;

/// <summary>
/// Configuration class for EventBridge service providing EventBridge-specific settings.
/// </summary>
public sealed class EventBridgeServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the EventBridgeServiceClientConfiguration class.
    /// </summary>
    public EventBridgeServiceClientConfiguration() : base("events")
    {
        ApiVersion = "2015-10-07";
    }
}
