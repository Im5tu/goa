using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.GetItem;

/// <summary>
/// Fluent builder for constructing DynamoDB GetItem requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to get the item from.</param>
public class GetItemBuilder(string tableName)
{
    private readonly GetItemRequest _request = new()
    {
        TableName = tableName,
        Key = new Dictionary<string, AttributeValue>()
    };

    /// <summary>
    /// Sets the primary key for the item to retrieve.
    /// </summary>
    /// <param name="key">The primary key attributes as a dictionary.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithKey(Dictionary<string, AttributeValue> key)
    {
        _request.Key = key ?? new Dictionary<string, AttributeValue>(StringComparer.OrdinalIgnoreCase);
        return this;
    }

    /// <summary>
    /// Adds a single key attribute to the primary key.
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithKey(string attributeName, AttributeValue value)
    {
        _request.Key[attributeName] = value;
        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the item using a params array for convenience.
    /// </summary>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithProjection(params string[] attributes)
    {
        if (attributes?.Length > 0)
        {
            _request.ProjectionExpression = string.Join(", ", attributes);
        }
        else
        {
            _request.ProjectionExpression = null;
        }

        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the item using an enumerable collection.
    /// </summary>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithProjection(IEnumerable<string> attributes)
    {
        if (attributes?.Any() == true)
        {
            _request.ProjectionExpression = string.Join(", ", attributes);
        }
        else
        {
            _request.ProjectionExpression = null;
        }

        return this;
    }

    /// <summary>
    /// Specifies whether to use consistent read for the get operation.
    /// </summary>
    /// <param name="consistentRead">True for consistent read, false for eventually consistent read.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithConsistentRead(bool consistentRead = true)
    {
        _request.ConsistentRead = consistentRead;
        return this;
    }

    /// <summary>
    /// Determines the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Sets expression attribute names for use in projection expressions.
    /// </summary>
    /// <param name="expressionAttributeNames">The expression attribute names.</param>
    /// <returns>The GetItemBuilder instance for method chaining.</returns>
    public GetItemBuilder WithExpressionAttributeNames(Dictionary<string, string> expressionAttributeNames)
    {
        _request.ExpressionAttributeNames = expressionAttributeNames;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured GetItemRequest.
    /// </summary>
    /// <returns>The configured GetItemRequest instance.</returns>
    public GetItemRequest Build()
    {
        return _request;
    }
}
