using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.DeleteItem;

/// <summary>
/// Fluent builder for constructing DynamoDB DeleteItem requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to delete the item from.</param>
public class DeleteItemBuilder(string tableName)
{
    private readonly DeleteItemRequest _request = new()
    {
        TableName = tableName,
        Key = new Dictionary<string, AttributeValue>()
    };

    /// <summary>
    /// Adds a key attribute to identify the item to delete.
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The DeleteItemBuilder instance for method chaining.</returns>
    public DeleteItemBuilder WithKey(string attributeName, AttributeValue value)
    {
        _request.Key[attributeName] = value;
        return this;
    }

    /// <summary>
    /// Sets a condition expression that must be satisfied for the delete operation to succeed.
    /// </summary>
    /// <param name="condition">The condition that must be met.</param>
    /// <returns>The DeleteItemBuilder instance for method chaining.</returns>
    public DeleteItemBuilder WithCondition(Condition condition)
    {
        _request.ConditionExpression = condition.Expression;
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        return this;
    }

    /// <summary>
    /// Sets the return values for the delete operation. For DeleteItem operations, only NONE and ALL_OLD values are supported.
    /// </summary>
    /// <param name="returnValues">The return values option.</param>
    /// <returns>The DeleteItemBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported ReturnValues option is provided for DeleteItem.</exception>
    public DeleteItemBuilder WithReturnValues(ReturnValues returnValues)
    {
        if (returnValues != ReturnValues.NONE && returnValues != ReturnValues.ALL_OLD)
        {
            throw new ArgumentException($"DeleteItem operation only supports NONE and ALL_OLD return values. Provided: {returnValues}", nameof(returnValues));
        }

        _request.ReturnValues = returnValues;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured DeleteItemRequest.
    /// </summary>
    /// <returns>The configured DeleteItemRequest instance.</returns>
    public DeleteItemRequest Build()
    {
        return _request;
    }
}
