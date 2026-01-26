using Goa.Clients.Bedrock.Conversation.Chat;
using Goa.Clients.Bedrock.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Goa.Clients.Bedrock.Conversation.Tests;

public class ServiceExtensionsTests
{
    [Test]
    public async Task AddBedrockChatSession_RegistersChatSessionFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);

        // Act
        services.AddBedrockChatSession();

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IChatSessionFactory>();

        await Assert.That(factory).IsNotNull();
        await Assert.That(factory).IsTypeOf<ChatSessionFactory>();
    }

    [Test]
    public async Task AddBedrockChatSession_WithConversationStore_InjectsStore()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();
        var mockStore = new Mock<IConversationStore>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);
        services.AddSingleton(mockStore.Object);

        // Act
        services.AddBedrockChatSession();

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IChatSessionFactory>();

        await Assert.That(factory).IsNotNull();
    }

    [Test]
    public async Task AddBedrockChatSession_WithoutConversationStore_FactoryStillResolves()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);
        // Intentionally not registering IConversationStore

        // Act
        services.AddBedrockChatSession();

        // Assert
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IChatSessionFactory>();

        await Assert.That(factory).IsNotNull();
    }

    [Test]
    public async Task AddBedrockChatSession_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);

        // Act
        services.AddBedrockChatSession();

        // Assert
        var provider = services.BuildServiceProvider();
        var factory1 = provider.GetRequiredService<IChatSessionFactory>();
        var factory2 = provider.GetRequiredService<IChatSessionFactory>();

        await Assert.That(factory1).IsSameReferenceAs(factory2);
    }

    [Test]
    public async Task AddBedrockChatSession_CalledMultipleTimes_DoesNotReplaceExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);

        // Act
        services.AddBedrockChatSession();
        services.AddBedrockChatSession();
        services.AddBedrockChatSession();

        // Assert
        var factoryDescriptors = services.Where(sd => sd.ServiceType == typeof(IChatSessionFactory)).ToList();
        await Assert.That(factoryDescriptors).Count().IsEqualTo(1);
    }

    [Test]
    public async Task AddBedrockChatSession_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            services!.AddBedrockChatSession();
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task AddBedrockChatSession_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        var mockAdapter = new Mock<IMcpToolAdapter>();

        services.AddSingleton(mockClient.Object);
        services.AddSingleton(mockAdapter.Object);

        // Act
        var result = services.AddBedrockChatSession();

        // Assert
        await Assert.That(result).IsSameReferenceAs(services);
    }

    [Test]
    public async Task AddBedrockChatSession_MissingBedrockClient_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockAdapter = new Mock<IMcpToolAdapter>();
        services.AddSingleton(mockAdapter.Object);
        // Not registering IBedrockClient

        services.AddBedrockChatSession();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            _ = provider.GetRequiredService<IChatSessionFactory>();
            await Task.CompletedTask;
        });
    }

    [Test]
    public async Task AddBedrockChatSession_MissingToolAdapter_ThrowsOnResolve()
    {
        // Arrange
        var services = new ServiceCollection();

        var mockClient = new Mock<IBedrockClient>();
        services.AddSingleton(mockClient.Object);
        // Not registering IMcpToolAdapter

        services.AddBedrockChatSession();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            _ = provider.GetRequiredService<IChatSessionFactory>();
            await Task.CompletedTask;
        });
    }
}
