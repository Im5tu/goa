using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Builder interface for configuring single Kinesis record handlers
/// </summary>
public interface ISingleRecordHandlerBuilder
{
    /// <summary>
    /// Configures the function to handle Kinesis records using the specified handler type
    /// </summary>
    /// <typeparam name="THandler">The type of handler that processes individual Kinesis records</typeparam>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using<THandler>()
        where THandler : class;

    /// <summary>
    /// Configures the function to handle Kinesis records using the specified handler function
    /// </summary>
    /// <param name="handler">The function that processes individual Kinesis records</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using(Func<KinesisRecord, CancellationToken, Task> handler);

    /// <summary>
    /// Configures the function to handle Kinesis records using the specified handler function with return value
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the handler</typeparam>
    /// <param name="handler">The function that processes individual Kinesis records and returns a result</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable Using<TResult>(Func<KinesisRecord, CancellationToken, Task<TResult>> handler);
}