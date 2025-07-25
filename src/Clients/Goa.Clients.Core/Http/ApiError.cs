using System.Net;
using System.Text.Json.Serialization;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Represents an API error with message, type, code, and additional context information.
/// </summary>
/// <param name="Message">The error message describing what went wrong.</param>
/// <param name="Type">The specific type or category of the error.</param>
/// <param name="Code">The error code for programmatic identification.</param>
public record ApiError(
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("__type")] string? Type = null,
    [property: JsonPropertyName("code")] string? Code = null)
{
    /// <summary>
    /// Gets or sets the raw payload that caused the error, if available.
    /// </summary>
    public string? Payload { get; set; }
    
    /// <summary>
    /// Gets or sets the HTTP status code associated with this error.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }
}
