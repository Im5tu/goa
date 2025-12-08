using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Builder interface for configuring batch Kinesis record handlers
/// </summary>
public interface IMultipleRecordHandlerBuilder : ITypedHandlerBuilder<KinesisEvent, BatchItemFailureResponse>
{
    /// <summary>
    /// Specifies the handler function to process batches of Kinesis records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of Kinesis records</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<KinesisRecord>, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, recs, _) => handler(h, recs));

    /// <summary>
    /// Specifies the handler function to process batches of Kinesis records with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of Kinesis records with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<KinesisRecord>, CancellationToken, Task> handler)
        where THandler : class;
}
