namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Individual transact write item.
/// </summary>
public class TransactWriteItem
{
    /// <summary>
    /// A request to perform a PutItem operation.
    /// </summary>
    public TransactPutItem? Put { get; set; }
    
    /// <summary>
    /// A request to perform an UpdateItem operation.
    /// </summary>
    public TransactUpdateItem? Update { get; set; }
    
    /// <summary>
    /// A request to perform a DeleteItem operation.
    /// </summary>
    public TransactDeleteItem? Delete { get; set; }
    
    /// <summary>
    /// A request to perform a condition check.
    /// </summary>
    public TransactConditionCheckItem? ConditionCheck { get; set; }
}