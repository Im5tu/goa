# Goa.Clients.Bedrock.Conversation

A session-based conversation client for Amazon Bedrock with automatic tool execution and persistence support. This package provides a high-level API for managing multi-turn conversations with Bedrock models, including token tracking and conversation history.

## Installation

```bash
dotnet add package Goa.Clients.Bedrock.Conversation
```

## Features

- Session-based chat with Bedrock models
- Automatic tool execution with configurable iteration limits
- Conversation history retrieval
- Optional persistence support via `IConversationStore`
- Token usage tracking across sessions
- Fluent API with method chaining
- Built-in error handling with ErrorOr pattern

## Basic Setup

```csharp
using Goa.Clients.Bedrock.Conversation;
using Microsoft.Extensions.DependencyInjection;

// Register Bedrock chat session services
services.AddBedrockChatSession();
```

The `AddBedrockChatSession()` method registers `IChatSessionFactory` as a singleton. It depends on:
- `IBedrockClient` - the underlying Bedrock client (registered automatically)
- `IMcpToolAdapter` - adapter for MCP tool execution (must be registered separately)
- `IConversationStore` (optional) - for conversation persistence

## Usage

### Creating Sessions

```csharp
using Goa.Clients.Bedrock.Conversation.Chat;

public class ChatService
{
    private readonly IChatSessionFactory _sessionFactory;

    public ChatService(IChatSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task StartChatAsync()
    {
        var options = new ChatSessionOptions
        {
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            SystemPrompt = "You are a helpful assistant.",
            MaxTokens = 1024,
            Temperature = 0.7f
        };

        var sessionResult = await _sessionFactory.CreateAsync(options);
        if (sessionResult.IsError)
        {
            // Handle error
            return;
        }

        await using var session = sessionResult.Value;
        // Use session...
    }
}
```

### Simple Conversation

```csharp
await using var session = (await _sessionFactory.CreateAsync(options)).Value;

// Send a text message
var response = await session.SendAsync("What is the capital of France?");

if (response.IsError)
{
    Console.WriteLine($"Error: {response.FirstError.Description}");
    return;
}

Console.WriteLine(response.Value.Text);
// Output: The capital of France is Paris.
```

### Using Tools

```csharp
using System.Text.Json;
using Goa.Clients.Bedrock.Mcp;

// Define a tool
var weatherTool = new McpToolDefinition
{
    Name = "get_weather",
    Description = "Gets the current weather for a location",
    InputSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "location": {
                    "type": "string",
                    "description": "The city name"
                }
            },
            "required": ["location"]
        }
        """).RootElement,
    Handler = async (input, ct) =>
    {
        var location = input.GetProperty("location").GetString();
        var result = new { temperature = 72, condition = "sunny", location };
        return JsonSerializer.SerializeToElement(result);
    }
};

// Register tools with the session
session
    .RegisterTool(weatherTool)
    .RegisterTools(additionalTools);

// The model will automatically use tools when needed
var response = await session.SendAsync("What's the weather like in Seattle?");

// Check which tools were executed
foreach (var tool in response.Value.ToolsExecuted)
{
    Console.WriteLine($"Tool: {tool.ToolName}, Success: {tool.Success}");
    Console.WriteLine($"Result: {tool.Result}");
}
```

### Persisted Conversations

```csharp
using Goa.Clients.Bedrock.Conversation.Entities;

// Create a persisted session
var options = new ChatSessionOptions
{
    ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
    PersistConversation = true,
    Metadata = new ConversationMetadata
    {
        Title = "Customer Support Chat",
        Tags = ["support", "billing"],
        CustomData = new Dictionary<string, string>
        {
            ["userId"] = "user-123"
        }
    }
};

await using var session = (await _sessionFactory.CreateAsync(options)).Value;

// The conversation ID is available for persisted sessions
Console.WriteLine($"Conversation ID: {session.ConversationId}");

// Send messages - they are automatically persisted
await session.SendAsync("Hello, I need help with my account.");

// Later, resume the conversation
var resumedSession = await _sessionFactory.ResumeAsync(
    conversationId: session.ConversationId!,
    options: new ChatSessionOptions
    {
        ModelId = "anthropic.claude-3-sonnet-20240229-v1:0"
    });

if (resumedSession.IsError)
{
    Console.WriteLine($"Failed to resume: {resumedSession.FirstError.Description}");
    return;
}

await using var continued = resumedSession.Value;
await continued.SendAsync("As I was saying earlier...");
```

### History Retrieval

```csharp
// Get the conversation history
var historyResult = await session.GetHistoryAsync();

if (historyResult.IsError)
{
    Console.WriteLine($"Error: {historyResult.FirstError.Description}");
    return;
}

foreach (var message in historyResult.Value)
{
    Console.WriteLine($"[{message.Role}] at {message.Timestamp}");
    foreach (var content in message.Content)
    {
        // Process content blocks
    }
    if (message.TokenUsage != null)
    {
        Console.WriteLine($"  Tokens: {message.TokenUsage.TotalTokens}");
    }
}
```

### Session Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `ModelId` | `string` | Required | The Bedrock model identifier |
| `SystemPrompt` | `string?` | `null` | Optional system prompt to guide model behavior |
| `MaxTokens` | `int` | `1024` | Maximum tokens to generate per response |
| `Temperature` | `float` | `0.7` | Controls randomness (0.0 = deterministic, 1.0 = creative) |
| `TopP` | `float?` | `null` | Nucleus sampling parameter |
| `StopSequences` | `IReadOnlyList<string>?` | `null` | Sequences that stop generation |
| `PersistConversation` | `bool` | `false` | Whether to persist the conversation |
| `Metadata` | `ConversationMetadata?` | `null` | Metadata for persisted conversations |
| `MaxToolIterations` | `int` | `10` | Maximum tool execution iterations per message |

### Token Usage Tracking

```csharp
await using var session = (await _sessionFactory.CreateAsync(options)).Value;

await session.SendAsync("Hello!");
await session.SendAsync("Tell me a joke.");

// Get total token usage for the session
var usage = session.TotalTokenUsage;
Console.WriteLine($"Input tokens: {usage.InputTokens}");
Console.WriteLine($"Output tokens: {usage.OutputTokens}");
Console.WriteLine($"Total tokens: {usage.TotalTokens}");

// Per-response usage is also available
var response = await session.SendAsync("Another message");
Console.WriteLine($"This response used {response.Value.Usage.TotalTokens} tokens");
```

## ChatResponse Properties

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | The text content of the response |
| `Content` | `IReadOnlyList<ContentBlock>` | All content blocks in the response |
| `StopReason` | `StopReason` | Why the model stopped generating |
| `Usage` | `TokenUsage` | Token usage for this response |
| `ToolsExecuted` | `IReadOnlyList<ToolExecution>` | Tools that were executed |

### StopReason Values

- `EndTurn` - Natural completion
- `ToolUse` - Model invoked a tool
- `MaxTokens` - Reached token limit
- `StopSequence` - Hit a stop sequence
- `GuardrailIntervened` - Guardrail blocked generation
- `ContentFiltered` - Content was filtered
- `ModelContextWindowExceeded` - Exceeded context window

## Error Handling

All operations return `ErrorOr<T>` results for comprehensive error handling:

```csharp
var sessionResult = await _sessionFactory.CreateAsync(options);

if (sessionResult.IsError)
{
    foreach (var error in sessionResult.Errors)
    {
        Console.WriteLine($"Error: {error.Code} - {error.Description}");
    }
    return;
}

await using var session = sessionResult.Value;

var response = await session.SendAsync("Hello");

if (response.IsError)
{
    // Handle specific error types
    var error = response.FirstError;
    Console.WriteLine($"Failed to send message: {error.Description}");
    return;
}

// Use successful response
Console.WriteLine(response.Value.Text);
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
