using Goa.Functions.Core;

namespace Goa.Functions.S3;

/// <summary>
/// Builder interface for configuring S3 Lambda functions
/// </summary>
public interface IS3FunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process S3 event records one at a time
    /// </summary>
    /// <returns>A builder for configuring single record handlers</returns>
    ISingleRecordHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process S3 event records as a batch
    /// </summary>
    /// <returns>A builder for configuring batch record handlers</returns>
    IMultipleRecordHandlerBuilder ProcessAsBatch();
}