using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Builder interface for configuring CloudWatch Logs Lambda functions
/// </summary>
public interface ICloudWatchLogsFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to skip control messages.
    /// Control messages (connectivity checks from CloudWatch) are automatically filtered out.
    /// </summary>
    /// <returns>A builder for configuring log event handlers</returns>
    ICloudWatchLogsHandlerBuilder ProcessWithoutControlMessages();

    /// <summary>
    /// Configures the function to process all messages including control messages.
    /// Your handler should check <see cref="CloudWatchLogsEvent.IsControlMessage"/> to handle them appropriately.
    /// </summary>
    /// <returns>A builder for configuring log event handlers</returns>
    ICloudWatchLogsHandlerBuilder ProcessWithControlMessages();
}
