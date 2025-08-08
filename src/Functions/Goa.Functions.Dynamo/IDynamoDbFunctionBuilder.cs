using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Builder interface for configuring DynamoDB Lambda functions
/// </summary>
public interface IDynamoDbFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the function to process DynamoDB stream records one at a time
    /// </summary>
    /// <returns>A builder for configuring single record handlers</returns>
    ISingleRecordHandlerBuilder ProcessOneAtATime();

    /// <summary>
    /// Configures the function to process DynamoDB stream records as a batch
    /// </summary>
    /// <returns>A builder for configuring batch record handlers</returns>
    IMultipleRecordHandlerBuilder ProcessAsBatch();
}