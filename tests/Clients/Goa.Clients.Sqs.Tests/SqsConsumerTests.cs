using ErrorOr;
using Goa.Clients.Sqs.Consumer;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Moq;

namespace Goa.Clients.Sqs.Tests;

/// <summary>
/// Unit tests for <see cref="SqsConsumerExtensions.ConsumeAsync"/>. These do not require LocalStack.
/// </summary>
public class SqsConsumerTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/000000000000/test-queue";

    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    private static SqsConsumerOptions CreateOptions() => new()
    {
        QueueUrl = QueueUrl,
        InitialRetryDelay = TimeSpan.FromMilliseconds(1),
        MaxRetryDelay = TimeSpan.FromMilliseconds(2)
    };

    private static ErrorOr<ReceiveMessageResponse> Batch(params string[] ids) => new ReceiveMessageResponse
    {
        Messages = ids.Select(id => new SqsMessage { MessageId = id, ReceiptHandle = $"receipt-{id}", Body = $"body-{id}" }).ToList()
    };

    // Mimics SqsServiceClient, which reports a cancelled request as a failure instead of throwing.
    private static readonly ErrorOr<ReceiveMessageResponse> CancelledFailure = Error.Failure("SQS.ReceiveMessage.Failed", "Failed to receive messages");

    [Test]
    public async Task ConsumeAsync_YieldsMessagesAcrossReceives_UntilCancelled()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Batch("1", "2"))
            .ReturnsAsync(Batch())
            .ReturnsAsync(Batch("3"))
            .ReturnsAsync(() =>
            {
                cts.Cancel();
                return CancelledFailure;
            });

        var received = new List<SqsReceivedMessage>();
        await foreach (var message in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: cts.Token))
        {
            received.Add(message);
        }

        await Assert.That(received.Select(m => m.MessageId!)).IsEquivalentTo(["1", "2", "3"]);
        await Assert.That(received[0].QueueUrl).IsEqualTo(QueueUrl);
        await Assert.That(received[0].Message.ReceiptHandle).IsEqualTo("receipt-1");
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Test]
    public async Task ConsumeAsync_MapsOptionsToReceiveRequest()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        ReceiveMessageRequest? captured = null;
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReceiveMessageRequest, CancellationToken>((request, _) =>
            {
                captured = request;
                cts.Cancel();
            })
            .ReturnsAsync(CancelledFailure);

        var options = new SqsConsumerOptions
        {
            QueueUrl = QueueUrl,
            MaxNumberOfMessages = 5,
            VisibilityTimeout = TimeSpan.FromSeconds(59.5),
            MessageAttributeNames = ["traceparent"],
            MessageSystemAttributeNames = ["MessageGroupId", "ApproximateReceiveCount"]
        };

        await foreach (var _ in mock.Object.ConsumeAsync(options, cancellationToken: cts.Token))
        {
        }

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.QueueUrl).IsEqualTo(QueueUrl);
        await Assert.That(captured.MaxNumberOfMessages).IsEqualTo(5);
        await Assert.That(captured.WaitTimeSeconds).IsEqualTo(20);
        await Assert.That(captured.VisibilityTimeout).IsEqualTo(60);
        await Assert.That(captured.MessageAttributeNames).IsEquivalentTo(["traceparent"]);
        await Assert.That(captured.MessageSystemAttributeNames).IsEquivalentTo(["MessageGroupId", "ApproximateReceiveCount"]);
    }

    [Test]
    public async Task ConsumeAsync_WithDefaultOptions_LongPollsTenMessagesAndUsesQueueVisibilityTimeout()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        ReceiveMessageRequest? captured = null;
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ReceiveMessageRequest, CancellationToken>((request, _) =>
            {
                captured = request;
                cts.Cancel();
            })
            .ReturnsAsync(CancelledFailure);

        await foreach (var _ in mock.Object.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl }, cancellationToken: cts.Token))
        {
        }

        await Assert.That(captured!.MaxNumberOfMessages).IsEqualTo(10);
        await Assert.That(captured.WaitTimeSeconds).IsEqualTo(20);
        await Assert.That(captured.VisibilityTimeout).IsNull();
        await Assert.That(captured.MessageAttributeNames).IsNull();
        await Assert.That(captured.MessageSystemAttributeNames).IsNull();
    }

    [Test]
    public async Task ConsumeAsync_WhenCancelledDuringReceive_EndsWithoutRetryingOrThrowing()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                cts.Cancel();
                return CancelledFailure;
            });

        // A long retry delay would hang the test if the cancelled receive were treated as a transient failure.
        var options = CreateOptions();
        options.InitialRetryDelay = TimeSpan.FromMinutes(10);
        options.MaxRetryDelay = TimeSpan.FromMinutes(10);

        var count = 0;
        await foreach (var _ in mock.Object.ConsumeAsync(options, cancellationToken: cts.Token))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConsumeAsync_WhenClientThrowsOperationCanceled_EndsWithoutThrowing()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Returns<ReceiveMessageRequest, CancellationToken>((_, token) =>
            {
                cts.Cancel();
                return Task.FromCanceled<ErrorOr<ReceiveMessageResponse>>(token);
            });

        var count = 0;
        await foreach (var _ in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: cts.Token))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task ConsumeAsync_WhenCancelledDuringBackoff_EndsWithoutThrowing()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                cts.CancelAfter(TimeSpan.FromMilliseconds(50));
                return Error.Failure("Goa.SQS.ServiceUnavailable", "Service unavailable");
            });

        var options = CreateOptions();
        options.InitialRetryDelay = TimeSpan.FromMinutes(10);
        options.MaxRetryDelay = TimeSpan.FromMinutes(10);

        var count = 0;
        await foreach (var _ in mock.Object.ConsumeAsync(options, cancellationToken: cts.Token))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConsumeAsync_RetriesTransientErrors_AndKeepsYielding()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Goa.SQS.RequestThrottled", "Throttled"))
            .ReturnsAsync(Error.Failure("SQS.ReceiveMessage.Failed", "Network error"))
            .ReturnsAsync(Batch("1"));

        SqsReceivedMessage? first = null;
        await foreach (var message in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: cts.Token))
        {
            first = message;
            break;
        }

        await Assert.That(first?.MessageId).IsEqualTo("1");
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Test]
    public async Task ConsumeAsync_WhenClientThrows_RetriesInsteadOfFaulting()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.SetupSequence(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connection reset"))
            .ReturnsAsync(Batch("1"));

        SqsReceivedMessage? first = null;
        await foreach (var message in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: cts.Token))
        {
            first = message;
            break;
        }

        await Assert.That(first?.MessageId).IsEqualTo("1");
    }

    [Test]
    public async Task ConsumeAsync_WhenCancelledMidBatch_StopsYieldingRemainingMessages()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Batch("1", "2", "3"));

        var received = new List<string>();
        await foreach (var message in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: cts.Token))
        {
            received.Add(message.MessageId!);
            cts.Cancel();
        }

        await Assert.That(received).IsEquivalentTo(["1"]);
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ConsumeAsync_WithAlreadyCancelledToken_DoesNotReceive()
    {
        var mock = new Mock<ISqsClient>();

        var count = 0;
        await foreach (var _ in mock.Object.ConsumeAsync(CreateOptions(), cancellationToken: new CancellationToken(canceled: true)))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
        mock.Verify(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ConsumeAsync_WithCancellationViaWithCancellation_EndsWithoutThrowing()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
            .Returns<ReceiveMessageRequest, CancellationToken>(async (_, token) =>
            {
                // Simulate a long poll that only returns when cancelled.
                await Task.Delay(Timeout.Infinite, token).ContinueWith(_ => { }, TaskScheduler.Default);
                return CancelledFailure;
            });

        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var count = 0;
        await foreach (var _ in mock.Object.ConsumeAsync(CreateOptions()).WithCancellation(cts.Token))
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task ConsumeAsync_WithInvalidOptions_ThrowsBeforeEnumerating()
    {
        var client = new Mock<ISqsClient>().Object;

        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = " " })).Throws<ArgumentException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, MaxNumberOfMessages = 0 })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, MaxNumberOfMessages = 11 })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, WaitTime = TimeSpan.FromSeconds(21) })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, WaitTime = TimeSpan.FromSeconds(-1) })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, VisibilityTimeout = TimeSpan.FromHours(13) })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, InitialRetryDelay = TimeSpan.Zero })).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => client.ConsumeAsync(new SqsConsumerOptions { QueueUrl = QueueUrl, InitialRetryDelay = TimeSpan.FromSeconds(5), MaxRetryDelay = TimeSpan.FromSeconds(1) })).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    [Arguments(1, 500, 1000)]
    [Arguments(2, 1000, 2000)]
    [Arguments(3, 2000, 4000)]
    [Arguments(10, 15000, 30000)]
    [Arguments(1000, 15000, 30000)]
    public async Task GetRetryDelay_GrowsExponentiallyWithJitter_AndIsCapped(int consecutiveFailures, double minMs, double maxMs)
    {
        for (var i = 0; i < 20; i++)
        {
            var delay = SqsConsumerExtensions.GetRetryDelay(consecutiveFailures, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));

            await Assert.That(delay.TotalMilliseconds).IsGreaterThanOrEqualTo(minMs);
            await Assert.That(delay.TotalMilliseconds).IsLessThanOrEqualTo(maxMs);
        }
    }
}
