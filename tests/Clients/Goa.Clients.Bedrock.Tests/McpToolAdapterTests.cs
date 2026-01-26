using System.Text.Json;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Tests;

public class McpToolAdapterTests
{
    [Test]
    public async Task ToBedrockTools_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var adapter = new McpToolAdapter();

        // Act
        var result = adapter.ToBedrockTools([]);

        // Assert
        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ToBedrockTools_SingleTool_ReturnsCorrectBedrockTool()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var inputSchema = JsonDocument.Parse("""{"type": "object", "properties": {"query": {"type": "string"}}}""").RootElement;
        var mcpTool = new McpToolDefinition
        {
            Name = "search",
            Description = "Search the web",
            InputSchema = inputSchema,
            Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement)
        };

        // Act
        var result = adapter.ToBedrockTools([mcpTool]);

        // Assert
        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0].ToolSpec.Name).IsEqualTo("search");
        await Assert.That(result[0].ToolSpec.Description).IsEqualTo("Search the web");
        await Assert.That(result[0].ToolSpec.InputSchema.Json.GetRawText()).IsEqualTo(inputSchema.GetRawText());
    }

    [Test]
    public async Task ToBedrockTools_MultipleTools_ReturnsAllTools()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var inputSchema = JsonDocument.Parse("{}").RootElement;
        var tools = new[]
        {
            new McpToolDefinition
            {
                Name = "tool1",
                Description = "First tool",
                InputSchema = inputSchema,
                Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement)
            },
            new McpToolDefinition
            {
                Name = "tool2",
                Description = "Second tool",
                InputSchema = inputSchema,
                Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement)
            }
        };

        // Act
        var result = adapter.ToBedrockTools(tools);

        // Assert
        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result[0].ToolSpec.Name).IsEqualTo("tool1");
        await Assert.That(result[1].ToolSpec.Name).IsEqualTo("tool2");
    }

    [Test]
    public async Task ToBedrockTools_NullDescription_SetsNullOnToolSpec()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var mcpTool = new McpToolDefinition
        {
            Name = "test_tool",
            Description = null,
            InputSchema = JsonDocument.Parse("{}").RootElement,
            Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement)
        };

        // Act
        var result = adapter.ToBedrockTools([mcpTool]);

        // Assert
        await Assert.That(result[0].ToolSpec.Description).IsNull();
    }

    [Test]
    public async Task ExecuteToolAsync_ToolNotFound_ReturnsErrorResult()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        adapter.ToBedrockTools([]); // Register empty tools

        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_123",
            Name = "unknown_tool",
            Input = JsonDocument.Parse("{}").RootElement
        };

        // Act
        var result = await adapter.ExecuteToolAsync(toolUse);

        // Assert
        await Assert.That(result.ToolUseId).IsEqualTo("use_123");
        await Assert.That(result.Status).IsEqualTo("error");
        await Assert.That(result.Content).Count().IsEqualTo(1);
        await Assert.That(result.Content[0].Text).Contains("unknown_tool");
        await Assert.That(result.Content[0].Text).Contains("not found");
    }

    [Test]
    public async Task ExecuteToolAsync_ToolFound_ExecutesHandler()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var handlerCalled = false;
        var expectedResult = JsonDocument.Parse("""{"result": "success"}""").RootElement;

        var mcpTool = new McpToolDefinition
        {
            Name = "test_tool",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (input, _) =>
            {
                handlerCalled = true;
                return Task.FromResult(expectedResult);
            }
        };

        adapter.ToBedrockTools([mcpTool]);

        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_456",
            Name = "test_tool",
            Input = JsonDocument.Parse("""{"param": "value"}""").RootElement
        };

        // Act
        var result = await adapter.ExecuteToolAsync(toolUse);

        // Assert
        await Assert.That(handlerCalled).IsTrue();
        await Assert.That(result.ToolUseId).IsEqualTo("use_456");
        await Assert.That(result.Status).IsEqualTo("success");
        await Assert.That(result.Content[0].Text).IsEqualTo(expectedResult.GetRawText());
    }

    [Test]
    public async Task ExecuteToolAsync_HandlerReceivesCorrectInput()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        JsonElement? receivedInput = null;

        var mcpTool = new McpToolDefinition
        {
            Name = "test_tool",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (input, _) =>
            {
                receivedInput = input;
                return Task.FromResult(JsonDocument.Parse("{}").RootElement);
            }
        };

        adapter.ToBedrockTools([mcpTool]);

        var inputJson = """{"location": "Seattle", "units": "metric"}""";
        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_789",
            Name = "test_tool",
            Input = JsonDocument.Parse(inputJson).RootElement
        };

        // Act
        await adapter.ExecuteToolAsync(toolUse);

        // Assert
        await Assert.That(receivedInput).IsNotNull();
        await Assert.That(receivedInput!.Value.GetProperty("location").GetString()).IsEqualTo("Seattle");
        await Assert.That(receivedInput!.Value.GetProperty("units").GetString()).IsEqualTo("metric");
    }

    [Test]
    public async Task ExecuteToolAsync_HandlerThrows_ReturnsErrorResult()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var mcpTool = new McpToolDefinition
        {
            Name = "failing_tool",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (_, _) => throw new InvalidOperationException("Tool execution failed")
        };

        adapter.ToBedrockTools([mcpTool]);

        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_error",
            Name = "failing_tool",
            Input = JsonDocument.Parse("{}").RootElement
        };

        // Act
        var result = await adapter.ExecuteToolAsync(toolUse);

        // Assert
        await Assert.That(result.ToolUseId).IsEqualTo("use_error");
        await Assert.That(result.Status).IsEqualTo("error");
        await Assert.That(result.Content[0].Text).IsEqualTo("Tool execution failed");
    }

    [Test]
    public async Task ExecuteToolAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var mcpTool = new McpToolDefinition
        {
            Name = "slow_tool",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = async (_, ct) =>
            {
                await Task.Delay(10000, ct);
                return JsonDocument.Parse("{}").RootElement;
            }
        };

        adapter.ToBedrockTools([mcpTool]);

        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_cancel",
            Name = "slow_tool",
            Input = JsonDocument.Parse("{}").RootElement
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await adapter.ExecuteToolAsync(toolUse, cts.Token));
    }

    [Test]
    public async Task ExecuteToolAsync_PassesCancellationToken()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        CancellationToken? receivedToken = null;

        var mcpTool = new McpToolDefinition
        {
            Name = "token_tool",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (_, ct) =>
            {
                receivedToken = ct;
                return Task.FromResult(JsonDocument.Parse("{}").RootElement);
            }
        };

        adapter.ToBedrockTools([mcpTool]);

        var toolUse = new ToolUseBlock
        {
            ToolUseId = "use_token",
            Name = "token_tool",
            Input = JsonDocument.Parse("{}").RootElement
        };

        using var cts = new CancellationTokenSource();

        // Act
        await adapter.ExecuteToolAsync(toolUse, cts.Token);

        // Assert
        await Assert.That(receivedToken).IsNotNull();
        await Assert.That(receivedToken!.Value).IsEqualTo(cts.Token);
    }

    [Test]
    public async Task ToBedrockTools_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var adapter = new McpToolAdapter();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            adapter.ToBedrockTools(null!);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task ExecuteToolAsync_NullToolUse_ThrowsArgumentNullException()
    {
        // Arrange
        var adapter = new McpToolAdapter();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await adapter.ExecuteToolAsync(null!));
    }

    [Test]
    public async Task ToBedrockTools_CalledMultipleTimes_ReplacesRegisteredTools()
    {
        // Arrange
        var adapter = new McpToolAdapter();
        var tool1 = new McpToolDefinition
        {
            Name = "tool1",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (_, _) => Task.FromResult(JsonDocument.Parse("""{"from": "first"}""").RootElement)
        };
        var tool2 = new McpToolDefinition
        {
            Name = "tool2",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (_, _) => Task.FromResult(JsonDocument.Parse("""{"from": "second"}""").RootElement)
        };

        // Act
        adapter.ToBedrockTools([tool1]);
        adapter.ToBedrockTools([tool2]);

        var toolUse1 = new ToolUseBlock { ToolUseId = "1", Name = "tool1", Input = JsonDocument.Parse("{}").RootElement };
        var toolUse2 = new ToolUseBlock { ToolUseId = "2", Name = "tool2", Input = JsonDocument.Parse("{}").RootElement };

        var result1 = await adapter.ExecuteToolAsync(toolUse1);
        var result2 = await adapter.ExecuteToolAsync(toolUse2);

        // Assert - tool1 should no longer exist after second ToBedrockTools call
        await Assert.That(result1.Status).IsEqualTo("error");
        await Assert.That(result2.Status).IsEqualTo("success");
    }
}
