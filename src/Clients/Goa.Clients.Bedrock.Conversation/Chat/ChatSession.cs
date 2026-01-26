using System.Text.Json;
using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Entities;
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

        // Add to in-memory history
        _messages.Add(userMessage);

        // Persist if configured
        if (_options.PersistConversation && _store != null && ConversationId != null)
        {
            var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.User, userMessage, null, ct).ConfigureAwait(false);
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

                // Add assistant message to history
                _messages.Add(assistantMessage);

                // Persist assistant message if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.Assistant, assistantMessage, response.Usage, ct).ConfigureAwait(false);
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
                _messages.Add(toolResultMessage);

                // Persist tool results if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.User, toolResultMessage, null, ct).ConfigureAwait(false);
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
                // Add to history
                _messages.Add(finalMessage);

                // Persist if configured
                if (_options.PersistConversation && _store != null && ConversationId != null)
                {
                    var addResult = await _store.AddMessageAsync(ConversationId, ConversationRole.Assistant, finalMessage, response.Usage, ct).ConfigureAwait(false);
                    if (addResult.IsError)
                    {
                        return addResult.Errors;
                    }
                }

                // Extract text content
                var textContent = finalMessage.Content
                    .Where(c => c.Text != null)
                    .Select(c => c.Text!)
                    .FirstOrDefault() ?? string.Empty;

                return new ChatResponse
                {
                    Text = textContent,
                    Content = finalMessage.Content,
                    StopReason = response.StopReason,
                    Usage = response.Usage ?? new TokenUsage(),
                    ToolsExecuted = toolExecutions
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
