using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;
using TUnit.Assertions;
using TUnit.Core;

namespace Goa.Clients.Sqs.Tests;

[ClassDataSource<SqsTestFixture>(Shared = SharedType.PerAssembly)]
public class SqsClientIntegrationTests
{
    private readonly SqsTestFixture _fixture;

    public SqsClientIntegrationTests(SqsTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task SendMessageAsync_WithValidMessage_ShouldSucceed()
    {
        // Arrange
        var queueUrl = await _fixture.CreateTestQueueAsync();
        var request = new SendMessageBuilder()
            .WithQueueUrl(queueUrl)
            .WithMessageBody("Hello, SQS!")
            .Build();

        // Act
        var result = await _fixture.SqsClient.SendMessageAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.MessageId).IsNotNull();
        await Assert.That(result.Value.MD5OfMessageBody).IsNotNull();
    }

    [Test]
    public async Task ReceiveMessageAsync_AfterSendingMessage_ShouldReceiveMessage()
    {
        // Arrange - Send a message first
        var queueUrl = await _fixture.CreateTestQueueAsync();
        var sendRequest = new SendMessageBuilder()
            .WithQueueUrl(queueUrl)
            .WithMessageBody("Test message for receive")
            .AddStringAttribute("TestAttribute", "TestValue")
            .Build();

        await _fixture.SqsClient.SendMessageAsync(sendRequest);

        var receiveRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            MessageAttributeNames = ["All"]
        };

        // Act
        var result = await _fixture.SqsClient.ReceiveMessageAsync(receiveRequest);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Messages).HasCount().GreaterThanOrEqualTo(1);

        var message = result.Value.Messages.First();
        await Assert.That(message.Body).IsEqualTo("Test message for receive");
        await Assert.That(message.MessageId).IsNotNull();
        await Assert.That(message.ReceiptHandle).IsNotNull();
    }

    [Test]
    public async Task DeleteMessageAsync_WithValidReceiptHandle_ShouldSucceed()
    {
        // Arrange - Send and receive a message first
        var queueUrl = await _fixture.CreateTestQueueAsync();
        var sendRequest = new SendMessageBuilder()
            .WithQueueUrl(queueUrl)
            .WithMessageBody("Message to delete")
            .Build();

        await _fixture.SqsClient.SendMessageAsync(sendRequest);

        var receiveRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1
        };

        var receiveResult = await _fixture.SqsClient.ReceiveMessageAsync(receiveRequest);
        var message = receiveResult.Value.Messages.First();

        var deleteRequest = new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = message.ReceiptHandle!
        };

        // Act
        var result = await _fixture.SqsClient.DeleteMessageAsync(deleteRequest);

        // Assert
        await Assert.That(result.IsError).IsFalse();
    }

    [Test]
    public async Task SendMessageBuilder_WithAttributes_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var queueUrl = await _fixture.CreateTestQueueAsync();
        var request = new SendMessageBuilder()
            .WithQueueUrl(queueUrl)
            .WithMessageBody("Test message")
            .WithDelaySeconds(30)
            .AddStringAttribute("attr1", "value1")
            .AddMessageAttribute("attr2", new MessageAttributeValue
            {
                DataType = "Number",
                StringValue = "123"
            })
            .Build();

        // Assert
        await Assert.That(request.QueueUrl).IsEqualTo(queueUrl);
        await Assert.That(request.MessageBody).IsEqualTo("Test message");
        await Assert.That(request.DelaySeconds).IsEqualTo(30);
        await Assert.That(request.MessageAttributes).HasCount().EqualTo(2);
        await Assert.That(request.MessageAttributes!["attr1"].StringValue).IsEqualTo("value1");
        await Assert.That(request.MessageAttributes["attr2"].DataType).IsEqualTo("Number");
    }
}
