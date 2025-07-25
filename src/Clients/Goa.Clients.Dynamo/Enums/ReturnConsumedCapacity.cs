namespace Goa.Clients.Dynamo.Enums;

/// <summary>
/// Determines the level of detail about provisioned throughput consumption that is returned in the response.
/// </summary>
public enum ReturnConsumedCapacity
{
    /// <summary>
    /// No ConsumedCapacity details are included in the response.
    /// </summary>
    NONE,
    
    /// <summary>
    /// The response includes the aggregate ConsumedCapacity for the operation, together with ConsumedCapacity 
    /// for each table and secondary index that was accessed.
    /// </summary>
    INDEXES,
    
    /// <summary>
    /// The response includes only the aggregate ConsumedCapacity for the operation.
    /// </summary>
    TOTAL
}