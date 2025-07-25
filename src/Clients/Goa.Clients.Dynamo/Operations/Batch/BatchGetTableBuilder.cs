using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Fluent builder for configuring table-specific requests within a BatchGetItem operation.
/// </summary>
public class BatchGetTableBuilder
{
    private readonly BatchGetRequestItem _request = new()
    {
        Keys = new List<Dictionary<string, AttributeValue>>()
    };

    /// <summary>
    /// Adds multiple keys to retrieve from the table.
    /// </summary>
    /// <param name="keys">The collection of keys to retrieve.</param>
    /// <returns>The BatchGetTableBuilder instance for method chaining.</returns>
    public BatchGetTableBuilder WithKeys(Dictionary<string, AttributeValue> keys)
    {
        _request.Keys.AddRange(keys);
        return this;
    }

    /// <summary>
    /// Adds a key with string attributes to retrieve from the table.
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The string value of the key.</param>
    /// <returns>The BatchGetTableBuilder instance for method chaining.</returns>
    public BatchGetTableBuilder WithKey(string attributeName, string value)
    {
        _request.Keys.Add(new Dictionary<string, AttributeValue>
        {
            [attributeName] = new AttributeValue { S = value }
        });
        return this;
    }

    /// <summary>
    /// Adds a key with the corresponding attribute value to retrieve from the table.
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The attribute value to add</param>
    /// <returns>The BatchGetTableBuilder instance for method chaining.</returns>
    public BatchGetTableBuilder WithKey(string attributeName, AttributeValue value)
    {
        _request.Keys.Add(new Dictionary<string, AttributeValue>
        {
            [attributeName] = value
        });
        return this;
    }

    /// <summary>
    /// Builds and returns the configured BatchGetRequestItem.
    /// </summary>
    /// <returns>The configured BatchGetRequestItem instance.</returns>
    internal BatchGetRequestItem Build()
    {
        return _request;
    }
}
