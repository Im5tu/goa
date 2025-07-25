using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Request for transactional write operations.
/// </summary>
public class TransactWriteRequest
{
    /// <summary>
    /// An ordered array of up to 100 TransactWriteItem objects, each of which contains a ConditionCheck, Put, Update, or Delete operation.
    /// </summary>
    public List<TransactWriteItem> TransactItems { get; set; } = new();

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;

    /// <summary>
    /// Determines whether item collection metrics are returned.
    /// </summary>
    public ReturnItemCollectionMetrics ReturnItemCollectionMetrics { get; set; } = ReturnItemCollectionMetrics.NONE;

    /// <summary>
    /// Providing a ClientRequestToken makes the call to TransactWriteItems idempotent,
    /// meaning that multiple identical calls have the same effect as one single call.
    /// </summary>
    public string? ClientRequestToken { get; set; }
}
