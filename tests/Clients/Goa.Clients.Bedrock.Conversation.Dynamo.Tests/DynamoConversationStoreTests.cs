using Goa.Clients.Bedrock.Conversation.Entities;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Conversation.Dynamo.Tests;

[ClassDataSource<ConversationStoreTestFixture>(Shared = SharedType.PerAssembly)]
public class DynamoConversationStoreTests
{
    private readonly ConversationStoreTestFixture _fixture;

    public DynamoConversationStoreTests(ConversationStoreTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task CreateConversationAsync_ShouldCreateConversation_WithoutMetadata()
    {
        var result = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Id).IsNotNull().And.IsNotEmpty();
        await Assert.That(result.Value.MessageCount).IsEqualTo(0);
        await Assert.That(result.Value.Metadata).IsNull();
    }

    [Test]
    public async Task CreateConversationAsync_ShouldCreateConversation_WithMetadata()
    {
        var metadata = new ConversationMetadata
        {
            Title = "Test Conversation",
            ModelId = "anthropic.claude-v3",
            Tags = ["tag1", "tag2"],
            CustomData = new Dictionary<string, string> { ["key1"] = "value1" }
        };

        var result = await _fixture.ConversationStore.CreateConversationAsync(metadata, CancellationToken.None);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Id).IsNotNull().And.IsNotEmpty();
        await Assert.That(result.Value.Metadata).IsNotNull();
        await Assert.That(result.Value.Metadata!.Title).IsEqualTo("Test Conversation");
        await Assert.That(result.Value.Metadata!.ModelId).IsEqualTo("anthropic.claude-v3");
        await Assert.That(result.Value.Metadata!.Tags).Contains("tag1").And.Contains("tag2");
        await Assert.That(result.Value.Metadata!.CustomData["key1"]).IsEqualTo("value1");
    }

    [Test]
    public async Task GetConversationAsync_ShouldReturnConversation_WhenExists()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var getResult = await _fixture.ConversationStore.GetConversationAsync(createResult.Value.Id, CancellationToken.None);

        await Assert.That(getResult.IsError).IsFalse();
        await Assert.That(getResult.Value.Id).IsEqualTo(createResult.Value.Id);
    }

    [Test]
    public async Task GetConversationAsync_ShouldReturnNotFound_WhenDoesNotExist()
    {
        var result = await _fixture.ConversationStore.GetConversationAsync("nonexistent", CancellationToken.None);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.NotFound);
    }

    [Test]
    public async Task AddMessageAsync_ShouldAddMessage_WithTextContent()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var message = new Message
        {
            Content = [new ContentBlock { Text = "Hello, how are you?" }]
        };

        var addResult = await _fixture.ConversationStore.AddMessageAsync(
            createResult.Value.Id,
            ConversationRole.User,
            message,
            null,
            CancellationToken.None);

        await Assert.That(addResult.IsError).IsFalse();
        await Assert.That(addResult.Value.ConversationId).IsEqualTo(createResult.Value.Id);
        await Assert.That(addResult.Value.SequenceNumber).IsEqualTo(1);
        await Assert.That(addResult.Value.Role).IsEqualTo(ConversationRole.User);
        await Assert.That(addResult.Value.Message.Content).Count().IsEqualTo(1);
        await Assert.That(addResult.Value.Message.Content[0].Text).IsEqualTo("Hello, how are you?");
    }

    [Test]
    public async Task AddMessageAsync_ShouldAddMessage_WithTokenUsage()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var message = new Message
        {
            Content = [new ContentBlock { Text = "Response text" }]
        };

        var tokenUsage = new TokenUsage
        {
            InputTokens = 10,
            OutputTokens = 20,
            TotalTokens = 30
        };

        var addResult = await _fixture.ConversationStore.AddMessageAsync(
            createResult.Value.Id,
            ConversationRole.Assistant,
            message,
            tokenUsage,
            CancellationToken.None);

        await Assert.That(addResult.IsError).IsFalse();
        await Assert.That(addResult.Value.TokenUsage).IsNotNull();
        await Assert.That(addResult.Value.TokenUsage!.InputTokens).IsEqualTo(10);
        await Assert.That(addResult.Value.TokenUsage!.OutputTokens).IsEqualTo(20);
        await Assert.That(addResult.Value.TokenUsage!.TotalTokens).IsEqualTo(30);
    }

    [Test]
    public async Task AddMessagesAsync_ShouldAddMultipleMessages()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var messages = new[]
        {
            (ConversationRole.User, new Message { Content = [new ContentBlock { Text = "User message" }] }, (TokenUsage?)null),
            (ConversationRole.Assistant, new Message { Content = [new ContentBlock { Text = "Assistant response" }] }, new TokenUsage { InputTokens = 5, OutputTokens = 10, TotalTokens = 15 })
        };

        var addResult = await _fixture.ConversationStore.AddMessagesAsync(
            createResult.Value.Id,
            messages,
            CancellationToken.None);

        await Assert.That(addResult.IsError).IsFalse();
        await Assert.That(addResult.Value).Count().IsEqualTo(2);
        await Assert.That(addResult.Value[0].SequenceNumber).IsEqualTo(1);
        await Assert.That(addResult.Value[1].SequenceNumber).IsEqualTo(2);
    }

    [Test]
    public async Task GetConversationWithMessagesAsync_ShouldReturnMessagesInOrder()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var messages = new[]
        {
            (ConversationRole.User, new Message { Content = [new ContentBlock { Text = "First" }] }, (TokenUsage?)null),
            (ConversationRole.Assistant, new Message { Content = [new ContentBlock { Text = "Second" }] }, (TokenUsage?)null),
            (ConversationRole.User, new Message { Content = [new ContentBlock { Text = "Third" }] }, (TokenUsage?)null)
        };

        await _fixture.ConversationStore.AddMessagesAsync(createResult.Value.Id, messages, CancellationToken.None);

        var getResult = await _fixture.ConversationStore.GetConversationWithMessagesAsync(
            createResult.Value.Id, 
            null, 
            null, 
            CancellationToken.None);

        await Assert.That(getResult.IsError).IsFalse();
        await Assert.That(getResult.Value.Messages).Count().IsEqualTo(3);
        await Assert.That(getResult.Value.Messages[0].Message.Content[0].Text).IsEqualTo("First");
        await Assert.That(getResult.Value.Messages[1].Message.Content[0].Text).IsEqualTo("Second");
        await Assert.That(getResult.Value.Messages[2].Message.Content[0].Text).IsEqualTo("Third");
    }

    [Test]
    public async Task GetConversationWithMessagesAsync_ShouldPaginate()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var messages = Enumerable.Range(1, 5)
            .Select(i => (ConversationRole.User, new Message { Content = [new ContentBlock { Text = $"Message {i}" }] }, (TokenUsage?)null))
            .ToArray();

        await _fixture.ConversationStore.AddMessagesAsync(createResult.Value.Id, messages, CancellationToken.None);

        var firstPage = await _fixture.ConversationStore.GetConversationWithMessagesAsync(
            createResult.Value.Id, 
            2, 
            null, 
            CancellationToken.None);

        await Assert.That(firstPage.IsError).IsFalse();
        await Assert.That(firstPage.Value.Messages).Count().IsEqualTo(2);
        await Assert.That(firstPage.Value.HasMoreMessages).IsTrue();
        await Assert.That(firstPage.Value.NextPaginationToken).IsNotNull();

        var secondPage = await _fixture.ConversationStore.GetConversationWithMessagesAsync(
            createResult.Value.Id, 
            2, 
            firstPage.Value.NextPaginationToken, 
            CancellationToken.None);

        await Assert.That(secondPage.IsError).IsFalse();
        await Assert.That(secondPage.Value.Messages).Count().IsEqualTo(2);
        await Assert.That(secondPage.Value.Messages[0].Message.Content[0].Text).IsEqualTo("Message 3");
    }

    [Test]
    public async Task UpdateConversationAsync_ShouldUpdateMetadata()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var newMetadata = new ConversationMetadata
        {
            Title = "Updated Title",
            Tags = ["updated-tag"]
        };

        var updateResult = await _fixture.ConversationStore.UpdateConversationAsync(
            createResult.Value.Id,
            newMetadata,
            CancellationToken.None);

        await Assert.That(updateResult.IsError).IsFalse();
        await Assert.That(updateResult.Value.Metadata).IsNotNull();
        await Assert.That(updateResult.Value.Metadata!.Title).IsEqualTo("Updated Title");
    }

    [Test]
    public async Task DeleteConversationAsync_ShouldDeleteConversationAndMessages()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var message = new Message { Content = [new ContentBlock { Text = "Test message" }] };
        await _fixture.ConversationStore.AddMessageAsync(createResult.Value.Id, ConversationRole.User, message, null, CancellationToken.None);

        var deleteResult = await _fixture.ConversationStore.DeleteConversationAsync(createResult.Value.Id, CancellationToken.None);

        await Assert.That(deleteResult.IsError).IsFalse();

        var getResult = await _fixture.ConversationStore.GetConversationAsync(createResult.Value.Id, CancellationToken.None);
        await Assert.That(getResult.IsError).IsTrue();
        await Assert.That(getResult.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.NotFound);
    }

    [Test]
    public async Task DeleteConversationAsync_ShouldReturnNotFound_WhenDoesNotExist()
    {
        var result = await _fixture.ConversationStore.DeleteConversationAsync("nonexistent", CancellationToken.None);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorOr.ErrorType.NotFound);
    }

    [Test]
    public async Task AddMessageAsync_ShouldUpdateConversationMessageCount()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var message = new Message { Content = [new ContentBlock { Text = "Test" }] };
        await _fixture.ConversationStore.AddMessageAsync(createResult.Value.Id, ConversationRole.User, message, null, CancellationToken.None);

        var getResult = await _fixture.ConversationStore.GetConversationAsync(createResult.Value.Id, CancellationToken.None);

        await Assert.That(getResult.IsError).IsFalse();
        await Assert.That(getResult.Value.MessageCount).IsEqualTo(1);
    }

    [Test]
    public async Task AddMessageAsync_ShouldAccumulateTotalTokenUsage()
    {
        var createResult = await _fixture.ConversationStore.CreateConversationAsync(null, CancellationToken.None);
        await Assert.That(createResult.IsError).IsFalse();

        var message1 = new Message { Content = [new ContentBlock { Text = "User" }] };
        var tokenUsage1 = new TokenUsage { InputTokens = 10, OutputTokens = 0, TotalTokens = 10 };
        await _fixture.ConversationStore.AddMessageAsync(createResult.Value.Id, ConversationRole.User, message1, tokenUsage1, CancellationToken.None);

        var message2 = new Message { Content = [new ContentBlock { Text = "Assistant" }] };
        var tokenUsage2 = new TokenUsage { InputTokens = 10, OutputTokens = 20, TotalTokens = 30 };
        await _fixture.ConversationStore.AddMessageAsync(createResult.Value.Id, ConversationRole.Assistant, message2, tokenUsage2, CancellationToken.None);

        var getResult = await _fixture.ConversationStore.GetConversationAsync(createResult.Value.Id, CancellationToken.None);

        await Assert.That(getResult.IsError).IsFalse();
        await Assert.That(getResult.Value.TotalTokenUsage).IsNotNull();
        await Assert.That(getResult.Value.TotalTokenUsage!.InputTokens).IsEqualTo(20);
        await Assert.That(getResult.Value.TotalTokenUsage!.OutputTokens).IsEqualTo(20);
        await Assert.That(getResult.Value.TotalTokenUsage!.TotalTokens).IsEqualTo(40);
    }
}