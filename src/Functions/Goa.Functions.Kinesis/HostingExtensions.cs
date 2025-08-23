using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle Kinesis stream events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>A Kinesis function builder for further configuration</returns>
    public static IKinesisFunctionBuilder ForKinesis(this ILambdaBuilder builder)
    {
        return new KinesisFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}