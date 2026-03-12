using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Individual transact write item.
/// </summary>
public sealed class TransactWriteItem
{
    /// <summary>
    /// A request to perform a PutItem operation.
    /// </summary>
    [JsonPropertyName("Put")]
    public TransactPutItem? Put { get; set; }

    /// <summary>
    /// A request to perform an UpdateItem operation.
    /// </summary>
    [JsonPropertyName("Update")]
    public TransactUpdateItem? Update { get; set; }

    /// <summary>
    /// A request to perform a DeleteItem operation.
    /// </summary>
    [JsonPropertyName("Delete")]
    public TransactDeleteItem? Delete { get; set; }

    /// <summary>
    /// A request to perform a condition check.
    /// </summary>
    [JsonPropertyName("ConditionCheck")]
    public TransactConditionCheckItem? ConditionCheck { get; set; }
}
