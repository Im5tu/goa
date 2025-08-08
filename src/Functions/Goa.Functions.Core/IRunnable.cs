namespace Goa.Functions.Core;

/// <summary>
/// Interface for running a configured Lambda function
/// </summary>
public interface IRunnable
{
    /// <summary>
    /// Runs the Lambda function with the specified initialization mode
    /// </summary>
    /// <param name="mode">The initialization mode for Lambda startup tasks</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RunAsync(InitializationMode mode = InitializationMode.Parallel);
}
