using Goa.Clients.Dynamo.Enums;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.Scan;

/// <summary>
/// Fluent builder for constructing DynamoDB Scan requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to scan.</param>
public class ScanBuilder(string tableName)
{
    private readonly ScanRequest _request = new()
    {
        TableName = tableName
    };

    /// <summary>
    /// Sets the filter expression for the scan.
    /// </summary>
    /// <param name="condition">The condition to apply as a filter.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithFilter(Condition condition)
    {
        _request.FilterExpression = condition.Expression;
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        return this;
    }

    /// <summary>
    /// Specifies whether to use consistent read for the scan.
    /// </summary>
    /// <param name="consistentRead">True for consistent read, false for eventually consistent read.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithConsistentRead(bool consistentRead = true)
    {
        _request.ConsistentRead = consistentRead;
        return this;
    }

    /// <summary>
    /// Specifies the Global Secondary Index (GSI) to scan, or null to scan the base table.
    /// </summary>
    /// <param name="indexName">The name of the index to scan, or null to scan the base table.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithIndex(string? indexName)
    {
        _request.IndexName = string.IsNullOrWhiteSpace(indexName) ? null : indexName;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of items to return from the scan.
    /// </summary>
    /// <param name="limit">The maximum number of items to return, or null for no limit.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithLimit(int? limit)
    {
        _request.Limit = limit > 0 ? limit : null;
        return this;
    }

    /// <summary>
    /// Specifies which attributes to retrieve from the items using a params array for convenience.
    /// </summary>
    /// <param name="attributes">The attribute names to include in the projection.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithProjection(params string[] attributes)
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
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithProjection(IEnumerable<string> attributes)
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
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Specifies the attributes to be returned in the result.
    /// </summary>
    /// <param name="select">The selection mode for returned attributes.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithSelectionMode(Select select)
    {
        _request.Select = select;
        return this;
    }

    /// <summary>
    /// Specifies the number of parallel scan segments for parallel processing.
    /// </summary>
    /// <param name="totalSegments">The total number of segments for parallel scan.</param>
    /// <param name="segment">The segment number (0-based) for this scan operation.</param>
    /// <returns>The ScanBuilder instance for method chaining.</returns>
    public ScanBuilder WithParallelScan(int totalSegments, int segment)
    {
        if (totalSegments > 1 && segment >= 0 && segment < totalSegments)
        {
            _request.TotalSegments = totalSegments;
            _request.Segment = segment;
        }
        return this;
    }

    /// <summary>
    /// Builds and returns the configured ScanRequest.
    /// </summary>
    /// <returns>The configured ScanRequest instance.</returns>
    public ScanRequest Build()
    {
        return _request;
    }
}
