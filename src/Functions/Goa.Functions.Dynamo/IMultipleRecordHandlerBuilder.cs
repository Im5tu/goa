using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Builder interface for configuring batch record handlers
/// </summary>
public interface IMultipleRecordHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process batches of DynamoDB stream records
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes a collection of DynamoDB stream records</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, IEnumerable<DynamoDbStreamRecord>, Task> handler) where THandler : class;
}
