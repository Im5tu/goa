using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Fluent builder for constructing DynamoDB TransactWrite requests with a user-friendly API.
/// </summary>
public class TransactWriteBuilder
{
    private readonly TransactWriteRequest _request = new()
    {
        TransactItems = new List<TransactWriteItem>()
    };

    /// <summary>
    /// Adds a condition check operation to the transaction.
    /// </summary>
    /// <param name="tableName">The name of the table containing the item.</param>
    /// <param name="key">The primary key of the item to check.</param>
    /// <param name="conditionExpression">The condition expression that must be satisfied.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithConditionCheck(string tableName, Dictionary<string, AttributeValue> key, string conditionExpression)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            ConditionCheck = new TransactConditionCheckItem
            {
                TableName = tableName,
                Key = key,
                ConditionExpression = conditionExpression
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a condition check operation to the transaction with attribute names and values.
    /// </summary>
    /// <param name="tableName">The name of the table containing the item.</param>
    /// <param name="key">The primary key of the item to check.</param>
    /// <param name="conditionExpression">The condition expression that must be satisfied.</param>
    /// <param name="expressionAttributeNames">Expression attribute names for the condition.</param>
    /// <param name="expressionAttributeValues">Expression attribute values for the condition.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithConditionCheck(string tableName, Dictionary<string, AttributeValue> key, string conditionExpression, 
        Dictionary<string, string>? expressionAttributeNames = null, Dictionary<string, AttributeValue>? expressionAttributeValues = null)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            ConditionCheck = new TransactConditionCheckItem
            {
                TableName = tableName,
                Key = key,
                ConditionExpression = conditionExpression,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a put operation to the transaction.
    /// </summary>
    /// <param name="tableName">The name of the table to put the item into.</param>
    /// <param name="item">The item to put.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithPut(string tableName, Dictionary<string, AttributeValue> item)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Put = new TransactPutItem
            {
                TableName = tableName,
                Item = item
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a put operation to the transaction with a condition.
    /// </summary>
    /// <param name="tableName">The name of the table to put the item into.</param>
    /// <param name="item">The item to put.</param>
    /// <param name="conditionExpression">The condition expression that must be satisfied.</param>
    /// <param name="expressionAttributeNames">Expression attribute names for the condition.</param>
    /// <param name="expressionAttributeValues">Expression attribute values for the condition.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithPut(string tableName, Dictionary<string, AttributeValue> item, string conditionExpression,
        Dictionary<string, string>? expressionAttributeNames = null, Dictionary<string, AttributeValue>? expressionAttributeValues = null)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Put = new TransactPutItem
            {
                TableName = tableName,
                Item = item,
                ConditionExpression = conditionExpression,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a delete operation to the transaction.
    /// </summary>
    /// <param name="tableName">The name of the table to delete the item from.</param>
    /// <param name="key">The primary key of the item to delete.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithDelete(string tableName, Dictionary<string, AttributeValue> key)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Delete = new TransactDeleteItem
            {
                TableName = tableName,
                Key = key
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a delete operation to the transaction with a condition.
    /// </summary>
    /// <param name="tableName">The name of the table to delete the item from.</param>
    /// <param name="key">The primary key of the item to delete.</param>
    /// <param name="conditionExpression">The condition expression that must be satisfied.</param>
    /// <param name="expressionAttributeNames">Expression attribute names for the condition.</param>
    /// <param name="expressionAttributeValues">Expression attribute values for the condition.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithDelete(string tableName, Dictionary<string, AttributeValue> key, string conditionExpression,
        Dictionary<string, string>? expressionAttributeNames = null, Dictionary<string, AttributeValue>? expressionAttributeValues = null)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Delete = new TransactDeleteItem
            {
                TableName = tableName,
                Key = key,
                ConditionExpression = conditionExpression,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            }
        });
        return this;
    }

    /// <summary>
    /// Adds an update operation to the transaction.
    /// </summary>
    /// <param name="tableName">The name of the table to update the item in.</param>
    /// <param name="key">The primary key of the item to update.</param>
    /// <param name="updateExpression">The update expression.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithUpdate(string tableName, Dictionary<string, AttributeValue> key, string updateExpression)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Update = new TransactUpdateItem
            {
                TableName = tableName,
                Key = key,
                UpdateExpression = updateExpression
            }
        });
        return this;
    }

    /// <summary>
    /// Adds an update operation to the transaction with expressions and conditions.
    /// </summary>
    /// <param name="tableName">The name of the table to update the item in.</param>
    /// <param name="key">The primary key of the item to update.</param>
    /// <param name="updateExpression">The update expression.</param>
    /// <param name="conditionExpression">The condition expression that must be satisfied.</param>
    /// <param name="expressionAttributeNames">Expression attribute names.</param>
    /// <param name="expressionAttributeValues">Expression attribute values.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithUpdate(string tableName, Dictionary<string, AttributeValue> key, string updateExpression, 
        string? conditionExpression = null, Dictionary<string, string>? expressionAttributeNames = null, 
        Dictionary<string, AttributeValue>? expressionAttributeValues = null)
    {
        _request.TransactItems.Add(new TransactWriteItem
        {
            Update = new TransactUpdateItem
            {
                TableName = tableName,
                Key = key,
                UpdateExpression = updateExpression,
                ConditionExpression = conditionExpression,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            }
        });
        return this;
    }

    /// <summary>
    /// Sets the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Sets whether to return item collection metrics.
    /// </summary>
    /// <param name="returnItemCollectionMetrics">The item collection metrics setting.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithReturnItemCollectionMetrics(ReturnItemCollectionMetrics returnItemCollectionMetrics)
    {
        _request.ReturnItemCollectionMetrics = returnItemCollectionMetrics;
        return this;
    }

    /// <summary>
    /// Sets a client request token to make the request idempotent.
    /// </summary>
    /// <param name="clientRequestToken">The client request token.</param>
    /// <returns>The TransactWriteBuilder instance for method chaining.</returns>
    public TransactWriteBuilder WithClientRequestToken(string clientRequestToken)
    {
        _request.ClientRequestToken = clientRequestToken;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured TransactWriteRequest.
    /// </summary>
    /// <returns>The configured TransactWriteRequest instance.</returns>
    public TransactWriteRequest Build()
    {
        return _request;
    }
}