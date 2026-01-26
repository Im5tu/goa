using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation;

/// <summary>
/// Interface for storing and retrieving conversations with Bedrock models.
/// </summary>
public interface IConversationStore
{
    /// <summary>
    /// Creates a new conversation with optional metadata.
    /// </summary>
    /// <param name="metadata">Optional metadata for the conversation.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The created conversation, or an error if the operation failed.</returns>
    Task<ErrorOr<Entities.Conversation>> CreateConversationAsync(ConversationMetadata? metadata, CancellationToken ct);

    /// <summary>
    /// Retrieves a conversation by its identifier.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The conversation, or an error if not found or the operation failed.</returns>
    Task<ErrorOr<Entities.Conversation>> GetConversationAsync(string conversationId, CancellationToken ct);

    /// <summary>
    /// Retrieves a conversation with its messages.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="limit">Optional limit on the number of messages to retrieve.</param>
    /// <param name="paginationToken">Optional pagination token for retrieving additional messages.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The conversation with messages, or an error if not found or the operation failed.</returns>
    Task<ErrorOr<ConversationWithMessages>> GetConversationWithMessagesAsync(string conversationId, int? limit, string? paginationToken, CancellationToken ct);

    /// <summary>
    /// Adds a message to a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="role">The role of the entity sending the message.</param>
    /// <param name="message">The message content.</param>
    /// <param name="tokenUsage">Optional token usage information for the message.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The created conversation message, or an error if the operation failed.</returns>
    Task<ErrorOr<ConversationMessage>> AddMessageAsync(string conversationId, ConversationRole role, Message message, TokenUsage? tokenUsage, CancellationToken ct);

    /// <summary>
    /// Adds multiple messages to a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="messages">The messages to add with their roles and optional token usage.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The created conversation messages, or an error if the operation failed.</returns>
    Task<ErrorOr<IReadOnlyList<ConversationMessage>>> AddMessagesAsync(string conversationId, IEnumerable<(ConversationRole Role, Message Message, TokenUsage? TokenUsage)> messages, CancellationToken ct);

    /// <summary>
    /// Updates the metadata of a conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="metadata">The new metadata for the conversation.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The updated conversation, or an error if not found or the operation failed.</returns>
    Task<ErrorOr<Entities.Conversation>> UpdateConversationAsync(string conversationId, ConversationMetadata metadata, CancellationToken ct);

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A Deleted result indicating success, or an error if the operation failed.</returns>
    Task<ErrorOr<Deleted>> DeleteConversationAsync(string conversationId, CancellationToken ct);
}
