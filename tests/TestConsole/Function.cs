using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Conversation.Chat;
using Goa.Clients.Bedrock.Conversation.Dynamo;
using Goa.Clients.Bedrock.Mcp;
using Microsoft.Extensions.DependencyInjection.Extensions;

const string ModelId = "amazon.nova-micro-v1:0";

const string SystemPrompt = """
    You are a helpful household assistant that helps users manage their home tasks and appliances.

    When responding, you MUST use the following JSON format:
    {
        "message": "Your response message to the user",
        "entities": [
            {
                "type": "task|appliance",
                "id": "entity-id",
                "name": "entity name",
                "action": "created|completed|listed|none"
            }
        ],
        "messageType": "info|success|error|question"
    }

    Rules:
    - Always extract relevant entities from your response
    - Use tools when appropriate to list, create, or complete tasks
    - Use tools to list available appliances
    - Be concise and helpful
    - If no entities are relevant, use an empty array for entities
    """;

Environment.SetEnvironmentVariable("AWS_REGION", "eu-west-2");

// Configure services with Bedrock client and chat session factory
var services = new ServiceCollection();
services.AddBedrockDynamoConversationStore(config =>
{
    config.TableName = "haus-staging-data";
});
services.TryAddSingleton<IMcpToolAdapter, McpToolAdapter>();

#pragma warning disable ASP0000 // Do not call 'IServiceCollection.BuildServiceProvider' in 'ConfigureServices'
var serviceProvider = services.BuildServiceProvider();
#pragma warning restore ASP0000

// Create mock MCP tools
var mcpTools = CreateMockTools();

// Create chat session with the new abstraction
var factory = serviceProvider.GetRequiredService<IChatSessionFactory>();

var sessionResult = await factory.CreateAsync(new ChatSessionOptions
{
    ModelId = ModelId,
    SystemPrompt = SystemPrompt,
    PersistConversation = true,
    Metadata = new() { Title = "TestConsole Demo", ModelId = ModelId }
});

if (sessionResult.IsError)
{
    Console.WriteLine($"Failed to create session: {sessionResult.FirstError.Description}");
    return;
}

await using var session = sessionResult.Value;
session.RegisterTools(mcpTools);

Console.WriteLine($"Conversation ID: {session.ConversationId}\n");
Console.WriteLine("Bedrock Conversation Demo");
Console.WriteLine("=========================");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
        continue;

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await session.SendAsync(userInput);

    if (response.IsError)
    {
        Console.WriteLine($"Error: {response.FirstError.Description}");
        continue;
    }

    foreach (var tool in response.Value.ToolsExecuted)
    {
        Console.WriteLine($"  [Tool: {tool.ToolName}]");
    }

    Console.WriteLine($"Assistant: {(string.IsNullOrEmpty(response.Value.Text) ? "(no response)" : response.Value.Text)}");
    Console.WriteLine($"  [Tokens - Input: {response.Value.Usage.InputTokens}, Output: {response.Value.Usage.OutputTokens}]");
    Console.WriteLine();
}

Console.WriteLine($"Total tokens - Input: {session.TotalTokenUsage.InputTokens}, Output: {session.TotalTokenUsage.OutputTokens}");
Console.WriteLine("Goodbye!");

static List<McpToolDefinition> CreateMockTools()
{
    return
    [
        new McpToolDefinition
        {
            Name = "list_tasks",
            Description = "Lists all household tasks with their current status",
            InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "status": {
                            "type": "string",
                            "description": "Filter by status: pending, completed, or all",
                            "enum": ["pending", "completed", "all"]
                        }
                    }
                }
                """).RootElement,
            Handler = async (input, ct) =>
            {
                await Task.CompletedTask;
                var status = input.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString() ?? "all"
                    : "all";

                var tasks = new TaskItem[]
                {
                    new() { Id = "task-1", Name = "Buy groceries", Status = "pending", DueDate = "2024-01-26" },
                    new() { Id = "task-2", Name = "Clean kitchen", Status = "completed", DueDate = "2024-01-25" },
                    new() { Id = "task-3", Name = "Take out trash", Status = "pending", DueDate = "2024-01-25" },
                    new() { Id = "task-4", Name = "Do laundry", Status = "pending", DueDate = "2024-01-27" }
                };

                var filtered = status == "all"
                    ? tasks
                    : tasks.Where(t => t.Status == status).ToArray();

                return JsonDocument.Parse(JsonSerializer.Serialize(filtered, DemoJsonContext.Default.TaskItemArray)).RootElement;
            }
        },
        new McpToolDefinition
        {
            Name = "create_task",
            Description = "Creates a new household task",
            InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "name": {
                            "type": "string",
                            "description": "The name of the task"
                        },
                        "dueDate": {
                            "type": "string",
                            "description": "The due date in YYYY-MM-DD format"
                        }
                    },
                    "required": ["name"]
                }
                """).RootElement,
            Handler = async (input, ct) =>
            {
                await Task.CompletedTask;
                var name = input.GetProperty("name").GetString() ?? "Unnamed task";
                var dueDate = input.TryGetProperty("dueDate", out var dueProp)
                    ? dueProp.GetString() ?? "2024-01-30"
                    : "2024-01-30";

                var newTask = new CreatedTaskResult
                {
                    Id = $"task-{Guid.NewGuid():N}"[..12],
                    Name = name,
                    Status = "pending",
                    DueDate = dueDate,
                    Created = true
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(newTask, DemoJsonContext.Default.CreatedTaskResult)).RootElement;
            }
        },
        new McpToolDefinition
        {
            Name = "complete_task",
            Description = "Marks a task as completed",
            InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "taskId": {
                            "type": "string",
                            "description": "The ID of the task to complete"
                        }
                    },
                    "required": ["taskId"]
                }
                """).RootElement,
            Handler = async (input, ct) =>
            {
                await Task.CompletedTask;
                var taskId = input.GetProperty("taskId").GetString() ?? "unknown";

                var result = new CompletedTaskResult
                {
                    Id = taskId,
                    Status = "completed",
                    CompletedAt = DateTime.UtcNow.ToString("O"),
                    Success = true
                };

                return JsonDocument.Parse(JsonSerializer.Serialize(result, DemoJsonContext.Default.CompletedTaskResult)).RootElement;
            }
        },
        new McpToolDefinition
        {
            Name = "list_appliances",
            Description = "Lists all smart home appliances and their current status",
            InputSchema = JsonDocument.Parse("""
                {
                    "type": "object",
                    "properties": {
                        "room": {
                            "type": "string",
                            "description": "Filter by room name, or omit for all rooms"
                        }
                    }
                }
                """).RootElement,
            Handler = async (input, ct) =>
            {
                await Task.CompletedTask;
                var room = input.TryGetProperty("room", out var roomProp)
                    ? roomProp.GetString()
                    : null;

                var appliances = new Appliance[]
                {
                    new() { Id = "app-1", Name = "Living Room Thermostat", Room = "Living Room", Status = "on", Temperature = 72 },
                    new() { Id = "app-2", Name = "Kitchen Lights", Room = "Kitchen", Status = "off", Temperature = null },
                    new() { Id = "app-3", Name = "Bedroom AC", Room = "Bedroom", Status = "on", Temperature = 68 },
                    new() { Id = "app-4", Name = "Front Door Lock", Room = "Entrance", Status = "locked", Temperature = null },
                    new() { Id = "app-5", Name = "Garage Door", Room = "Garage", Status = "closed", Temperature = null }
                };

                var filtered = string.IsNullOrEmpty(room)
                    ? appliances
                    : appliances.Where(a => a.Room.Equals(room, StringComparison.OrdinalIgnoreCase)).ToArray();

                return JsonDocument.Parse(JsonSerializer.Serialize(filtered, DemoJsonContext.Default.ApplianceArray)).RootElement;
            }
        }
    ];
}

// AOT-compatible model types
internal sealed class TaskItem
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("dueDate")]
    public required string DueDate { get; init; }
}

internal sealed class CreatedTaskResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("dueDate")]
    public required string DueDate { get; init; }

    [JsonPropertyName("created")]
    public required bool Created { get; init; }
}

internal sealed class CompletedTaskResult
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("completedAt")]
    public required string CompletedAt { get; init; }

    [JsonPropertyName("success")]
    public required bool Success { get; init; }
}

internal sealed class Appliance
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("room")]
    public required string Room { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("temperature")]
    public int? Temperature { get; init; }
}

[JsonSerializable(typeof(TaskItem[]))]
[JsonSerializable(typeof(CreatedTaskResult))]
[JsonSerializable(typeof(CompletedTaskResult))]
[JsonSerializable(typeof(Appliance[]))]
internal sealed partial class DemoJsonContext : JsonSerializerContext;
