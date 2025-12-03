using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Fluent builder for constructing DynamoDB BatchWriteItem requests with a user-friendly API.
/// </summary>
public class BatchWriteItemBuilder
{
    private readonly BatchWriteItemRequest _request = new()
    {
        RequestItems = new Dictionary<string, List<BatchWriteRequestItem>>()
    };

    /// <summary>
    /// Adds a table to the batch write operation with its configuration.
    /// </summary>
    /// <param name="tableName">The name of the table to write items to.</param>
    /// <param name="configure">The action to configure the table-specific requests.</param>
    /// <returns>The BatchWriteItemBuilder instance for method chaining.</returns>
    public BatchWriteItemBuilder WithTable(string tableName, Action<BatchWriteTableBuilder> configure)
    {
        var tableBuilder = new BatchWriteTableBuilder();
        configure(tableBuilder);
        _request.RequestItems[tableName] = tableBuilder.Build();
        return this;
    }

    /// <summary>
    /// Determines the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The BatchWriteItemBuilder instance for method chaining.</returns>
    public BatchWriteItemBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Determines whether item collection metrics are returned.
    /// </summary>
    /// <param name="returnItemCollectionMetrics">The item collection metrics option.</param>
    /// <returns>The BatchWriteItemBuilder instance for method chaining.</returns>
    public BatchWriteItemBuilder WithReturnItemCollectionMetrics(ReturnItemCollectionMetrics returnItemCollectionMetrics)
    {
        _request.ReturnItemCollectionMetrics = returnItemCollectionMetrics;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured BatchWriteItemRequest.
    /// </summary>
    /// <returns>The configured BatchWriteItemRequest instance.</returns>
    public BatchWriteItemRequest Build()
    {
        return _request;
    }
}