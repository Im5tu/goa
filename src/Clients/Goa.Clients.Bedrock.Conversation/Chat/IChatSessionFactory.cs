using ErrorOr;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Factory for creating and resuming chat sessions.
/// </summary>
public interface IChatSessionFactory
{
    /// <summary>
    /// Creates a new chat session with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for the session.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The created chat session, or an error if the operation failed.</returns>
    Task<ErrorOr<IChatSession>> CreateAsync(ChatSessionOptions options, CancellationToken ct = default);

    /// <summary>
    /// Resumes an existing chat session from a persisted conversation.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to resume.</param>
    /// <param name="options">Optional configuration options to override session settings.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>The resumed chat session, or an error if the conversation was not found.</returns>
    Task<ErrorOr<IChatSession>> ResumeAsync(string conversationId, ChatSessionOptions? options = null, CancellationToken ct = default);
}
