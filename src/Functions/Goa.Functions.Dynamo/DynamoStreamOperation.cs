namespace Goa.Functions.Dynamo;

/// <summary>
/// Represents the type of operation that triggered a DynamoDB stream event
/// </summary>
public enum DynamoStreamOperation
{
    /// <summary>
    /// A new item was added to the table
    /// </summary>
    INSERT,

    /// <summary>
    /// An existing item was modified
    /// </summary>
    MODIFY,

    /// <summary>
    /// An item was removed from the table
    /// </summary>
    REMOVE
}
