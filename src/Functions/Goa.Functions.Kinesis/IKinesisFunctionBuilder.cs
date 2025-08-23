using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Builder interface for configuring Kinesis Lambda functions
/// </summary>
public interface IKinesisFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process Kinesis stream records one at a time
    /// </summary>
    /// <returns>A builder for configuring single record handlers</returns>
    ISingleRecordHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process Kinesis stream records as a batch
    /// </summary>
    /// <returns>A builder for configuring batch record handlers</returns>
    IMultipleRecordHandlerBuilder ProcessAsBatch();
}