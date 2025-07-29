namespace Goa.Functions.Core;

/// <summary>
///     Represents a task that should be executed during Lambda function initialization.
/// </summary>
public interface ILambdaInitializationTask
{
    /// <summary>
    ///     Performs initialization work asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task InitializeAsync();
}