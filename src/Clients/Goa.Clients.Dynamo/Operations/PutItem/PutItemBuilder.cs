using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.PutItem;

/// <summary>
/// Fluent builder for constructing DynamoDB PutItem requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to put the item into.</param>
public class PutItemBuilder(string tableName)
{
    private readonly PutItemRequest _request = new()
    {
        TableName = tableName,
        Item = new Dictionary<string, AttributeValue>()
    };

    /// <summary>
    /// Sets the item to be put into the table.
    /// </summary>
    /// <param name="item">The item attributes as a dictionary.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithItem(Dictionary<string, AttributeValue> item)
    {
        _request.Item = item ?? new (StringComparer.OrdinalIgnoreCase);
        return this;
    }

    /// <summary>
    /// Adds a single attribute to the item. Supports implicit conversions to AttributeValue
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithAttribute(string attributeName, AttributeValue value)
    {
        _request.Item[attributeName] = value;
        return this;
    }

    /// <summary>
    /// Sets a condition expression that must be satisfied for the put operation to succeed.
    /// Multiple conditions are combined with AND.
    /// </summary>
    /// <param name="condition">The condition that must be met.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithCondition(Condition condition)
    {
        if (string.IsNullOrEmpty(condition.Expression))
        {
            return this;
        }

        if (string.IsNullOrEmpty(_request.ConditionExpression))
        {
            _request.ConditionExpression = condition.Expression;
        }
        else
        {
            _request.ConditionExpression = $"({_request.ConditionExpression}) AND ({condition.Expression})";
        }

        if (condition.ExpressionNames.Count > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        }

        if (condition.ExpressionValues.Count > 0)
        {
            _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        }

        return this;
    }

    /// <summary>
    /// Determines the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Determines what item attributes to return in the response.
    /// </summary>
    /// <param name="returnValues">The return values setting.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithReturnValues(ReturnValues returnValues)
    {
        _request.ReturnValues = returnValues;
        return this;
    }

    /// <summary>
    /// Specifies how to return attribute values when a conditional check fails.
    /// </summary>
    /// <param name="returnValuesOnConditionCheckFailure">The return values on condition check failure setting.</param>
    /// <returns>The PutItemBuilder instance for method chaining.</returns>
    public PutItemBuilder WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure returnValuesOnConditionCheckFailure)
    {
        _request.ReturnValuesOnConditionCheckFailure = returnValuesOnConditionCheckFailure;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured PutItemRequest.
    /// </summary>
    /// <returns>The configured PutItemRequest instance.</returns>
    public PutItemRequest Build()
    {
        return _request;
    }
}
