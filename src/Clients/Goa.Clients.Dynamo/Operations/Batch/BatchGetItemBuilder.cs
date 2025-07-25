using Goa.Clients.Dynamo.Enums;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Fluent builder for constructing DynamoDB BatchGetItem requests with a user-friendly API.
/// </summary>
public class BatchGetItemBuilder
{
    private readonly BatchGetItemRequest _request = new()
    {
        RequestItems = new Dictionary<string, BatchGetRequestItem>(),
        ReturnConsumedCapacity = ReturnConsumedCapacity.NONE
    };

    /// <summary>
    /// Adds a table to the batch get operation with its configuration.
    /// </summary>
    /// <param name="tableName">The name of the table to get items from.</param>
    /// <param name="configure">The action to configure the table-specific request.</param>
    /// <returns>The BatchGetItemBuilder instance for method chaining.</returns>
    public BatchGetItemBuilder WithTable(string tableName, Action<BatchGetTableBuilder> configure)
    {
        var tableBuilder = new BatchGetTableBuilder();
        configure(tableBuilder);
        _request.RequestItems[tableName] = tableBuilder.Build();
        return this;
    }

    /// <summary>
    /// Sets the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The BatchGetItemBuilder instance for method chaining.</returns>
    public BatchGetItemBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured BatchGetItemRequest.
    /// </summary>
    /// <returns>The configured BatchGetItemRequest instance.</returns>
    public BatchGetItemRequest Build()
    {
        return _request;
    }
}
