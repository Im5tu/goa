using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents user identity information in an S3 event
/// </summary>
public class S3Identity
{
    /// <summary>
    /// Gets or sets the principal ID of the user who caused the event
    /// </summary>
    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; set; }
}