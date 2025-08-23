using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Builder interface for configuring batch Kinesis record handlers
/// </summary>
public interface IMultipleRecordHandlerBuilder
{
    /// <summary>
    /// Configures the function to handle Kinesis record batches using the specified handler type
    /// </summary>
    /// <typeparam name="THandler">The type of handler that processes batches of Kinesis records</typeparam>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using<THandler>()
        where THandler : class;

    /// <summary>
    /// Configures the function to handle Kinesis record batches using the specified handler function
    /// </summary>
    /// <param name="handler">The function that processes batches of Kinesis records</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using(Func<KinesisEvent, CancellationToken, Task> handler);

    /// <summary>
    /// Configures the function to handle Kinesis record batches using the specified handler function with return value
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the handler</typeparam>
    /// <param name="handler">The function that processes batches of Kinesis records and returns a result</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using<TResult>(Func<KinesisEvent, CancellationToken, Task<TResult>> handler);
}