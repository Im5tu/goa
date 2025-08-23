using Goa.Functions.Core;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle EventBridge events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>An EventBridge function builder for further configuration</returns>
    public static IEventbridgeFunctionBuilder ForEventBridge(this ILambdaBuilder builder)
    {
        return new EventbridgeFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}