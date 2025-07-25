namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Individual transact get item.
/// </summary>
public class TransactGetItem
{
    /// <summary>
    /// Contains the primary key that identifies the item to get, together with the name of the table that contains the item.
    /// </summary>
    public TransactGetItemRequest Get { get; set; } = new();
}