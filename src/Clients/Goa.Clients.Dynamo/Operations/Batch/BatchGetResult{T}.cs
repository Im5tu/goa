using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Typed result wrapper for BatchGetItem operations with direct deserialization support.
/// </summary>
public sealed class BatchGetResult<T>
{
    /// <summary>
    /// The items retrieved by table name.
    /// </summary>
    public Dictionary<string, List<T>> Responses { get; set; } = new();

    /// <summary>
    /// Keys that were not processed and need to be retried.
    /// </summary>
    public Dictionary<string, BatchGetRequestItem>? UnprocessedKeys { get; set; }

    /// <summary>
    /// Gets a value indicating whether there are unprocessed keys requiring additional requests.
    /// </summary>
    public bool HasUnprocessedKeys => UnprocessedKeys?.Count > 0;

    /// <summary>
    /// The capacity consumed by the operation.
    /// </summary>
    public List<ConsumedCapacity>? ConsumedCapacity { get; set; }
}
