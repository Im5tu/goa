using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Builder interface for configuring single Kinesis record handlers
/// </summary>
public interface ISingleRecordHandlerBuilder : ITypedHandlerBuilder<KinesisEvent, BatchItemFailureResponse>
{
    /// <summary>
    /// Specifies the handler function to process individual Kinesis records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single Kinesis record</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, KinesisRecord, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, rec, _) => handler(h, rec));

    /// <summary>
    /// Specifies the handler function to process individual Kinesis records with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single Kinesis record with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, KinesisRecord, CancellationToken, Task> handler)
        where THandler : class;
}
