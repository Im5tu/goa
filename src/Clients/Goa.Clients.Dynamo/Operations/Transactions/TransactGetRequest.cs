using Goa.Clients.Dynamo.Enums;

namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Request for transactional get operations.
/// </summary>
public class TransactGetRequest
{
    /// <summary>
    /// An ordered array of up to 100 TransactGetItem objects, each of which contains a Get operation.
    /// </summary>
    public List<TransactGetItem> TransactItems { get; set; } = new();

    /// <summary>
    /// Determines the level of detail about provisioned throughput consumption that is returned in the response.
    /// </summary>
    public ReturnConsumedCapacity ReturnConsumedCapacity { get; set; } = ReturnConsumedCapacity.NONE;
}