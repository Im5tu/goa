namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Detailed capacity information for DynamoDB operations.
/// </summary>
public class CapacityDetail
{
    /// <summary>
    /// The number of read capacity units consumed by the operation.
    /// </summary>
    public double? ReadCapacityUnits { get; set; }
    
    /// <summary>
    /// The number of write capacity units consumed by the operation.
    /// </summary>
    public double? WriteCapacityUnits { get; set; }
    
    /// <summary>
    /// The total number of capacity units consumed by the operation.
    /// </summary>
    public double? CapacityUnits { get; set; }
}