using Goa.Functions.Core;

namespace Goa.Functions.S3;

/// <summary>
/// Builder interface for configuring batch record handlers
/// </summary>
public interface IMultipleRecordHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process batches of S3 event records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of S3 event records</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<S3EventRecord>, Task> handler) where THandler : class;
}