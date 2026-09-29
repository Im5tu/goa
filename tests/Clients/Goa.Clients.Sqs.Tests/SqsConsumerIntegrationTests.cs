using Goa.Clients.Sqs.Consumer;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;

namespace Goa.Clients.Sqs.Tests;

[ClassDataSource<SqsTestFixture>(Shared = SharedType.PerAssembly)]
public class SqsConsumerIntegrationTests
{
    private readonly SqsTestFixture _fixture;

    public SqsConsumerIntegrationTests(SqsTestFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task SendAsync(string queueUrl, string body)
    {
        var result = await _fixture.SqsClient.SendMessageAsync(new SendMessageBuilder()
            .WithQueueUrl(queueUrl)
            .WithMessageBody(body)
            .AddStringAttribute("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01")
            .Build());

        await Assert.That(result.IsError).IsFalse();
    }

    private async Task<List<SqsReceivedMessage>> ConsumeAsync(string queueUrl, int count)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var options = new SqsConsumerOptions
        {
            QueueUrl = queueUrl,
            WaitTime = TimeSpan.FromSeconds(1),
            VisibilityTimeout = TimeSpan.FromSeconds(30),
            MessageAttributeNames = ["traceparent"],
            MessageSystemAttributeNames = ["ApproximateReceiveCount"]
        };

        var received = new List<SqsReceivedMessage>();
        await foreach (var message in _fixture.SqsClient.ConsumeAsync(options, cancellationToken: cts.Token))
        {
            received.Add(message);
            if (received.Count == count)
                break;
        }

        return received;
    }

    private async Task<int> CountVisibleMessagesAsync(string queueUrl)
    {
        var result = await _fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 1
        });

        await Assert.That(result.IsError).IsFalse();
        return result.Value.Messages.Count;
    }

    [Test]
    public async Task ConsumeAsync_CompleteAsync_DeletesMessages()
    {
        var queueUrl = await _fixture.CreateTestQueueAsync();
        await SendAsync(queueUrl, "first");
        await SendAsync(queueUrl, "second");

        var received = await ConsumeAsync(queueUrl, 2);

        await Assert.That(received.Select(m => m.Body!)).IsEquivalentTo(["first", "second"], TUnit.Assertions.Enums.CollectionOrdering.Any);
        await Assert.That(received[0].ApproximateReceiveCount).IsEqualTo(1);
        await Assert.That(received[0].Message.MessageAttributes!["traceparent"].StringValue)
            .IsEqualTo("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");

        foreach (var message in received)
        {
            var extended = await message.ExtendVisibilityAsync(TimeSpan.FromSeconds(60));
            await Assert.That(extended.IsError).IsFalse();

            var completed = await message.CompleteAsync();
            await Assert.That(completed.IsError).IsFalse();
        }

        await Assert.That(await CountVisibleMessagesAsync(queueUrl)).IsEqualTo(0);
    }

    [Test]
    public async Task ConsumeAsync_AbandonAsyncWithZeroDelay_RedeliversMessage()
    {
        var queueUrl = await _fixture.CreateTestQueueAsync();
        await SendAsync(queueUrl, "retry me");

        var first = (await ConsumeAsync(queueUrl, 1)).Single();
        var abandoned = await first.AbandonAsync(TimeSpan.Zero);
        await Assert.That(abandoned.IsError).IsFalse();

        var second = (await ConsumeAsync(queueUrl, 1)).Single();

        await Assert.That(second.MessageId).IsEqualTo(first.MessageId);
        await Assert.That(second.ApproximateReceiveCount).IsEqualTo(2);

        var completed = await second.CompleteAsync();
        await Assert.That(completed.IsError).IsFalse();
    }

    [Test]
    public async Task ChangeMessageVisibilityAsync_ToZero_MakesMessageVisibleAgain()
    {
        var queueUrl = await _fixture.CreateTestQueueAsync();
        await SendAsync(queueUrl, "visibility");

        var received = await _fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            VisibilityTimeout = 300,
            WaitTimeSeconds = 1
        });
        var receiptHandle = received.Value.Messages.Single().ReceiptHandle!;
        await Assert.That(await CountVisibleMessagesAsync(queueUrl)).IsEqualTo(0);

        var result = await _fixture.SqsClient.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = receiptHandle,
            VisibilityTimeout = 0
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(await CountVisibleMessagesAsync(queueUrl)).IsEqualTo(1);
    }

    [Test]
    public async Task ChangeMessageVisibilityBatchAsync_ReportsSuccessfulAndFailedEntries()
    {
        var queueUrl = await _fixture.CreateTestQueueAsync();
        await SendAsync(queueUrl, "batch");

        var received = await _fixture.SqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            VisibilityTimeout = 300,
            WaitTimeSeconds = 1
        });
        var receiptHandle = received.Value.Messages.Single().ReceiptHandle!;

        var result = await _fixture.SqsClient.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = queueUrl,
            Entries =
            [
                new ChangeMessageVisibilityBatchRequestEntry { Id = "valid", ReceiptHandle = receiptHandle, VisibilityTimeout = 0 },
                new ChangeMessageVisibilityBatchRequestEntry { Id = "invalid", ReceiptHandle = "not-a-receipt-handle", VisibilityTimeout = 0 }
            ]
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Successful!.Single().Id).IsEqualTo("valid");
        await Assert.That(result.Value.Failed!.Single().Id).IsEqualTo("invalid");
        await Assert.That(await CountVisibleMessagesAsync(queueUrl)).IsEqualTo(1);
    }
}
