using ErrorOr;
using Goa.Clients.Bedrock.Conversation.Chat;
using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Mcp;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.Converse;
using Moq;

namespace Goa.Clients.Bedrock.Conversation.Tests;

public class ChatSessionFactoryTests
{
    private const string TestModelId = "anthropic.claude-3-sonnet-20240229-v1:0";

    [Test]
    public async Task CreateAsync_WithoutPersistence_ReturnsSessionWithNullConversationId()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object);

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = false
        };

        // Act
        var result = await factory.CreateAsync(options);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ConversationId).IsNull();
    }

    [Test]
    public async Task CreateAsync_WithPersistenceButNoStore_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, store: null);

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        // Act
        var result = await factory.CreateAsync(options);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ChatErrorCodes.PersistenceNotConfigured);
    }

    [Test]
    public async Task CreateAsync_WithPersistenceAndStore_CreatesConversationAndReturnsSession()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        var createdConversation = new Entities.Conversation
        {
            Id = "conv-123",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        mockStore.Setup(s => s.CreateConversationAsync(It.IsAny<ConversationMetadata?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdConversation);

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        // Act
        var result = await factory.CreateAsync(options);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ConversationId).IsEqualTo("conv-123");
        mockStore.Verify(s => s.CreateConversationAsync(It.IsAny<ConversationMetadata?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateAsync_WithMetadata_PassesMetadataToStore()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        ConversationMetadata? capturedMetadata = null;
        mockStore.Setup(s => s.CreateConversationAsync(It.IsAny<ConversationMetadata?>(), It.IsAny<CancellationToken>()))
            .Callback<ConversationMetadata?, CancellationToken>((m, _) => capturedMetadata = m)
            .ReturnsAsync(new Entities.Conversation { Id = "conv-456", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow });

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true,
            Metadata = new ConversationMetadata
            {
                Title = "Test Conversation",
                Tags = ["test", "unit"]
            }
        };

        // Act
        await factory.CreateAsync(options);

        // Assert
        await Assert.That(capturedMetadata).IsNotNull();
        await Assert.That(capturedMetadata!.Title).IsEqualTo("Test Conversation");
        await Assert.That(capturedMetadata.ModelId).IsEqualTo(TestModelId);
        await Assert.That(capturedMetadata.Tags).Contains("test");
    }

    [Test]
    public async Task CreateAsync_StoreReturnsError_PropagatesError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockStore.Setup(s => s.CreateConversationAsync(It.IsAny<ConversationMetadata?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Store.Error", "Database connection failed"));

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        var options = new ChatSessionOptions
        {
            ModelId = TestModelId,
            PersistConversation = true
        };

        // Act
        var result = await factory.CreateAsync(options);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("Store.Error");
    }

    [Test]
    public async Task ResumeAsync_WithoutStore_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, store: null);

        // Act
        var result = await factory.ResumeAsync("conv-123");

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(ChatErrorCodes.PersistenceNotConfigured);
    }

    [Test]
    public async Task ResumeAsync_ConversationNotFound_ReturnsError()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        mockStore.Setup(s => s.GetConversationWithMessagesAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.NotFound("Conversation.NotFound", "Conversation not found"));

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        // Act
        var result = await factory.ResumeAsync("non-existent");

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("Conversation.NotFound");
    }

    [Test]
    public async Task ResumeAsync_ConversationFound_LoadsMessagesAndReturnsSession()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        var conversationWithMessages = new ConversationWithMessages
        {
            Conversation = new Entities.Conversation
            {
                Id = "conv-789",
                Metadata = new ConversationMetadata { ModelId = TestModelId },
                TotalTokenUsage = new TokenUsage { InputTokens = 100, OutputTokens = 50, TotalTokens = 150 },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Messages =
            [
                new ConversationMessage
                {
                    Id = "msg-1",
                    ConversationId = "conv-789",
                    Role = Bedrock.Enums.ConversationRole.User,
                    Message = new Message
                    {
                        Role = Bedrock.Enums.ConversationRole.User,
                        Content = [new ContentBlock { Text = "Hello" }]
                    }
                }
            ]
        };

        mockStore.Setup(s => s.GetConversationWithMessagesAsync("conv-789", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversationWithMessages);

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        // Act
        var result = await factory.ResumeAsync("conv-789");

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ConversationId).IsEqualTo("conv-789");
        await Assert.That(result.Value.TotalTokenUsage.InputTokens).IsEqualTo(100);
        await Assert.That(result.Value.TotalTokenUsage.OutputTokens).IsEqualTo(50);
    }

    [Test]
    public async Task ResumeAsync_WithOptions_UsesProvidedOptions()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        var conversationWithMessages = new ConversationWithMessages
        {
            Conversation = new Entities.Conversation
            {
                Id = "conv-abc",
                Metadata = new ConversationMetadata { ModelId = "old-model" },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            Messages = []
        };

        mockStore.Setup(s => s.GetConversationWithMessagesAsync("conv-abc", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversationWithMessages);

        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        var overrideOptions = new ChatSessionOptions
        {
            ModelId = "new-model",
            MaxTokens = 2048,
            Temperature = 0.5f
        };

        // Act
        var result = await factory.ResumeAsync("conv-abc", overrideOptions);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ConversationId).IsEqualTo("conv-abc");
    }

    [Test]
    public async Task Constructor_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        var mockAdapter = new Mock<IMcpToolAdapter>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            _ = new ChatSessionFactory(null!, mockAdapter.Object);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task Constructor_NullAdapter_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            _ = new ChatSessionFactory(mockClient.Object, null!);
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task CreateAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await factory.CreateAsync(null!));
    }

    [Test]
    public async Task ResumeAsync_NullConversationId_ThrowsArgumentNullException()
    {
        // Arrange
        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();
        var factory = new ChatSessionFactory(mockClient.Object, mockAdapter.Object, mockStore.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await factory.ResumeAsync(null!));
    }
}
