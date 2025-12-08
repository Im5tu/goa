using System.Text.Json.Serialization;

namespace Goa.Functions.Core;

/// <summary>
/// Response type for Lambda batch item failures (SQS, Kinesis)
/// </summary>
public sealed class BatchItemFailureResponse
{
    /// <summary>
    /// Gets or sets the list of failed batch items
    /// </summary>
    [JsonPropertyName("batchItemFailures")]
    public List<BatchItemFailure> BatchItemFailures { get; set; } = [];
}