namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Request for batch writing items to DynamoDB.
/// </summary>
public class BatchWriteItemRequest
{
    /// <summary>
    /// A map of one or more table names and, for each table, a list of operations to be performed.
    /// </summary>
    public Dictionary<string, List<BatchWriteRequestItem>> RequestItems { get; set; } = new();
}