namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A tool result block containing the response from a tool invocation.
/// </summary>
public class ToolResultBlock
{
    /// <summary>
    /// The identifier of the tool use request this is a response to.
    /// </summary>
    public string ToolUseId { get; set; } = string.Empty;

    /// <summary>
    /// The content of the tool result.
    /// </summary>
    public List<ContentBlock> Content { get; set; } = new();

    /// <summary>
    /// The status of the tool result (e.g., "success", "error").
    /// </summary>
    public string? Status { get; set; }
}
