namespace Goa.Functions.ApiGateway;

/// <summary>
///     Creates a pipeline of middleware for execution
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    ///     Adds a middleware to the pipeline execution
    /// </summary>
    /// <typeparam name="T">The type of middleware to add</typeparam>
    IPipelineBuilder Use<T>() where T : IMiddleware, new();
    /// <summary>
    ///     Adds a middleware to the pipeline execution
    /// </summary>
    IPipelineBuilder Use<T>(Func<T> factory) where T : IMiddleware;
    /// <summary>
    ///     Adds a middleware to the pipeline execution
    /// </summary>
    IPipelineBuilder Use(Func<InvocationContext, Func<Task>, CancellationToken, Task> handler);
}
