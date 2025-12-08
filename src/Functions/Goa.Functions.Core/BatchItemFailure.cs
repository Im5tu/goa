using System.Text.Json.Serialization;

namespace Goa.Functions.Core;

/// <summary>
/// Represents a single failed item in a batch
/// </summary>
public sealed class BatchItemFailure
{
    /// <summary>
    /// Gets or sets the identifier of the failed item (MessageId for SQS, SequenceNumber for Kinesis)
    /// </summary>
    [JsonPropertyName("itemIdentifier")]
    public string? ItemIdentifier { get; set; }
}