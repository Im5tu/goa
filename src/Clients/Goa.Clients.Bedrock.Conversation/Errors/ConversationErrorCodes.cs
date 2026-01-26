using ErrorOr;

namespace Goa.Clients.Bedrock.Conversation.Errors;

/// <summary>
/// Static class containing all conversation store error codes with the Goa.Bedrock.Conversation prefix.
/// </summary>
public static class ConversationErrorCodes
{
    private const string Prefix = "Goa.Bedrock.Conversation";

    /// <summary>
    /// Error code for when a conversation is not found.
    /// </summary>
    public static readonly string NotFound = $"{Prefix}.NotFound";

    /// <summary>
    /// Error code for when a message is not found.
    /// </summary>
    public static readonly string MessageNotFound = $"{Prefix}.MessageNotFound";

    /// <summary>
    /// Error code for invalid conversation data.
    /// </summary>
    public static readonly string InvalidConversation = $"{Prefix}.InvalidConversation";

    /// <summary>
    /// Error code for invalid message data.
    /// </summary>
    public static readonly string InvalidMessage = $"{Prefix}.InvalidMessage";

    /// <summary>
    /// Error code for concurrent modification conflicts.
    /// </summary>
    public static readonly string ConcurrencyConflict = $"{Prefix}.ConcurrencyConflict";

    /// <summary>
    /// Error code for storage operation failures.
    /// </summary>
    public static readonly string StorageError = $"{Prefix}.StorageError";

    // Conversation validation errors

    /// <summary>
    /// Error code for missing conversation ID.
    /// </summary>
    public static readonly string MissingId = $"{Prefix}.MissingId";

    /// <summary>
    /// Error code for missing conversation CreatedAt timestamp.
    /// </summary>
    public static readonly string MissingCreatedAt = $"{Prefix}.MissingCreatedAt";

    /// <summary>
    /// Error code for missing conversation UpdatedAt timestamp.
    /// </summary>
    public static readonly string MissingUpdatedAt = $"{Prefix}.MissingUpdatedAt";

    // Message validation errors

    /// <summary>
    /// Error code for empty messages collection.
    /// </summary>
    public static readonly string MessagesEmpty = $"{Prefix}.MessagesEmpty";

    /// <summary>
    /// Error code for missing message ID.
    /// </summary>
    public static readonly string MessageMissingId = $"{Prefix}.Message.MissingId";

    /// <summary>
    /// Error code for missing message conversation ID.
    /// </summary>
    public static readonly string MessageMissingConversationId = $"{Prefix}.Message.MissingConversationId";

    /// <summary>
    /// Error code for missing message sequence number.
    /// </summary>
    public static readonly string MessageMissingSequenceNumber = $"{Prefix}.Message.MissingSequenceNumber";

    /// <summary>
    /// Error code for missing message role.
    /// </summary>
    public static readonly string MessageMissingRole = $"{Prefix}.Message.MissingRole";

    /// <summary>
    /// Error code for missing message CreatedAt timestamp.
    /// </summary>
    public static readonly string MessageMissingCreatedAt = $"{Prefix}.Message.MissingCreatedAt";

    /// <summary>
    /// Error code for missing message content.
    /// </summary>
    public static readonly string MessageMissingContent = $"{Prefix}.Message.MissingContent";

    // Pagination errors

    /// <summary>
    /// Error code for invalid pagination token.
    /// </summary>
    public static readonly string PaginationTokenInvalid = $"{Prefix}.PaginationToken.Invalid";

    // ContentBlock errors

    /// <summary>
    /// Error code for empty content block.
    /// </summary>
    public static readonly string ContentBlockEmpty = $"{Prefix}.ContentBlock.Empty";

    /// <summary>
    /// Error code for invalid content block format.
    /// </summary>
    public static readonly string ContentBlockInvalidFormat = $"{Prefix}.ContentBlock.InvalidFormat";

    /// <summary>
    /// Error code for missing content block type.
    /// </summary>
    public static readonly string ContentBlockMissingType = $"{Prefix}.ContentBlock.MissingType";

    /// <summary>
    /// Error code for unknown content block type.
    /// </summary>
    public static readonly string ContentBlockUnknownType = $"{Prefix}.ContentBlock.UnknownType";

    /// <summary>
    /// Error code for missing text in text content block.
    /// </summary>
    public static readonly string ContentBlockTextMissing = $"{Prefix}.ContentBlock.Text.Missing";

    /// <summary>
    /// Error code for missing image format.
    /// </summary>
    public static readonly string ContentBlockImageMissingFormat = $"{Prefix}.ContentBlock.Image.MissingFormat";

    /// <summary>
    /// Error code for unsupported image bytes (must use S3).
    /// </summary>
    public static readonly string ContentBlockImageBytesNotSupported = $"{Prefix}.ContentBlock.Image.BytesNotSupported";

    /// <summary>
    /// Error code for missing image source.
    /// </summary>
    public static readonly string ContentBlockImageMissingSource = $"{Prefix}.ContentBlock.Image.MissingSource";

    /// <summary>
    /// Error code for missing document format.
    /// </summary>
    public static readonly string ContentBlockDocumentMissingFormat = $"{Prefix}.ContentBlock.Document.MissingFormat";

    /// <summary>
    /// Error code for missing document name.
    /// </summary>
    public static readonly string ContentBlockDocumentMissingName = $"{Prefix}.ContentBlock.Document.MissingName";

    /// <summary>
    /// Error code for unsupported document bytes (must use S3).
    /// </summary>
    public static readonly string ContentBlockDocumentBytesNotSupported = $"{Prefix}.ContentBlock.Document.BytesNotSupported";

    /// <summary>
    /// Error code for missing document source.
    /// </summary>
    public static readonly string ContentBlockDocumentMissingSource = $"{Prefix}.ContentBlock.Document.MissingSource";

    /// <summary>
    /// Error code for missing tool use ID.
    /// </summary>
    public static readonly string ContentBlockToolUseMissingId = $"{Prefix}.ContentBlock.ToolUse.MissingId";

    /// <summary>
    /// Error code for missing tool use name.
    /// </summary>
    public static readonly string ContentBlockToolUseMissingName = $"{Prefix}.ContentBlock.ToolUse.MissingName";

    /// <summary>
    /// Error code for missing tool use input.
    /// </summary>
    public static readonly string ContentBlockToolUseMissingInput = $"{Prefix}.ContentBlock.ToolUse.MissingInput";

    /// <summary>
    /// Error code for invalid tool use input JSON.
    /// </summary>
    public static readonly string ContentBlockToolUseInvalidInput = $"{Prefix}.ContentBlock.ToolUse.InvalidInput";

    /// <summary>
    /// Error code for missing tool result ID.
    /// </summary>
    public static readonly string ContentBlockToolResultMissingId = $"{Prefix}.ContentBlock.ToolResult.MissingId";

    /// <summary>
    /// Error code for missing tool result content.
    /// </summary>
    public static readonly string ContentBlockToolResultMissingContent = $"{Prefix}.ContentBlock.ToolResult.MissingContent";

    /// <summary>
    /// Creates a not found error for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation identifier that was not found.</param>
    /// <returns>An Error representing the not found condition.</returns>
    public static Error ConversationNotFound(string conversationId) =>
        Error.NotFound(NotFound, $"Conversation with id '{conversationId}' was not found.");

    /// <summary>
    /// Creates a not found error for a message.
    /// </summary>
    /// <param name="messageId">The message identifier that was not found.</param>
    /// <returns>An Error representing the not found condition.</returns>
    public static Error MessageNotFoundError(string messageId) =>
        Error.NotFound(MessageNotFound, $"Message with id '{messageId}' was not found.");

    /// <summary>
    /// Creates an error for invalid conversation data.
    /// </summary>
    /// <param name="description">Description of the validation failure.</param>
    /// <returns>An Error representing the validation failure.</returns>
    public static Error InvalidConversationError(string description) =>
        Error.Validation(InvalidConversation, description);

    /// <summary>
    /// Creates an error for invalid message data.
    /// </summary>
    /// <param name="description">Description of the validation failure.</param>
    /// <returns>An Error representing the validation failure.</returns>
    public static Error InvalidMessageError(string description) =>
        Error.Validation(InvalidMessage, description);

    /// <summary>
    /// Creates an error for concurrency conflicts.
    /// </summary>
    /// <param name="conversationId">The conversation identifier that had a conflict.</param>
    /// <returns>An Error representing the concurrency conflict.</returns>
    public static Error ConcurrencyConflictError(string conversationId) =>
        Error.Conflict(ConcurrencyConflict, $"Conversation with id '{conversationId}' was modified by another process.");

    /// <summary>
    /// Creates an error for storage operation failures.
    /// </summary>
    /// <param name="description">Description of the storage error.</param>
    /// <returns>An Error representing the storage failure.</returns>
    public static Error StorageOperationError(string description) =>
        Error.Failure(StorageError, description);
}
