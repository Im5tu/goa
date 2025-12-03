using Goa.Functions.Core;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Builder interface for configuring CloudWatch Logs Lambda functions
/// </summary>
public interface ICloudWatchLogsFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process log events one at a time.
    /// Control messages are automatically filtered out.
    /// </summary>
    /// <returns>A builder for configuring single log event handlers</returns>
    ISingleLogEventHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process all log events as a batch
    /// </summary>
    /// <returns>A builder for configuring batch log event handlers</returns>
    IMultipleLogEventHandlerBuilder ProcessAsBatch();
}
