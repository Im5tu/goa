using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Fluent builder for configuring table-specific requests within a BatchWriteItem operation.
/// </summary>
public class BatchWriteTableBuilder
{
    private readonly List<BatchWriteRequestItem> _requests = new();

    /// <summary>
    /// Adds a delete request to remove an item from the table.
    /// </summary>
    /// <param name="key">The key of the item to delete.</param>
    /// <returns>The BatchWriteTableBuilder instance for method chaining.</returns>
    public BatchWriteTableBuilder WithDelete(Dictionary<string, AttributeValue> key)
    {
        _requests.Add(new BatchWriteRequestItem
        {
            DeleteRequest = new DeleteRequest { Key = key }
        });
        return this;
    }

    /// <summary>
    /// Adds multiple delete requests to remove items from the table.
    /// </summary>
    /// <param name="keys">The keys of the items to delete.</param>
    /// <returns>The BatchWriteTableBuilder instance for method chaining.</returns>
    public BatchWriteTableBuilder WithDelete(IEnumerable<Dictionary<string, AttributeValue>> keys)
    {
        foreach (var key in keys)
        {
            WithDelete(key);
        }
        return this;
    }

    /// <summary>
    /// Adds a put request to insert an item into the table.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    /// <returns>The BatchWriteTableBuilder instance for method chaining.</returns>
    public BatchWriteTableBuilder WithPut(Dictionary<string, AttributeValue> item)
    {
        _requests.Add(new BatchWriteRequestItem
        {
            PutRequest = new PutRequest { Item = item }
        });
        return this;
    }

    /// <summary>
    /// Adds multiple put requests to insert items into the table.
    /// </summary>
    /// <param name="items">The items to insert.</param>
    /// <returns>The BatchWriteTableBuilder instance for method chaining.</returns>
    public BatchWriteTableBuilder WithPut(IEnumerable<Dictionary<string, AttributeValue>> items)
    {
        foreach (var item in items)
        {
            WithPut(item);
        }
        return this;
    }

    /// <summary>
    /// Builds and returns the configured list of BatchWriteRequestItem.
    /// </summary>
    /// <returns>The configured list of BatchWriteRequestItem instances.</returns>
    internal List<BatchWriteRequestItem> Build()
    {
        return _requests;
    }
}
