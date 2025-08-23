using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Fluent builder for constructing DynamoDB TransactGet requests with a user-friendly API.
/// </summary>
public class TransactGetBuilder
{
    private readonly TransactGetRequest _request = new();

    /// <summary>
    /// Adds a get operation to the transaction.
    /// </summary>
    /// <param name="tableName">The name of the table to get the item from.</param>
    /// <param name="key">The primary key of the item to get.</param>
    /// <returns>The TransactGetBuilder instance for method chaining.</returns>
    public TransactGetBuilder WithGet(string tableName, Dictionary<string, AttributeValue> key)
    {
        _request.TransactItems.Add(new TransactGetItem
        {
            Get = new()
            {
                TableName = tableName,
                Key = key
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a get operation to the transaction with projection expression.
    /// </summary>
    /// <param name="tableName">The name of the table to get the item from.</param>
    /// <param name="key">The primary key of the item to get.</param>
    /// <param name="projectionExpression">The projection expression specifying which attributes to retrieve.</param>
    /// <param name="expressionAttributeNames">Expression attribute names for the projection.</param>
    /// <returns>The TransactGetBuilder instance for method chaining.</returns>
    public TransactGetBuilder WithGet(string tableName, Dictionary<string, AttributeValue> key, string projectionExpression,
        Dictionary<string, string>? expressionAttributeNames = null)
    {
        _request.TransactItems.Add(new TransactGetItem
        {
            Get = new TransactGetItemRequest
            {
                TableName = tableName,
                Key = key,
                ProjectionExpression = projectionExpression,
                ExpressionAttributeNames = expressionAttributeNames
            }
        });
        return this;
    }

    /// <summary>
    /// Adds a get operation to the transaction with specific attributes to retrieve.
    /// </summary>
    /// <param name="tableName">The name of the table to get the item from.</param>
    /// <param name="key">The primary key of the item to get.</param>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The TransactGetBuilder instance for method chaining.</returns>
    public TransactGetBuilder WithGet(string tableName, Dictionary<string, AttributeValue> key, params string[] attributes)
    {
        if (attributes?.Length > 0)
        {
            var projectionExpression = string.Join(", ", attributes);
            return WithGet(tableName, key, projectionExpression);
        }

        return WithGet(tableName, key);
    }

    /// <summary>
    /// Builds and returns the configured TransactGetRequest.
    /// </summary>
    /// <returns>The configured TransactGetRequest instance.</returns>
    public TransactGetRequest Build()
    {
        return _request;
    }
}
