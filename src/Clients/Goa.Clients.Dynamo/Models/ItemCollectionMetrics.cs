namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Information about item collections, if any, that were affected by the operation.
/// </summary>
public class ItemCollectionMetrics
{
    /// <summary>
    /// The partition key value of the item collection.
    /// </summary>
    public Dictionary<string, AttributeValue>? ItemCollectionKey { get; set; }

    /// <summary>
    /// An estimate of item collection size, in gigabytes. This value is a two-element array
    /// containing a lower bound and an upper bound for the estimate.
    /// </summary>
    public List<double>? SizeEstimateRangeGB { get; set; }
}
