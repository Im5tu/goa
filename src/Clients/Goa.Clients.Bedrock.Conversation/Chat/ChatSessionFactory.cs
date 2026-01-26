using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Mcp;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Factory for creating and resuming chat sessions.
/// </summary>
public sealed class ChatSessionFactory : IChatSessionFactory
{
    private readonly IBedrockClient _client;
    private readonly IMcpToolAdapter _toolAdapter;
    private readonly IConversationStore? _store;

    /// <summary>
    /// Creates a new instance of the chat session factory.
    /// </summary>
    /// <param name="client">The Bedrock client.</param>
    /// <param name="toolAdapter">The MCP tool adapter.</param>
    /// <param name="store">The optional conversation store for persistence.</param>
    public ChatSessionFactory(IBedrockClient client, IMcpToolAdapter toolAdapter, IConversationStore? store = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _toolAdapter = toolAdapter ?? throw new ArgumentNullException(nameof(toolAdapter));
        _store = store;
    }

    /// <inheritdoc />
    public async Task<ErrorOr<IChatSession>> CreateAsync(ChatSessionOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        string? conversationId = null;

        // If persistence is requested, create a conversation in the store
        if (options.PersistConversation)
        {
            if (_store == null)
            {
                return ChatErrorCodes.PersistenceNotConfiguredError();
            }

            var metadata = options.Metadata ?? new ConversationMetadata();
            metadata.ModelId ??= options.ModelId;

            var createResult = await _store.CreateConversationAsync(metadata, ct).ConfigureAwait(false);
            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            conversationId = createResult.Value.Id;
        }

        return new ChatSession(_client, _toolAdapter, _store, options, conversationId);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<IChatSession>> ResumeAsync(string conversationId, ChatSessionOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conversationId);

        // Persistence is required for resuming
        if (_store == null)
        {
            return ChatErrorCodes.PersistenceNotConfiguredError();
        }

        // Load the conversation with messages
        var conversationResult = await _store.GetConversationWithMessagesAsync(conversationId, null, null, ct).ConfigureAwait(false);
        if (conversationResult.IsError)
        {
            return conversationResult.Errors;
        }

        var conversationData = conversationResult.Value;

        // Create options from the conversation if not provided
        var sessionOptions = options ?? new ChatSessionOptions
        {
            ModelId = conversationData.Conversation.Metadata?.ModelId ?? throw new InvalidOperationException("Conversation does not have a model ID configured."),
            PersistConversation = true
        };

        // Create the session
        var session = new ChatSession(_client, _toolAdapter, _store, sessionOptions, conversationId);

        // Load existing messages into the session
        session.LoadMessages(conversationData.Messages);

        // Set initial token usage
        session.SetInitialTokenUsage(conversationData.Conversation.TotalTokenUsage);

        return session;
    }
}
