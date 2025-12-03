using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Individual table request within a BatchGetItem operation.
/// </summary>
public class BatchGetRequestItem
{
    /// <summary>
    /// The primary keys of the items to retrieve. For each key, DynamoDB performs a GetItem operation and returns the entire item.
    /// </summary>
    public List<Dictionary<string, AttributeValue>> Keys { get; set; } = new();

    /// <summary>
    /// A string that identifies one or more attributes to retrieve from the table.
    /// These attributes can include scalars, sets, or elements of a JSON document.
    /// </summary>
    public string? ProjectionExpression { get; set; }

    /// <summary>
    /// The consistency of a read operation. If set to true, then a strongly consistent read is used; otherwise, an eventually consistent read is used.
    /// </summary>
    public bool ConsistentRead { get; set; }

    /// <summary>
    /// One or more substitution tokens for attribute names in the ProjectionExpression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }
}