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
}