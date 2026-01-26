using Goa.Clients.Bedrock.Conversation.Entities;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Configuration options for a chat session.
/// </summary>
public sealed class ChatSessionOptions
{
    /// <summary>
    /// Gets the model identifier to use for the conversation.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets the optional system prompt to guide the model's behavior.
    /// </summary>
    public string? SystemPrompt { get; init; }

    /// <summary>
    /// Gets the maximum number of tokens to generate in the response.
    /// </summary>
    public int MaxTokens { get; init; } = 1024;

    /// <summary>
    /// Gets the temperature for controlling randomness in the response.
    /// Higher values produce more random outputs.
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// Gets the optional top-p (nucleus sampling) value.
    /// </summary>
    public float? TopP { get; init; }

    /// <summary>
    /// Gets the optional stop sequences that cause the model to stop generating.
    /// </summary>
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>
    /// Gets a value indicating whether the conversation should be persisted to storage.
    /// </summary>
    public bool PersistConversation { get; init; }

    /// <summary>
    /// Gets the optional metadata to associate with the conversation.
    /// </summary>
    public ConversationMetadata? Metadata { get; init; }

    /// <summary>
    /// Gets the maximum number of tool execution iterations allowed per message.
    /// </summary>
    public int MaxToolIterations { get; init; } = 10;
}
