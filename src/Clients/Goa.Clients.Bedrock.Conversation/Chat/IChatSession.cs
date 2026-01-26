using ErrorOr;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Represents an active chat session with a Bedrock model.
/// </summary>
public interface IChatSession : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier of the conversation.
    /// Null if the session is not persisted.
    /// </summary>
    string? ConversationId { get; }

    /// <summary>
    /// Gets the total token usage accumulated during this session.
    /// </summary>
    TokenUsage TotalTokenUsage { get; }

    /// <summary>
    /// Registers a tool that can be used by the model during the conversation.
    /// </summary>
    /// <param name="tool">The tool definition to register.</param>
    /// <returns>The current session for method chaining.</returns>
    IChatSession RegisterTool(McpToolDefinition tool);

    /// <summary>
    /// Registers multiple tools that can be used by the model during the conversation.
    /// </summary>
    /// <param name="tools">The tool definitions to register.</param>
    /// <returns>The current session for method chaining.</returns>
    IChatSession RegisterTools(IEnumerable<McpToolDefinition> tools);

    /// <summary>
    /// Sends a text message to the model and returns the response.
    /// </summary>
    /// <param name="message">The text message to send.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The model's response, or an error if the operation failed.</returns>
    Task<ErrorOr<ChatResponse>> SendAsync(string message, CancellationToken ct = default);

    /// <summary>
    /// Sends content blocks to the model and returns the response.
    /// </summary>
    /// <param name="content">The content blocks to send.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The model's response, or an error if the operation failed.</returns>
    Task<ErrorOr<ChatResponse>> SendAsync(IEnumerable<ContentBlock> content, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the conversation history.
    /// </summary>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The list of messages in the conversation, or an error if the operation failed.</returns>
    Task<ErrorOr<IReadOnlyList<ChatMessage>>> GetHistoryAsync(CancellationToken ct = default);
}
