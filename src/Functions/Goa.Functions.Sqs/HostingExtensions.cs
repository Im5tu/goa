using Goa.Functions.Core;

namespace Goa.Functions.Sqs;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle SQS events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>An SQS function builder for further configuration</returns>
    public static ISqsFunctionBuilder ForSQS(this ILambdaBuilder builder)
    {
        return new SqsFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}