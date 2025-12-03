using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Extensions for hosting CloudWatch Logs Lambda functions
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle CloudWatch Logs subscription filter events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>A CloudWatch Logs function builder for further configuration</returns>
    public static ICloudWatchLogsFunctionBuilder ForCloudWatchLogs(this ILambdaBuilder builder)
    {
        return new CloudWatchLogsFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}
