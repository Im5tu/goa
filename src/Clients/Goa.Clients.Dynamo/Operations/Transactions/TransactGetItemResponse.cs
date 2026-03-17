using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Response for TransactGetItem operations.
/// </summary>
public sealed class TransactGetItemResponse
{
    /// <summary>
    /// An ordered array of up to 100 response items, each of which corresponds to the TransactGetItem request in the same position.
    /// </summary>
    [JsonPropertyName("Responses")]
    public List<TransactGetResult> Responses { get; set; } = new();

    /// <summary>
    /// The capacity units consumed by the entire TransactGetItem operation.
    /// </summary>
    [JsonPropertyName("ConsumedCapacity")]
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}
