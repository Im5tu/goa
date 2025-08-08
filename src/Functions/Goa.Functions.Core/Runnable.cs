namespace Goa.Functions.Core;

/// <summary>
/// Default implementation of <see cref="IRunnable"/> that executes a configured Lambda function
/// </summary>
public sealed class Runnable : IRunnable
{
    private readonly ILambdaBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="Runnable"/> class
    /// </summary>
    /// <param name="builder">The Lambda builder to execute</param>
    public Runnable(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
    public Task RunAsync(InitializationMode mode = InitializationMode.Parallel)
    {
        return _builder.RunAsync(mode);
    }
}
