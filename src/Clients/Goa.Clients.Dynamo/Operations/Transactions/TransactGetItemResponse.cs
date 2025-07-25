using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Response for TransactGetItem operations.
/// </summary>
public class TransactGetItemResponse
{
    /// <summary>
    /// An ordered array of up to 100 response items, each of which corresponds to the TransactGetItem request in the same position.
    /// </summary>
    public List<TransactGetResult> Responses { get; set; } = new();
    
    /// <summary>
    /// The capacity units consumed by the entire TransactGetItem operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}