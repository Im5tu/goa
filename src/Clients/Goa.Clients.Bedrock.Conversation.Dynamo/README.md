# Goa.Clients.Bedrock.Conversation.Dynamo

A DynamoDB-backed persistence layer for Bedrock conversations. This package provides durable storage for chat sessions with support for TTL-based expiration, pagination, and single-table design patterns.

## Installation

```bash
dotnet add package Goa.Clients.Bedrock.Conversation.Dynamo
```

## Features

- **DynamoDB persistence** - Store conversations and messages in DynamoDB with atomic transactions
- **TTL support** - Automatic expiration of conversations with configurable time-to-live
- **Pagination** - Efficient retrieval of large conversations with token-based pagination
- **Single-table design** - Conversations and messages share a table using composite keys
- **Transactional writes** - Messages are added atomically with conversation updates
- **Token usage tracking** - Aggregate token counts across all messages in a conversation
- **Native AOT support** - Optimized for AWS Lambda with fast cold starts

## Basic Setup

### Simple Registration

```csharp
using Goa.Clients.Bedrock.Conversation.Dynamo;
using Microsoft.Extensions.DependencyInjection;

// Register with default table name "conversations"
services.AddBedrockDynamoConversationStore();

// Or specify a custom table name
services.AddBedrockDynamoConversationStore("my-conversations-table");
```

### Full Configuration

```csharp
services.AddBedrockDynamoConversationStore(config =>
{
    config.TableName = "my-conversations";
    config.PartitionKeyName = "PK";
    config.SortKeyName = "SK";
    config.TtlAttributeName = "TTL";
    config.DefaultTtl = TimeSpan.FromDays(30);
    config.DefaultMessageLimit = 100;
    config.ConversationPkFormat = id => $"CONV#{id}";
    config.ConversationSkValue = "METADATA";
    config.MessageSkPrefix = "MSG#";
});
```

## DynamoDB Table Design

This library uses a single-table design with composite primary keys. All items for a conversation share the same partition key.

### Key Structure

| Item Type | Partition Key (PK) | Sort Key (SK) |
|-----------|-------------------|---------------|
| Conversation | `Conversation#{id}` | `_` |
| Message | `Conversation#{id}` | `message#0000000001` |

### Conversation Record Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | String | Conversation identifier |
| `CreatedAt` | Number | Unix timestamp (seconds) |
| `UpdatedAt` | Number | Unix timestamp (seconds) |
| `MessageCount` | Number | Total messages in conversation |
| `Title` | String | Optional conversation title |
| `ModelId` | String | Optional Bedrock model identifier |
| `Tags` | String Set | Optional tags for categorization |
| `CustomData` | Map | Optional key-value metadata |
| `TotalInputTokens` | Number | Aggregate input tokens |
| `TotalOutputTokens` | Number | Aggregate output tokens |
| `TotalTokens` | Number | Aggregate total tokens |
| `TTL` | Number | Expiration timestamp (if configured) |

### Message Record Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `Id` | String | Message identifier |
| `ConversationId` | String | Parent conversation ID |
| `SequenceNumber` | Number | Order within conversation |
| `Role` | String | `User` or `Assistant` |
| `Content` | List | Serialized content blocks |
| `CreatedAt` | Number | Unix timestamp (seconds) |
| `InputTokens` | Number | Token usage (if provided) |
| `OutputTokens` | Number | Token usage (if provided) |
| `Tokens` | Number | Total tokens (if provided) |
| `TTL` | Number | Expiration timestamp (if configured) |

### Table Requirements

Create a DynamoDB table with:

```
Partition Key: PK (String)
Sort Key: SK (String)
TTL Attribute: TTL (enabled if using expiration)
```

## Usage Examples

### With Chat Sessions

The recommended approach is to use the `IChatSession` abstraction, which handles message persistence automatically.

```csharp
using Goa.Clients.Bedrock.Conversation;
using Goa.Clients.Bedrock.Models;

public class ChatService
{
    private readonly IChatSessionFactory _sessionFactory;

    public ChatService(IChatSessionFactory sessionFactory)
    {
        _sessionFactory = sessionFactory;
    }

    public async Task<string> StartNewConversationAsync(string userMessage)
    {
        // Create a new conversation with metadata
        var session = await _sessionFactory.CreateAsync(new ConversationMetadata
        {
            Title = "New Chat",
            ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
            Tags = ["support", "general"]
        });

        // Send message and get response - automatically persisted
        var response = await session.SendMessageAsync(userMessage);

        return session.ConversationId;
    }

    public async Task<string> ContinueConversationAsync(string conversationId, string userMessage)
    {
        // Resume existing conversation
        var session = await _sessionFactory.ResumeAsync(conversationId);

        // Previous messages are loaded, new messages are persisted
        var response = await session.SendMessageAsync(userMessage);

        return response.Content[0].Text;
    }
}
```

### Direct Store Access

For more control, use `IConversationStore` directly.

```csharp
using Goa.Clients.Bedrock.Conversation;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

public class ConversationService
{
    private readonly IConversationStore _store;

    public ConversationService(IConversationStore store)
    {
        _store = store;
    }

    public async Task<Conversation> CreateConversationAsync()
    {
        var result = await _store.CreateConversationAsync(
            new ConversationMetadata { Title = "My Conversation" },
            CancellationToken.None);

        if (result.IsError)
            throw new Exception(result.FirstError.Description);

        return result.Value;
    }

    public async Task AddExchangeAsync(string conversationId, string userText, string assistantText)
    {
        // Add user and assistant messages in a single transaction
        var messages = new[]
        {
            (ConversationRole.User, new Message
            {
                Role = ConversationRole.User,
                Content = [new ContentBlock { Text = userText }]
            }, (TokenUsage?)null),
            (ConversationRole.Assistant, new Message
            {
                Role = ConversationRole.Assistant,
                Content = [new ContentBlock { Text = assistantText }]
            }, new TokenUsage { InputTokens = 100, OutputTokens = 50, TotalTokens = 150 })
        };

        var result = await _store.AddMessagesAsync(conversationId, messages, CancellationToken.None);

        if (result.IsError)
            throw new Exception(result.FirstError.Description);
    }
}
```

### Pagination

Retrieve messages in batches for large conversations.

```csharp
public async Task<List<ConversationMessage>> GetAllMessagesAsync(string conversationId)
{
    var allMessages = new List<ConversationMessage>();
    string? paginationToken = null;

    do
    {
        var result = await _store.GetConversationWithMessagesAsync(
            conversationId,
            limit: 50,
            paginationToken: paginationToken,
            CancellationToken.None);

        if (result.IsError)
            throw new Exception(result.FirstError.Description);

        allMessages.AddRange(result.Value.Messages);
        paginationToken = result.Value.NextPaginationToken;

    } while (paginationToken != null);

    return allMessages;
}
```

### Configuration Options

```csharp
public class DynamoConversationStoreConfiguration
{
    // Table name for storing conversations
    string TableName { get; set; } = "conversations";

    // Primary key attribute names
    string PartitionKeyName { get; set; } = "PK";
    string SortKeyName { get; set; } = "SK";

    // TTL attribute name for DynamoDB expiration
    string TtlAttributeName { get; set; } = "TTL";

    // Partition key format function
    Func<string, string> ConversationPkFormat { get; set; } = id => $"Conversation#{id}";

    // Sort key value for conversation metadata
    string ConversationSkValue { get; set; } = "_";

    // Prefix for message sort keys (used with begins_with queries)
    string MessageSkPrefix { get; set; } = "message#";

    // Default TTL for conversations (null = no expiration)
    TimeSpan? DefaultTtl { get; set; } = TimeSpan.FromDays(7);

    // Default message limit when not specified
    int DefaultMessageLimit { get; set; } = 50;
}
```

### Metadata

Store custom data with conversations.

```csharp
// Create with metadata
var conversation = await _store.CreateConversationAsync(new ConversationMetadata
{
    Title = "Customer Support Chat",
    ModelId = "anthropic.claude-3-sonnet-20240229-v1:0",
    Tags = ["support", "billing", "priority-high"],
    CustomData = new Dictionary<string, string>
    {
        ["customerId"] = "cust_123",
        ["ticketId"] = "ticket_456",
        ["department"] = "billing"
    }
}, CancellationToken.None);

// Update metadata later
await _store.UpdateConversationAsync(conversation.Value.Id, new ConversationMetadata
{
    Title = "Customer Support Chat - Resolved",
    Tags = ["support", "billing", "resolved"]
}, CancellationToken.None);
```

### Deletion

Delete a conversation and all its messages.

```csharp
public async Task DeleteConversationAsync(string conversationId)
{
    var result = await _store.DeleteConversationAsync(conversationId, CancellationToken.None);

    if (result.IsError)
    {
        if (result.FirstError.Code == "Conversation.NotFound")
            throw new NotFoundException("Conversation not found");

        throw new Exception(result.FirstError.Description);
    }
}
```

## IConversationStore Interface

| Method | Description |
|--------|-------------|
| `CreateConversationAsync(metadata, ct)` | Create a new conversation with optional metadata |
| `GetConversationAsync(conversationId, ct)` | Retrieve conversation by ID |
| `GetConversationWithMessagesAsync(conversationId, limit, paginationToken, ct)` | Get conversation with paginated messages |
| `AddMessageAsync(conversationId, role, message, tokenUsage, ct)` | Add a single message |
| `AddMessagesAsync(conversationId, messages, ct)` | Add multiple messages atomically |
| `UpdateConversationAsync(conversationId, metadata, ct)` | Update conversation metadata |
| `DeleteConversationAsync(conversationId, ct)` | Delete conversation and all messages |

## Error Handling

All operations return `ErrorOr<T>` results for comprehensive error handling.

```csharp
var result = await _store.GetConversationAsync(conversationId, ct);

if (result.IsError)
{
    foreach (var error in result.Errors)
    {
        switch (error.Code)
        {
            case "Conversation.NotFound":
                // Handle not found
                break;
            case "Conversation.PaginationTokenInvalid":
                // Handle invalid pagination token
                break;
            default:
                // Handle other errors
                _logger.LogError("Error: {Code} - {Description}", error.Code, error.Description);
                break;
        }
    }
    return;
}

// Use successful result
var conversation = result.Value;
```

### Error Codes

| Code | Description |
|------|-------------|
| `Conversation.NotFound` | Conversation does not exist |
| `Conversation.PaginationTokenInvalid` | Malformed pagination token |
| `Conversation.MessagesEmpty` | No messages provided to AddMessagesAsync |
| `Conversation.MissingId` | Record missing required Id attribute |
| `Conversation.MissingCreatedAt` | Record missing required CreatedAt attribute |
| `Conversation.MissingUpdatedAt` | Record missing required UpdatedAt attribute |

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
