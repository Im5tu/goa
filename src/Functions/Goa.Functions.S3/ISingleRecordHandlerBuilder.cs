using Goa.Functions.Core;

namespace Goa.Functions.S3;

/// <summary>
/// Builder interface for configuring single record handlers
/// </summary>
public interface ISingleRecordHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process individual S3 event records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single S3 event record</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, S3EventRecord, Task> handler) where THandler : class;
}