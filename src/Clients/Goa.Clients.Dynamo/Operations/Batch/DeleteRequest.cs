using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Delete request within a batch write operation.
/// </summary>
public class DeleteRequest
{
    /// <summary>
    /// The primary key of the item to be deleted.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();
}