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
        mockStore.Verify(s => s.AddMessageAsync("conv-1", ConversationRole.User, It.IsAny<Message>(), null, It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>()), Times.Once);
        mockStore.Verify(s => s.AddMessageAsync("conv-1", ConversationRole.Assistant, It.IsAny<Message>(), It.IsAny<TokenUsage?>(), It.IsAny<CancellationToken>(), It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>()), Times.Once);
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

    [Test]
    public async Task ChangeModelAsync_UpdatesModelForSubsequentCalls()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        var capturedRequests = new List<ConverseRequest>();
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((r, _) => capturedRequests.Add(r))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput { Message = new Message { Role = ConversationRole.Assistant, Content = [new ContentBlock { Text = "OK" }] } }
            });

        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        await session.SendAsync("First");
        await session.ChangeModelAsync("amazon.nova-pro-v1:0");
        await session.SendAsync("Second");

        // Assert
        await Assert.That(capturedRequests).Count().IsEqualTo(2);
        await Assert.That(capturedRequests[0].ModelId).IsEqualTo(TestModelId);
        await Assert.That(capturedRequests[1].ModelId).IsEqualTo("amazon.nova-pro-v1:0");
    }

    [Test]
    public async Task ChangeModelAsync_WithPersistence_UpdatesStore()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        mockStore.Setup(s => s.AddMessageAsync(It.IsAny<string>(), It.IsAny<ConversationRole>(), It.IsAny<Message>(), It.IsAny<TokenUsage?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversationMessage { Id = "msg-1", ConversationId = "conv-1", Message = new Message() });

        mockStore.Setup(s => s.UpdateConversationAsync(It.IsAny<string>(), It.IsAny<ConversationMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entities.Conversation { Id = "conv-1", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, mockStore.Object, options, "conv-1");

        // Act
        var result = await session.ChangeModelAsync("amazon.nova-pro-v1:0");

        // Assert
        await Assert.That(result.IsError).IsFalse();
        mockStore.Verify(s => s.UpdateConversationAsync(
            "conv-1",
            It.Is<ConversationMetadata>(m => m.ModelId == "amazon.nova-pro-v1:0"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ChangeModelAsync_WithoutPersistence_DoesNotCallStore()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act
        var result = await session.ChangeModelAsync("amazon.nova-pro-v1:0");

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(session.ModelId).IsEqualTo("amazon.nova-pro-v1:0");
    }

    [Test]
    public async Task ChangeModelAsync_NullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await session.ChangeModelAsync(null!));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await session.ChangeModelAsync(""));
    }

    [Test]
    public async Task ModelId_ReturnsCurrentModelId()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var session = await CreateSession(mockClient.Object, mockAdapter.Object);

        // Assert initial
        await Assert.That(session.ModelId).IsEqualTo(TestModelId);

        // Act
        await session.ChangeModelAsync("new-model-id");

        // Assert updated
        await Assert.That(session.ModelId).IsEqualTo("new-model-id");
    }

    [Test]
    public async Task SendAsync_WithServiceTier_IncludesInRequest()
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
            ServiceTier = ServiceTier.Priority
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, options: options);

        // Act
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.RequestMetadata?.ServiceTier).IsEqualTo(ServiceTier.Priority);
    }

    [Test]
    public async Task SendAsync_WithOutputConfig_IncludesInRequest()
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

        var outputConfig = new OutputConfig
        {
            TextFormat = new OutputFormat
            {
                Type = "json_schema",
                Structure = new OutputFormatStructure
                {
                    JsonSchema = new JsonSchemaDefinition
                    {
                        Name = "TestSchema",
                        Schema = """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}"""
                    }
                }
            }
        };

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            OutputConfig = outputConfig
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, options: options);

        // Act
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.OutputConfig).IsNotNull();
        await Assert.That(capturedRequest.OutputConfig!.TextFormat).IsNotNull();
        await Assert.That(capturedRequest.OutputConfig.TextFormat!.Type).IsEqualTo("json_schema");
        await Assert.That(capturedRequest.OutputConfig.TextFormat.Structure!.JsonSchema!.Name).IsEqualTo("TestSchema");
    }

    [Test]
    public async Task SendAsync_WithoutOutputConfig_DoesNotIncludeInRequest()
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

        // Act
        await session.SendAsync("Test");

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.OutputConfig).IsNull();
    }

    [Test]
    public async Task SendAsync_WhenResponseContainsOnlyXmlTags_PreservesOriginalContent()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        // Bedrock responds with ONLY thinking tags - no visible text
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Role = ConversationRole.Assistant,
                        Content = [new ContentBlock { Text = "<thinking>The previous request was about contacts.</thinking>" }]
                    }
                },
                Usage = new TokenUsage { InputTokens = 10, OutputTokens = 46, TotalTokens = 56 }
            });

        Message? persistedAssistantMessage = null;
        mockStore.Setup(s => s.AddMessageAsync(
                It.IsAny<string>(),
                ConversationRole.Assistant,
                It.IsAny<Message>(),
                It.IsAny<TokenUsage?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>()))
            .Callback<string, ConversationRole, Message, TokenUsage?, CancellationToken, IReadOnlyDictionary<string, IReadOnlyList<string>>?>(
                (_, _, msg, _, _, _) => persistedAssistantMessage = msg)
            .ReturnsAsync(new ConversationMessage { Id = "msg-2", ConversationId = "conv-1", Message = new Message() });

        mockStore.Setup(s => s.AddMessageAsync(
                It.IsAny<string>(),
                ConversationRole.User,
                It.IsAny<Message>(),
                It.IsAny<TokenUsage?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>()))
            .ReturnsAsync(new ConversationMessage { Id = "msg-1", ConversationId = "conv-1", Message = new Message() });

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        var session = await CreateSession(mockClient.Object, mockAdapter.Object, mockStore.Object, options, "conv-1");

        // Act
        var result = await session.SendAsync("Debug: why didn't you find this immediately?");

        // Assert - response should not be empty
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Text).IsNotEmpty();

        // Assert - persisted message must have non-empty content
        await Assert.That(persistedAssistantMessage).IsNotNull();
        await Assert.That(persistedAssistantMessage!.Content).Count().IsGreaterThan(0);

        // Assert - extracted tags should still contain the thinking content
        await Assert.That(result.Value.ExtractedTags).ContainsKey("thinking");
    }

    [Test]
    public async Task ResumeAsync_WithEmptyContentMessages_FiltersThemOut()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockAdapter.Setup(a => a.ToBedrockTools(It.IsAny<IEnumerable<McpToolDefinition>>()))
            .Returns(new List<Tool>());

        var conversationWithMessages = new ConversationWithMessages
        {
            Conversation = new Entities.Conversation
            {
                Id = "conv-corrupted",
                Metadata = new ConversationMetadata { ModelId = TestModelId },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                MessageCount = 3
            },
            Messages =
            [
                new ConversationMessage
                {
                    Id = "msg-1",
                    ConversationId = "conv-corrupted",
                    Role = Bedrock.Enums.ConversationRole.User,
                    Message = new Message
                    {
                        Role = Bedrock.Enums.ConversationRole.User,
                        Content = [new ContentBlock { Text = "Hello" }]
                    }
                },
                new ConversationMessage
                {
                    Id = "msg-2",
                    ConversationId = "conv-corrupted",
                    Role = Bedrock.Enums.ConversationRole.Assistant,
                    Message = new Message
                    {
                        Role = Bedrock.Enums.ConversationRole.Assistant,
                        Content = [] // Empty - corrupted message
                    }
                },
                new ConversationMessage
                {
                    Id = "msg-3",
                    ConversationId = "conv-corrupted",
                    Role = Bedrock.Enums.ConversationRole.User,
                    Message = new Message
                    {
                        Role = Bedrock.Enums.ConversationRole.User,
                        Content = [new ContentBlock { Text = "Hello?" }]
                    }
                }
            ]
        };

        mockStore.Setup(s => s.GetConversationWithMessagesAsync("conv-corrupted", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversationWithMessages);

        ConverseRequest? capturedRequest = null;
        mockClient.Setup(c => c.ConverseAsync(It.IsAny<ConverseRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConverseRequest, CancellationToken>((r, _) => capturedRequest = r)
            .ReturnsAsync(new ConverseResponse
            {
                StopReason = StopReason.EndTurn,
                Output = new ConverseOutput
                {
                    Message = new Message
                    {
                        Role = ConversationRole.Assistant,
                        Content = [new ContentBlock { Text = "Hi there!" }]
                    }
                },
                Usage = new TokenUsage { InputTokens = 10, OutputTokens = 5, TotalTokens = 15 }
            });

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        // Act
        var sessionResult = await factory.ResumeAsync("conv-corrupted", new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        });

        await Assert.That(sessionResult.IsError).IsFalse();
        var session = sessionResult.Value;

        mockStore.Setup(s => s.AddMessageAsync(
                It.IsAny<string>(),
                It.IsAny<ConversationRole>(),
                It.IsAny<Message>(),
                It.IsAny<TokenUsage?>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IReadOnlyDictionary<string, IReadOnlyList<string>>?>()))
            .ReturnsAsync(new ConversationMessage { Id = "msg-4", ConversationId = "conv-corrupted", Message = new Message() });

        var result = await session.SendAsync("New message");

        // Assert - request should only contain non-empty messages (msg-1, msg-3) plus the new message
        // The empty msg-2 should have been filtered out
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(capturedRequest).IsNotNull();

        // History: msg-1 (user) + msg-3 (user) + new message (user) = 3 messages
        // msg-2 was filtered out
        await Assert.That(capturedRequest!.Messages).Count().IsEqualTo(3);
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
                    ServiceTier = options.ServiceTier,
                    OutputConfig = options.OutputConfig,
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
