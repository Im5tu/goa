using System.Text.Json;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A block of content in a message. Only one type of content can be set per block.
/// </summary>
public class ContentBlock
{
    /// <summary>
    /// Text content.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Image content.
    /// </summary>
    public ImageBlock? Image { get; set; }

    /// <summary>
    /// Document content.
    /// </summary>
    public DocumentBlock? Document { get; set; }

    /// <summary>
    /// Tool use content (when the model wants to use a tool).
    /// </summary>
    public ToolUseBlock? ToolUse { get; set; }

    /// <summary>
    /// Tool result content (response from a tool invocation).
    /// </summary>
    public ToolResultBlock? ToolResult { get; set; }
}
