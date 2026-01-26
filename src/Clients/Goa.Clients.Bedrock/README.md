# Goa.Clients.Bedrock

A high-performance Amazon Bedrock client optimized for AWS Lambda functions. This package provides a lightweight, AOT-ready Bedrock client with strongly-typed operations and comprehensive error handling using the ErrorOr pattern.

## Installation

```bash
dotnet add package Goa.Clients.Bedrock
```

## Features

- Native AOT support for faster Lambda cold starts
- Converse API with fluent builder pattern
- InvokeModel API for raw model access
- ApplyGuardrail API for content moderation
- CountTokens API for token estimation
- Tool/function calling support
- MCP (Model Context Protocol) tool adapter
- Strongly-typed request/response objects
- Built-in error handling with ErrorOr pattern

## Basic Setup

```csharp
using Goa.Clients.Bedrock;
using Microsoft.Extensions.DependencyInjection;

// Register Bedrock client with defaults
services.AddBedrock();

// Or with custom configuration
services.AddBedrock(config =>
{
    config.ServiceUrl = "https://bedrock-runtime.us-east-1.amazonaws.com";
    config.Region = "us-east-1";
    config.LogLevel = LogLevel.Debug;
});

// Or with simple parameters
services.AddBedrock(
    serviceUrl: null,
    region: "us-west-2",
    logLevel: LogLevel.Information
);
```

## Usage

### Simple Conversation

```csharp
using Goa.Clients.Bedrock;
using Goa.Clients.Bedrock.Operations.Converse;

public class ChatService
{
    private readonly IBedrockClient _client;

    public ChatService(IBedrockClient client)
    {
        _client = client;
    }

    public async Task<string?> ChatAsync(string userMessage)
    {
        var request = new ConverseBuilder("anthropic.claude-3-sonnet-20240229-v1:0")
            .WithSystemPrompt("You are a helpful assistant.")
            .AddUserMessage(userMessage)
            .WithMaxTokens(1024)
            .Build();

        var result = await _client.ConverseAsync(request);

        if (result.IsError)
            return null;

        return result.Value.Output?.Message?.Content?.FirstOrDefault()?.Text;
    }
}
```

### Builder Pattern with ConverseBuilder

The `ConverseBuilder` provides a fluent API for constructing conversation requests:

```csharp
var request = new ConverseBuilder("anthropic.claude-3-sonnet-20240229-v1:0")
    .WithSystemPrompt("You are an expert programmer.")
    .AddUserMessage("What is dependency injection?")
    .AddAssistantMessage("Dependency injection is a design pattern...")
    .AddUserMessage("Can you show me an example?")
    .WithMaxTokens(2048)
    .WithTemperature(0.7f)
    .WithTopP(0.9f)
    .WithStopSequences("END", "STOP")
    .WithGuardrail("my-guardrail-id", "1")
    .WithPerformance(LatencyMode.Standard)
    .WithServiceTier(ServiceTier.Auto)
    .Build();

var result = await _client.ConverseAsync(request);
```

### Tool/Function Calling

Define and use tools that the model can call:

```csharp
using System.Text.Json;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Models;

// Define the tool schema
var weatherSchema = JsonDocument.Parse("""
{
    "type": "object",
    "properties": {
        "location": {
            "type": "string",
            "description": "The city and state, e.g. San Francisco, CA"
        }
    },
    "required": ["location"]
}
""").RootElement;

// Build request with tool
var request = new ConverseBuilder("anthropic.claude-3-sonnet-20240229-v1:0")
    .AddUserMessage("What's the weather in Seattle?")
    .WithTool("get_weather", "Get the current weather for a location", weatherSchema)
    .WithToolChoice(new ToolChoice { Auto = new AutoToolChoice() })
    .WithMaxTokens(1024)
    .Build();

var result = await _client.ConverseAsync(request);

// Check if model wants to use a tool
if (result.Value.StopReason == StopReason.ToolUse)
{
    var toolUse = result.Value.Output?.Message?.Content?
        .FirstOrDefault(c => c.ToolUse != null)?.ToolUse;

    if (toolUse != null)
    {
        // Execute the tool and continue conversation
        var toolResult = ExecuteWeatherTool(toolUse.Input);

        // Add tool result and continue
        var followUp = new ConverseBuilder("anthropic.claude-3-sonnet-20240229-v1:0")
            .AddMessage(ConversationRole.User, result.Value.Output.Message.Content)
            .AddMessage(ConversationRole.User, new List<ContentBlock>
            {
                new() { ToolResult = new ToolResultBlock
                {
                    ToolUseId = toolUse.ToolUseId,
                    Status = "success",
                    Content = new List<ContentBlock> { new() { Text = toolResult } }
                }}
            })
            .Build();
    }
}
```

### MCP Integration with McpToolAdapter

The `McpToolAdapter` bridges MCP tool definitions to Bedrock format:

```csharp
using System.Text.Json;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Operations.Converse;

// Create the adapter
var adapter = new McpToolAdapter();

// Define MCP tools
var mcpTools = new List<McpToolDefinition>
{
    new()
    {
        Name = "calculator",
        Description = "Performs basic arithmetic operations",
        InputSchema = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "operation": { "type": "string", "enum": ["add", "subtract", "multiply", "divide"] },
                "a": { "type": "number" },
                "b": { "type": "number" }
            },
            "required": ["operation", "a", "b"]
        }
        """).RootElement,
        Handler = async (input, ct) =>
        {
            var op = input.GetProperty("operation").GetString();
            var a = input.GetProperty("a").GetDouble();
            var b = input.GetProperty("b").GetDouble();

            var result = op switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" => a / b,
                _ => throw new ArgumentException($"Unknown operation: {op}")
            };

            return JsonDocument.Parse($"{{\"result\": {result}}}").RootElement;
        }
    }
};

// Convert to Bedrock tools
var bedrockTools = adapter.ToBedrockTools(mcpTools);

// Use in conversation
var request = new ConverseBuilder("anthropic.claude-3-sonnet-20240229-v1:0")
    .AddUserMessage("What is 42 multiplied by 17?")
    .WithMaxTokens(1024)
    .Build();

// Add tools to request
request.ToolConfig = new ToolConfiguration { Tools = bedrockTools.ToList() };

var result = await _client.ConverseAsync(request);

// Execute tool if requested
if (result.Value.StopReason == StopReason.ToolUse)
{
    var toolUse = result.Value.Output?.Message?.Content?
        .FirstOrDefault(c => c.ToolUse != null)?.ToolUse;

    if (toolUse != null)
    {
        var toolResult = await adapter.ExecuteToolAsync(toolUse);
        // Continue conversation with tool result...
    }
}
```

### Raw Model Invocation

For direct model access with custom payloads:

```csharp
using Goa.Clients.Bedrock.Operations.InvokeModel;

var request = new InvokeModelRequest
{
    ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
    Body = """
    {
        "anthropic_version": "bedrock-2023-05-31",
        "max_tokens": 1024,
        "messages": [
            {"role": "user", "content": "Hello!"}
        ]
    }
    """,
    ContentType = "application/json",
    Accept = "application/json"
};

var result = await _client.InvokeModelAsync(request);

if (!result.IsError)
{
    var responseBody = result.Value.Body;
    // Parse model-specific response format
}
```

### Guardrails

Apply content moderation with Bedrock Guardrails:

```csharp
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;

var request = new ApplyGuardrailRequest
{
    GuardrailIdentifier = "my-guardrail-id",
    GuardrailVersion = "1",
    Source = "INPUT",
    Content = new List<GuardrailContentBlock>
    {
        new()
        {
            Text = new GuardrailTextBlock
            {
                Text = "User provided content to check..."
            }
        }
    }
};

var result = await _client.ApplyGuardrailAsync(request);

if (!result.IsError)
{
    var action = result.Value.Action; // NONE or GUARDRAIL_INTERVENED
}
```

### Token Counting

Estimate token usage before making requests:

```csharp
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Enums;

var request = new CountTokensRequest
{
    ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
    Messages = new List<Message>
    {
        new()
        {
            Role = ConversationRole.User,
            Content = new List<ContentBlock>
            {
                new() { Text = "What is the meaning of life?" }
            }
        }
    },
    System = new List<SystemContentBlock>
    {
        new() { Text = "You are a philosophical assistant." }
    }
};

var result = await _client.CountTokensAsync(request);

if (!result.IsError)
{
    var inputTokens = result.Value.InputTokens;
}
```

## Available Operations

| Operation | Method | Description |
|-----------|--------|-------------|
| Converse | `ConverseAsync` | Send conversation requests using the Converse API |
| InvokeModel | `InvokeModelAsync` | Invoke a model with raw JSON payload |
| ApplyGuardrail | `ApplyGuardrailAsync` | Apply content moderation guardrails |
| CountTokens | `CountTokensAsync` | Count tokens in a request |

## Error Handling

All operations return `ErrorOr<T>` results, providing comprehensive error handling:

```csharp
var result = await _client.ConverseAsync(request);

if (result.IsError)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Code} - {error.Description}");
    }
    return;
}

// Use successful result
var response = result.Value;
var text = response.Output?.Message?.Content?.FirstOrDefault()?.Text;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
