using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Contains details about the type of identity that made the request
/// </summary>
public class Identity
{
    /// <summary>
    /// Gets or sets the principal ID of the requester
    /// </summary>
    [JsonPropertyName("principalId")]
    public string? PrincipalId { get; set; }

    /// <summary>
    /// Gets or sets the type of identity
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
