using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Typed result wrapper for TransactGetItems operations with direct deserialization support.
/// </summary>
public sealed class TransactGetResult<T>
{
    /// <summary>
    /// The items retrieved, in the same order as the request. Null entries indicate items that were not found.
    /// </summary>
    public List<T?> Items { get; set; } = new();

    /// <summary>
    /// The capacity consumed by the operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}
