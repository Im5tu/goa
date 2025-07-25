using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Response for TransactWriteItem operations.
/// </summary>
public class TransactWriteItemResponse
{
    /// <summary>
    /// The capacity units consumed by the entire TransactWriteItem operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
    
    /// <summary>
    /// A list of tables that were processed by TransactWriteItem and, for each table, information about any item collections that were affected by individual operations.
    /// </summary>
    public Dictionary<string, object>? ItemCollectionMetrics { get; set; }
}