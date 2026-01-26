using System.Text.Json;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Represents the execution of a tool during a chat response.
/// </summary>
public sealed class ToolExecution
{
    /// <summary>
    /// Gets the name of the tool that was executed.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier for this tool use request.
    /// </summary>
    public string ToolUseId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the input provided to the tool.
    /// </summary>
    public JsonDocument Input { get; init; } = JsonDocument.Parse("{}");

    /// <summary>
    /// Gets the result returned from the tool execution.
    /// </summary>
    public string Result { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the tool execution was successful.
    /// </summary>
    public bool Success { get; init; }
}
