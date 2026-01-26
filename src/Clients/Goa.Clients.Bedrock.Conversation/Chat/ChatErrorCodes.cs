using ErrorOr;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Static class containing all chat error codes with the Goa.Bedrock.Chat prefix.
/// </summary>
public static class ChatErrorCodes
{
    private const string Prefix = "Goa.Bedrock.Chat";

    /// <summary>
    /// Error code for when the maximum tool iterations limit is exceeded.
    /// </summary>
    public static readonly string MaxToolIterationsExceeded = $"{Prefix}.MaxToolIterationsExceeded";

    /// <summary>
    /// Error code for when a conversation is not found.
    /// </summary>
    public static readonly string ConversationNotFound = $"{Prefix}.ConversationNotFound";

    /// <summary>
    /// Error code for when persistence is required but not configured.
    /// </summary>
    public static readonly string PersistenceNotConfigured = $"{Prefix}.PersistenceNotConfigured";

    /// <summary>
    /// Error code for when a tool execution fails.
    /// </summary>
    public static readonly string ToolExecutionFailed = $"{Prefix}.ToolExecutionFailed";

    /// <summary>
    /// Creates an error for when the maximum tool iterations limit is exceeded.
    /// </summary>
    /// <param name="maxIterations">The maximum number of iterations that was exceeded.</param>
    /// <returns>An Error representing the limit exceeded condition.</returns>
    public static Error MaxToolIterationsExceededError(int maxIterations) =>
        Error.Failure(MaxToolIterationsExceeded, $"Maximum tool iterations ({maxIterations}) exceeded.");

    /// <summary>
    /// Creates an error for when a conversation is not found.
    /// </summary>
    /// <param name="conversationId">The conversation identifier that was not found.</param>
    /// <returns>An Error representing the not found condition.</returns>
    public static Error ConversationNotFoundError(string conversationId) =>
        Error.NotFound(ConversationNotFound, $"Conversation with id '{conversationId}' was not found.");

    /// <summary>
    /// Creates an error for when persistence is required but not configured.
    /// </summary>
    /// <returns>An Error representing the configuration failure.</returns>
    public static Error PersistenceNotConfiguredError() =>
        Error.Failure(PersistenceNotConfigured, "Conversation persistence is required but no conversation store is configured.");

    /// <summary>
    /// Creates an error for when a tool execution fails.
    /// </summary>
    /// <param name="toolName">The name of the tool that failed.</param>
    /// <param name="reason">The reason for the failure.</param>
    /// <returns>An Error representing the tool execution failure.</returns>
    public static Error ToolExecutionFailedError(string toolName, string reason) =>
        Error.Failure(ToolExecutionFailed, $"Tool '{toolName}' execution failed: {reason}");
}
