namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents consumed capacity information for DynamoDB operations.
/// </summary>
public class ConsumedCapacity
{
    /// <summary>
    /// The name of the table that was affected by the operation.
    /// </summary>
    public string? TableName { get; set; }
    
    /// <summary>
    /// The total number of capacity units consumed by the operation.
    /// </summary>
    public double? CapacityUnits { get; set; }
    
    /// <summary>
    /// The total number of read capacity units consumed by the operation.
    /// </summary>
    public double? ReadCapacityUnits { get; set; }
    
    /// <summary>
    /// The total number of write capacity units consumed by the operation.
    /// </summary>
    public double? WriteCapacityUnits { get; set; }
    
    /// <summary>
    /// The capacity consumed by the global secondary indexes affected by the operation.
    /// </summary>
    public Dictionary<string, ConsumedCapacity>? GlobalSecondaryIndexes { get; set; }
    
    /// <summary>
    /// The capacity consumed by the local secondary indexes affected by the operation.
    /// </summary>
    public Dictionary<string, ConsumedCapacity>? LocalSecondaryIndexes { get; set; }
    
    /// <summary>
    /// The capacity consumed by the table itself.
    /// </summary>
    public CapacityDetail? Table { get; set; }
}