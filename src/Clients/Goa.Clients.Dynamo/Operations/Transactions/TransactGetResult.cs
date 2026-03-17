using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Result item from TransactGetItem operation.
/// </summary>
public sealed class TransactGetResult
{
    /// <summary>
    /// Map of attribute names to values for the requested item, or null if the item was not found.
    /// </summary>
    [JsonPropertyName("Item")]
    public DynamoRecord? Item { get; set; }
}
