using Goa.Functions.Core;

namespace Goa.Functions.S3;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle S3 events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>An S3 function builder for further configuration</returns>
    public static IS3FunctionBuilder ForS3(this ILambdaBuilder builder)
    {
        return new S3FunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}