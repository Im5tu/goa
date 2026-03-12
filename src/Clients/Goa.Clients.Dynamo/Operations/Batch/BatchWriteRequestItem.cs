using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Individual write request within a BatchWriteItem operation.
/// </summary>
public sealed class BatchWriteRequestItem
{
    /// <summary>
    /// A request to perform a PutItem operation.
    /// </summary>
    [JsonPropertyName("PutRequest")]
    public PutRequest? PutRequest { get; set; }

    /// <summary>
    /// A request to perform a DeleteItem operation.
    /// </summary>
    [JsonPropertyName("DeleteRequest")]
    public DeleteRequest? DeleteRequest { get; set; }
}
