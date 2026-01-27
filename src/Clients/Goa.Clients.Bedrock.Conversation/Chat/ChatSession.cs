using System.Text.Json;
using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Conversation.Internal;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;

namespace Goa.Clients.Bedrock.Conversation.Chat;

/// <summary>
/// Internal implementation of an active chat session with a Bedrock model.
/// </summary>
internal sealed class ChatSession : IChatSession
{
    private readonly IBedrockClient _client;
    private readonly IMcpToolAdapter _toolAdapter;
    private readonly IConversationStore? _store;
    private readonly ChatSessionOptions _options;
    private readonly List<Message> _messages = [];
    private readonly List<McpToolDefinition> _tools = [];
    private TokenUsage _totalTokenUsage = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="client">The Bedrock client.</param>
    /// <param name="toolAdapter">The MCP tool adapter.</param>
    /// <param name="store">The optional conversation store.</param>
    /// <param name="options">The session options.</param>
    /// <param name="conversationId">The optional conversation ID for persisted sessions.</param>
    internal ChatSession(
        IBedrockClient client,
        IMcpToolAdapter toolAdapter,
        IConversationStore? store,
        ChatSessionOptions options,
        string? conversationId)
    {
        _client = client;
        _toolAdapter = toolAdapter;
        _store = store;
        _options = options;
        ConversationId = conversationId;
    }

    /// <inheritdoc />
    public string? ConversationId { get; }

    /// <inheritdoc />
    public TokenUsage TotalTokenUsage => _totalTokenUsage;

    /// <inheritdoc />
    public IChatSession RegisterTool(McpToolDefinition tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        _tools.Add(tool);
        return this;
    }

    /// <inheritdoc />
    public IChatSession RegisterTools(IEnumerable<McpToolDefinition> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        _tools.AddRange(tools);
        return this;
    }

    /// <inheritdoc />
    public Task<ErrorOr<ChatResponse>> SendAsync(string message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return SendAsync([new ContentBlock { Text = message }], ct);
    }

    /// <inheritdoc />
    public async Task<ErrorOr<ChatResponse>> SendAsync(IEnumerable<ContentBlock> content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Create user message
        var userMessage = new Message
        {
            Role = ConversationRole.User,
            Content = content.ToList()
        };

        // Clean message content (extract XML tags)
        var (cleanedUserMessage, userExtractedTags) = CleanMessageContent(userMessage);

        // Add cleaned message to in-memory history
        _messages.Add(cleanedUserMessage);

        // Persist if configured
        if (_options.PersistConversation && _store != null && ConversationId != null)
        {
            var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.User, cleanedUserMessage, null, ct, userExtractedTags).ConfigureAwait(false);
            if (addResult.IsError)
            {
                return addResult.Errors;
            }
        }

        // Track tool executions for response
        var toolExecutions = new List<ToolExecution>();
        var iterationCount = 0;

        // Convert registered tools to Bedrock format
        var bedrockTools = _tools.Count > 0 ? _toolAdapter.ToBedrockTools(_tools) : null;

        while (true)
        {
            // Build the converse request
            var request = BuildConverseRequest(bedrockTools);

            // Call Bedrock
            var result = await _client.ConverseAsync(request, ct).ConfigureAwait(false);
            if (result.IsError)
            {
                return result.Errors;
            }

            var response = result.Value;

            // Accumulate token usage
            AccumulateTokenUsage(response.Usage);

            // Handle tool use
            if (response.StopReason == StopReason.ToolUse)
            {
                iterationCount++;
                if (iterationCount > _options.MaxToolIterations)
                {
                    return ChatErrorCodes.MaxToolIterationsExceededError(_options.MaxToolIterations);
                }

                var assistantMessage = response.Output?.Message;
                if (assistantMessage == null)
                {
                    return Error.Failure("Goa.Bedrock.Chat.NoAssistantMessage", "No assistant message in tool use response.");
                }

                // Clean assistant message content (extract XML tags)
                var (cleanedAssistantMessage, assistantExtractedTags) = CleanMessageContent(assistantMessage);

                // Add cleaned message to history
                _messages.Add(cleanedAssistantMessage);

                // Persist assistant message if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.Assistant, cleanedAssistantMessage, response.Usage, ct, assistantExtractedTags).ConfigureAwait(false);
                    if (addResult.IsError)
                    {
                        return addResult.Errors;
                    }
                }

                // Execute tools
                var toolUseBlocks = assistantMessage.Content
                    .Where(c => c.ToolUse != null)
                    .Select(c => c.ToolUse!)
                    .ToList();

                var toolResults = new List<ContentBlock>();
                foreach (var toolUse in toolUseBlocks)
                {
                    var toolResult = await _toolAdapter.ExecuteToolAsync(toolUse, ct).ConfigureAwait(false);
                    toolResults.Add(new ContentBlock { ToolResult = toolResult });

                    // Track tool execution
                    toolExecutions.Add(new ToolExecution
                    {
                        ToolName = toolUse.Name,
                        ToolUseId = toolUse.ToolUseId,
                        Input = JsonDocument.Parse(toolUse.Input.GetRawText()),
                        Result = toolResult.Content.FirstOrDefault()?.Text ?? string.Empty,
                        Success = toolResult.Status == "success"
                    });
                }

                // Add tool results as user message
                var toolResultMessage = new Message
                {
                    Role = ConversationRole.User,
                    Content = toolResults
                };

                // Clean tool result message (extract XML tags)
                var (cleanedToolResultMessage, toolResultExtractedTags) = CleanMessageContent(toolResultMessage);
                _messages.Add(cleanedToolResultMessage);

                // Persist tool results if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.User, cleanedToolResultMessage, null, ct, toolResultExtractedTags).ConfigureAwait(false);
                    if (addResult.IsError)
                    {
                        return addResult.Errors;
                    }
                }

                // Continue the loop
                continue;
            }

            // Final response
            var finalMessage = response.Output?.Message;
            if (finalMessage != null)
            {
                // Clean message content (extract XML tags)
                var (cleanedFinalMessage, finalExtractedTags) = CleanMessageContent(finalMessage);

                // Add cleaned message to history
                _messages.Add(cleanedFinalMessage);

                // Extract cleaned text content
                var cleanedText = cleanedFinalMessage.Content
                    .Where(c => c.Text != null)
                    .Select(c => c.Text!)
                    .FirstOrDefault() ?? string.Empty;

                // Persist if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.Assistant, cleanedFinalMessage, response.Usage, ct, finalExtractedTags).ConfigureAwait(false);
                    if (addResult.IsError)
                    {
                        return addResult.Errors;
                    }
                }

                return new ChatResponse
                {
                    Text = cleanedText,
                    Content = cleanedFinalMessage.Content,
                    StopReason = response.StopReason,
                    Usage = response.Usage ?? new TokenUsage(),
                    ToolsExecuted = toolExecutions,
                    ExtractedTags = finalExtractedTags
                };
            }

            // No message in response
            return new ChatResponse
            {
                Text = string.Empty,
                Content = [],
                StopReason = response.StopReason,
                Usage = response.Usage ?? new TokenUsage(),
                ToolsExecuted = toolExecutions
            };
        }
    }

    /// <inheritdoc />
    public Task<ErrorOr<IReadOnlyList<ChatMessage>>> GetHistoryAsync(CancellationToken ct = default)
    {
        var history = _messages.Select((m, index) => new ChatMessage
        {
            Role = m.Role,
            Content = m.Content,
            Timestamp = DateTimeOffset.UtcNow, // In-memory messages don't have timestamps
            TokenUsage = null
        }).ToList();

        return Task.FromResult<ErrorOr<IReadOnlyList<ChatMessage>>>(history);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Loads messages from persisted storage into the in-memory history.
    /// </summary>
    /// <param name="messages">The messages to load.</param>
    internal void LoadMessages(IEnumerable<ConversationMessage> messages)
    {
        foreach (var msg in messages)
        {
            _messages.Add(msg.Message);
        }
    }

    /// <summary>
    /// Sets the initial token usage from a persisted conversation.
    /// </summary>
    /// <param name="tokenUsage">The token usage to set.</param>
    internal void SetInitialTokenUsage(TokenUsage? tokenUsage)
    {
        if (tokenUsage != null)
        {
            _totalTokenUsage = new TokenUsage
            {
                InputTokens = tokenUsage.InputTokens,
                OutputTokens = tokenUsage.OutputTokens,
                TotalTokens = tokenUsage.TotalTokens
            };
        }
    }

    private static (Message CleanedMessage, IReadOnlyDictionary<string, IReadOnlyList<string>> ExtractedTags) CleanMessageContent(Message message)
    {
        var cleanedContent = new List<ContentBlock>();
        var allTags = new Dictionary<string, List<string>>();

        foreach (var block in message.Content)
        {
            if (block.Text != null)
            {
                var (cleanedText, tags) = XmlTagParser.Parse(block.Text);

                // Merge extracted tags
                foreach (var (key, values) in tags)
                {
                    if (!allTags.TryGetValue(key, out var list))
                    {
                        list = [];
                        allTags[key] = list;
                    }
                    list.AddRange(values);
                }

                if (!string.IsNullOrWhiteSpace(cleanedText))
                {
                    cleanedContent.Add(new ContentBlock { Text = cleanedText });
                }
            }
            else
            {
                // Preserve non-text blocks (ToolUse, ToolResult, etc.)
                cleanedContent.Add(block);
            }
        }

        var result = allTags.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value);

        return (new Message { Role = message.Role, Content = cleanedContent }, result);
    }

    private ConverseRequest BuildConverseRequest(IReadOnlyList<Tool>? bedrockTools)
    {
        var builder = new ConverseBuilder(_options.ModelId)
            .WithMaxTokens(_options.MaxTokens)
            .WithTemperature(_options.Temperature);

        if (!string.IsNullOrEmpty(_options.SystemPrompt))
        {
            builder.WithSystemPrompt(_options.SystemPrompt);
        }

        if (_options.TopP.HasValue)
        {
            builder.WithTopP(_options.TopP.Value);
        }

        if (_options.StopSequences is { Count: > 0 })
        {
            builder.WithStopSequences(_options.StopSequences.ToArray());
        }

        // Add all messages from history
        foreach (var msg in _messages)
        {
            builder.AddMessage(msg.Role, msg.Content);
        }

        var request = builder.Build();

        // Add tools if any are registered
        if (bedrockTools is { Count: > 0 })
        {
            request.ToolConfig = new ToolConfiguration { Tools = bedrockTools.ToList() };
        }

        return request;
    }

    private void AccumulateTokenUsage(TokenUsage? usage)
    {
        if (usage == null)
        {
            return;
        }

        _totalTokenUsage = new TokenUsage
        {
            InputTokens = _totalTokenUsage.InputTokens + usage.InputTokens,
            OutputTokens = _totalTokenUsage.OutputTokens + usage.OutputTokens,
            TotalTokens = _totalTokenUsage.TotalTokens + usage.TotalTokens
        };
    }
}
