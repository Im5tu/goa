using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Builder interface for configuring single record handlers
/// </summary>
public interface ISingleRecordHandlerBuilder : ITypedHandlerBuilder<DynamoDbEvent, string>
{
    /// <summary>
    /// Specifies the handler function to process individual DynamoDB stream records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single DynamoDB stream record</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, DynamoDbStreamRecord, Task> handler)
        where THandler : class
        => HandleWith<THandler>((h, rec, _) => handler(h, rec));

    /// <summary>
    /// Specifies the handler function to process individual DynamoDB stream records with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a single DynamoDB stream record with cancellation token</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, DynamoDbStreamRecord, CancellationToken, Task> handler)
        where THandler : class;
}
