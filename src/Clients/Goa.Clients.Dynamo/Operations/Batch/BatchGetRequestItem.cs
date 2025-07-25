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
}