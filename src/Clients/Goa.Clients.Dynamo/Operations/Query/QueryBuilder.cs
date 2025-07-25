using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.Query;

/// <summary>
/// Fluent builder for constructing DynamoDB Query requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to query.</param>
public class QueryBuilder(string tableName)
{
    private readonly QueryRequest _request = new()
    {
        TableName = tableName
    };

    /// <summary>
    /// Sets the key condition expression for the query.
    /// </summary>
    /// <param name="condition">The condition to apply to the key attributes.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithKey(Condition condition)
    {
        _request.KeyConditionExpression = condition.Expression;
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        return this;
    }

    /// <summary>
    /// Specifies whether to use consistent read for the query.
    /// </summary>
    /// <param name="consistentRead">True for consistent read, false for eventually consistent read.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithConsistentRead(bool consistentRead = true)
    {
        _request.ConsistentRead = consistentRead;
        return this;
    }

    /// <summary>
    /// Specifies the Global Secondary Index (GSI) or Local Secondary Index (LSI) to query, or null to query the base table.
    /// </summary>
    /// <param name="indexName">The name of the index to query, or null to query the base table.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithIndex(string? indexName)
    {
        _request.IndexName = string.IsNullOrWhiteSpace(indexName) ? null : indexName;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of items to return from the query.
    /// </summary>
    /// <param name="limit">The maximum number of items to return, or null for no limit.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithLimit(int? limit)
    {
        _request.Limit = limit > 0 ? limit : null;
        return this;
    }

    /// <summary>
    /// Sets the query to return items in ascending order by sort key.
    /// </summary>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithOrderAscending()
    {
        _request.ScanIndexForward = true;
        return this;
    }

    /// <summary>
    /// Sets the query to return items in descending order by sort key.
    /// </summary>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithOrderDescending()
    {
        _request.ScanIndexForward = false;
        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the items using a params array for convenience.
    /// </summary>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithProjection(params string[] attributes)
    {
        if (attributes?.Length > 0)
        {
            _request.ProjectionExpression = string.Join(", ", attributes);
            return WithSelectionMode(Select.SPECIFIC_ATTRIBUTES);
        }

        _request.ProjectionExpression = null;
        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the items using an enumerable collection.
    /// </summary>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithProjection(IEnumerable<string> attributes)
    {
        if (attributes?.Any() == true)
        {
            _request.ProjectionExpression = string.Join(", ", attributes);
            return WithSelectionMode(Select.SPECIFIC_ATTRIBUTES);
        }

        _request.ProjectionExpression = null;
        return this;
    }

    /// <summary>
    /// Determines the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Specifies the attributes to be returned in the result.
    /// </summary>
    /// <param name="select">The selection mode for returned attributes.</param>
    /// <returns>The QueryBuilder instance for method chaining.</returns>
    public QueryBuilder WithSelectionMode(Select select)
    {
        _request.Select = select;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured QueryRequest.
    /// </summary>
    /// <returns>The configured QueryRequest instance.</returns>
    public QueryRequest Build()
    {
        return _request;
    }
}

