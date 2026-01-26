using System.Text.Json;
using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Chat;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;
using Moq;

namespace Goa.Clients.Bedrock.Conversation.Tests;

public class ChatSessionTests
{
    private const string TestModelId = "anthropic.claude-3-sonnet-20240229-v1:0";

    private static ChatSessionOptions CreateDefaultOptions() => new()
    {
        ModelId = TestModelId,
        SystemPrompt = "You are a helpful assistant.",
        MaxTokens = 1024,
        Temperature = 0.7f,
        PersistConversation = false
    };

    [Test]
    public async Task SendAsync_StringMessage_CallsBedrockClient()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        var response = new ConverseResponse
        {
            StopReason = StopReason.EndTurn,
            Output = new ConverseOutput
            {
                Message = new Message
                {
                    Role = ConversationRole.Assistant,
                    Content = [new ContentBlock { Text = "Hello! How can I help you?" }]
                }
            },
            Usage = new TokenUsage { InputTokens = 10, OutputTokens = 20, TotalTokens = 30 }
        };

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        var result = await session.SendAsync("Hello");

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Text).IsEqualTo("Hello! How can I help you?");
        await Assert.That(result.Value.StopReason).IsEqualTo(StopReason.EndTurn);

        mockClient.Verify(c => c.ConverseAsync(
            It.Is<ConverseRequest>(r => r.Messages.Count == 1 && r.Messages[0].Role == ConversationRole.User),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_ContentBlocks_BuildsCorrectRequest()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        ConverseRequest? capturedRequest = null;
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = "OK" }] } }
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        var contentBlocks = new List<ContentBlock>
        {
            new() { Text = "What is in this image?" },
            new() { Image = new ImageBlock { Format = "png", Source = new ImageSource { Bytes = "base64data" } } }
        };

        // Act
        await session.SendAsync(contentBlocks);

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.Messages[0].Content).Count().IsEqualTo(2);
        await Assert.That(capturedRequest.Messages[0].Content[0].Text).IsEqualTo("What is in this image?");
        await Assert.That(capturedRequest.Messages[0].Content[1].Image).IsNotNull();
    }

    [Test]
    public async Task SendAsync_AccumulatesTokenUsage()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        var callCount = 0;
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new ConverseResponse
                {
                    StopReason = StopReason.EndTurn,
                    Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = $"Response {callCount}" }] } },
                    Usage = new TokenUsage { InputTokens = 10, OutputTokens = 20, TotalTokens = 30 }
                };
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        await session.SendAsync("First message");
        await session.SendAsync("Second message");

        // Assert
        await Assert.That(session.TotalTokenUsage.InputTokens).IsEqualTo(20);
        await Assert.That(session.TotalTokenUsage.OutputTokens).IsEqualTo(40);
        await Assert.That(session.TotalTokenUsage.TotalTokens).IsEqualTo(60);
    }

    [Test]
    public async Task SendAsync_WithToolUse_ExecutesToolAndContinues()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        var toolUseBlock = new ToolUseBlock
        {
            ToolUseId = "tool-123",
            Name = "get_weather",
            Input = JsonDocument.Parse("""{"location": "Seattle"}""").RootElement
        };

        var toolResultBlock = new ToolResultBlock
        {
            ToolUseId = "tool-123",
            Status = "success",
            Content = [new ContentBlock { Text = """{"temp": 72}""" }]
        };

        var callCount = 0;
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return new ConverseResponse
                    {
                        StopReason = StopReason.ToolUse,
                        Output = new ConverseOutput
                        {
                            Message = new Message
                            {
                                Role = ConversationRole.Assistant,
                                Content = [new ContentBlock { ToolUse = toolUseBlock }]
                            }
                        },
                        Usage = new TokenUsage { InputTokens = 10, OutputTokens = 5, TotalTokens = 15 }
                    };
                }
                return new ConverseResponse
                {
                    StopReason = StopReason.EndTurn,
                    Output = new ConverseOutput
                    {
                        Message = new Message
                        {
                            Role = ConversationRole.Assistant,
                            Content = [new ContentBlock { Text = "The weather in Seattle is 72F." }]
                        }
                    },
                    Usage = new TokenUsage { InputTokens = 20, OutputTokens = 10, TotalTokens = 30 }
                };
            });

        var mcpTool = new McpToolDefinition
        {
            Name = "get_weather",
            InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement,
            Handler = (_, _) => Task.FromResult(JsonDocument.Parse("""{"temp": 72}""").RootElement)
        };

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns([new Tool { ToolSpec = new ToolSpec { Name = "get_weather" } }]);

        mockAdapter.Setup(a => a.ExecuteToolAsync(It.IsAny<ToolUseBlock>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(toolResultBlock);

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);
        session.RegisterTool(mcpTool);

        // Act
        var result = await session.SendAsync("What's the weather in Seattle?");

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Text).IsEqualTo("The weather in Seattle is 72F.");
        await Assert.That(result.Value.ToolsExecuted).Count().IsEqualTo(1);
        await Assert.That(result.Value.ToolsExecuted[0].ToolName).IsEqualTo("get_weather");
        await Assert.That(result.Value.ToolsExecuted[0].Success).IsTrue();

        mockClient.Verify(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        mockAdapter.Verify(a => a.ExecuteToolAsync(It.IsAny<ToolUseBlock>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_ExceedsMaxToolIterations_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        var toolUseBlock = new ToolUseBlock
        {
            ToolUseId = "tool-loop",
            Name = "loop_tool",
            Input = JsonDocument.Parse("{}").RootElement
        };

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.ToolUse,
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Role = ConversationRole.Assistant,
                        Content = [new ContentBlock { ToolUse = toolUseBlock }]
                    }
                },
                Usage = new TokenUsage { InputTokens = 5, OutputTokens = 5, TotalTokens = 10 }
            });

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns([new Tool { ToolSpec = new ToolSpec { Name = "loop_tool" } }]);

        mockAdapter.Setup(a => a.ExecuteToolAsync(It.IsAny<ToolUseBlock>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ToolResultBlock { ToolUseId = "tool-loop", Status = "success", Content = [] });

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            MaxToolIterations = 2
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, options: options);
        session.RegisterTool(new McpToolDefinition { Name = "loop_tool", InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement, Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement) });

        // Act
        var result = await session.SendAsync("Start looping");

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ChatErrorCodes.MaxToolIterationsExceeded);
    }

    [Test]
    public async Task SendAsync_ClientReturnsError_PropagatesError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Bedrock.Error", "Model not available"));

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        var result = await session.SendAsync("Hello");

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("Bedrock.Error");
    }

    [Test]
    public async Task RegisterTool_AddsSingleTool()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        IEnumerable<McpToolDefinition>? capturedTools = null;
        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Callback<IEnumerable<McpToolDefinition>>(t => capturedTools = t)
            .Returns([new Tool { ToolSpec = new ToolSpec { Name = "test_tool" } }]);

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = "OK" }] } }
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);
        var tool = new McpToolDefinition { Name = "test_tool", InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement, Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement) };

        // Act
        session.RegisterTool(tool);
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedTools).IsNotNull();
        await Assert.That(capturedTools!.Count()).IsEqualTo(1);
        await Assert.That(capturedTools!.First().Name).IsEqualTo("test_tool");
    }

    [Test]
    public async Task RegisterTools_AddsMultipleTools()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        IEnumerable<McpToolDefinition>? capturedTools = null;
        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Callback<IEnumerable<McpToolDefinition>>(t => capturedTools = t)
            .Returns([]);

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = "OK" }] } }
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);
        var tools = new[]
        {
            new McpToolDefinition { Name = "tool1", InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement, Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement) },
            new McpToolDefinition { Name = "tool2", InputSchema = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement, Handler = (_, _) => Task.FromResult(JsonDocument.Parse("{}").RootElement) }
        };

        // Act
        session.RegisterTools(tools);
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedTools).IsNotNull();
        await Assert.That(capturedTools!.Count()).IsEqualTo(2);
    }

    [Test]
    public async Task GetHistoryAsync_ReturnsInMemoryHistory()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Role = ConversationRole.Assistant,
                        Content = [new ContentBlock { Text = "Hello!" }]
                    }
                }
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        await session.SendAsync("Hi there");
        var historyResult = await session.GetHistoryAsync();

        // Assert
        await Assert.That(historyResult.IsError).IsFalse();
        await Assert.That(historyResult.Value).Count().IsEqualTo(2);
        await Assert.That(historyResult.Value[0].Role).IsEqualTo(ConversationRole.User);
        await Assert.That(historyResult.Value[1].Role).IsEqualTo(ConversationRole.Assistant);
    }

    [Test]
    public async Task SendAsync_WithPersistence_SavesMessages()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Role = ConversationRole.Assistant,
                        Content = [new ContentBlock { Text = "Response" }]
                    }
                },
                Usage = new TokenUsage { InputTokens = 10, OutputTokens = 5, TotalTokens = 15 }
            });

        mockStore.Setup(s => s.AddMessageAsync(It.IsAny<string>(), It.IsAny<ConversationRole>(), It.IsAny<Message>(), It.IsAny<TokenUsage?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationMessage { Id = "msg-1", ConversationId = "conv-1", Message = new Message() });

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, mockStore.Object, options, "conv-1");

        // Act
        await session.SendAsync("Hello");

        // Assert - should save user message and assistant message
        mockStore.Verify(s => s.AddMessageAsync("conv-1", ConversationRole.User, It.IsAny<Message>(), null, It.IsAny<CancellationToken>()), Times.Once);
        mockStore.Verify(s => s.AddMessageAsync("conv-1", ConversationRole.Assistant, It.IsAny<Message>(), It.IsAny<TokenUsage?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SendAsync_BuildsRequestWithCorrectOptions()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        ConverseRequest? capturedRequest = null;
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = "OK" }] } }
            });

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            SystemPrompt = "You are helpful.",
            MaxTokens = 2048,
            Temperature = 0.5f,
            TopP = 0.9f,
            StopSequences = ["###", "END"]
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, options: options);

        // Act
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.ModelId).IsEqualTo(TestModelId);
        await Assert.That(capturedRequest.System).Count().IsEqualTo(1);
        await Assert.That(capturedRequest.System![0].Text).IsEqualTo("You are helpful.");
        await Assert.That(capturedRequest.InferenceConfig!.MaxTokens).IsEqualTo(2048);
        await Assert.That(capturedRequest.InferenceConfig.Temperature).IsEqualTo(0.5f);
        await Assert.That(capturedRequest.InferenceConfig.TopP).IsEqualTo(0.9f);
        await Assert.That(capturedRequest.InferenceConfig.StopSequences).Count().IsEqualTo(2);
    }

    [Test]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert - should not throw
        await session.DisposeAsync();
        await session.DisposeAsync();
    }

    [Test]
    public async Task SendAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.SendAsync((string)null!));
    }

    [Test]
    public async Task SendAsync_NullContentBlocks_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.SendAsync((IEnumerable<ContentBlock>)null!));
    }

    [Test]
    public async Task RegisterTool_NullTool_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            session.RegisterTool(null!);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task RegisterTools_NullTools_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            session.RegisterTools(null!);
            await Task.CompletedTask;
        });
    }

    private static async Task<IChatSession> CreateSession(
        IBedrockClient client,
        IMcpToolAdapter adapter,
        IConversationStore? store = null,
        ChatSessionOptions? options = null,
        string? conversationId = null)
    {
        var factory = new ChatSessionFactory(client, adapter, store);

        ChatSessionOptions sessionOptions;
        if (conversationId != null && store != null)
        {
            // For persisted sessions, we need to set up the store mock
            var mockStore = Mock.Get(store);
            mockStore.Setup(s => s.CreateConversationAsync(It.IsAny<ConversationMetadata?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Entities.Conversation { Id = conversationId, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

            sessionOptions = options != null
                ? new ChatSessionOptions
                {
                    ModelId = options.ModelId,
                    SystemPrompt = options.SystemPrompt,
                    MaxTokens = options.MaxTokens,
                    Temperature = options.Temperature,
                    TopP = options.TopP,
                    StopSequences = options.StopSequences,
                    Metadata = options.Metadata,
                    MaxToolIterations = options.MaxToolIterations,
                    PersistConversation = true
                }
                : new ChatSessionOptions
                {
                    ModelId = TestModelId,
                    PersistConversation = true
                };
        }
        else
        {
            sessionOptions = options ?? CreateDefaultOptions();
        }

        var result = await factory.CreateAsync(sessionOptions);
        return result.Value;
    }
}
