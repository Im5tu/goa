namespace Goa.Clients.Dynamo.Enums;

/// <summary>
/// Determines whether item collection metrics are returned.
/// </summary>
public enum ReturnItemCollectionMetrics
{
    /// <summary>
    /// No ItemCollectionMetrics are returned.
    /// </summary>
    NONE,
    
    /// <summary>
    /// Statistics about item collections, if any, that were modified during the operation are returned in the response.
    /// </summary>
    SIZE
}