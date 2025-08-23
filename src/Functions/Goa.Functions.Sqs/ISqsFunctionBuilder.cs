using Goa.Functions.Core;

namespace Goa.Functions.Sqs;

/// <summary>
/// Builder interface for configuring SQS Lambda functions
/// </summary>
public interface ISqsFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process SQS messages one at a time
    /// </summary>
    /// <returns>A builder for configuring single message handlers</returns>
    ISingleMessageHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process SQS messages as a batch
    /// </summary>
    /// <returns>A builder for configuring batch message handlers</returns>
    IMultipleMessageHandlerBuilder ProcessAsBatch();
}