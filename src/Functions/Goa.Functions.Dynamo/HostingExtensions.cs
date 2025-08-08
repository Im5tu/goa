using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle DynamoDB stream events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>A DynamoDB function builder for further configuration</returns>
    public static IDynamoDbFunctionBuilder ForDynamoDB(this ILambdaBuilder builder)
    {
        return new DynamoDbFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}
