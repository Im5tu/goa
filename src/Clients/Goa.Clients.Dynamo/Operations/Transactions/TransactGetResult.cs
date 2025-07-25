using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Result item from TransactGetItem operation.
/// </summary>
public class TransactGetResult
{
    /// <summary>
    /// Map of attribute names to values for the requested item, or null if the item was not found.
    /// </summary>
    public DynamoRecord? Item { get; set; }
}