namespace Goa.Clients.Dynamo.Operations.Transactions;

/// <summary>
/// Types of transaction operations.
/// </summary>
public enum TransactOperationType
{
    /// <summary>
    /// Put a new item or replace an existing item in the transaction.
    /// </summary>
    Put,
    
    /// <summary>
    /// Update an existing item in the transaction.
    /// </summary>
    Update,
    
    /// <summary>
    /// Delete an existing item in the transaction.
    /// </summary>
    Delete,
    
    /// <summary>
    /// Check that an item exists or check the condition of an item's attributes in the transaction.
    /// </summary>
    ConditionCheck
}