using ErrorOr;
using Goa.Clients.Sqs.Consumer;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Moq;

namespace Goa.Clients.Sqs.Tests;

/// <summary>
/// Unit tests for the <see cref="SqsReceivedMessage"/> ack handle. These do not require LocalStack.
/// </summary>
public class SqsReceivedMessageTests
{
    private const string QueueUrl = "https://sqs.us-east-1.amazonaws.com/000000000000/test-queue";

    private static SqsMessage CreateMessage(Dictionary<string, string>? attributes = null) => new()
    {
        MessageId = "message-1",
        ReceiptHandle = "receipt-1",
        Body = "body",
        Attributes = attributes
    };

    [Test]
    public async Task CompleteAsync_DeletesMessageWithReceiptHandle()
    {
        var mock = new Mock<ISqsClient>();
        DeleteMessageRequest? captured = null;
        mock.Setup(c => c.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteMessageRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new DeleteMessageResponse());

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).CompleteAsync();

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(captured!.QueueUrl).IsEqualTo(QueueUrl);
        await Assert.That(captured.ReceiptHandle).IsEqualTo("receipt-1");
    }

    [Test]
    public async Task CompleteAsync_WhenDeleteFails_ReturnsError()
    {
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Goa.SQS.ReceiptHandleIsInvalid", "invalid"));

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).CompleteAsync();

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("Goa.SQS.ReceiptHandleIsInvalid");
    }

    [Test]
    public async Task AbandonAsync_WithoutDelay_SendsNoRequest()
    {
        var mock = new Mock<ISqsClient>(MockBehavior.Strict);

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).AbandonAsync();

        await Assert.That(result.IsError).IsFalse();
        mock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task AbandonAsync_WithDelay_ChangesVisibilityTimeout()
    {
        var mock = new Mock<ISqsClient>();
        ChangeMessageVisibilityRequest? captured = null;
        mock.Setup(c => c.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeMessageVisibilityRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).AbandonAsync(TimeSpan.Zero);

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(captured!.QueueUrl).IsEqualTo(QueueUrl);
        await Assert.That(captured.ReceiptHandle).IsEqualTo("receipt-1");
        await Assert.That(captured.VisibilityTimeout).IsEqualTo(0);
        mock.Verify(c => c.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ExtendVisibilityAsync_RoundsUpToWholeSeconds()
    {
        var mock = new Mock<ISqsClient>();
        ChangeMessageVisibilityRequest? captured = null;
        mock.Setup(c => c.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeMessageVisibilityRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new ChangeMessageVisibilityResponse());

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).ExtendVisibilityAsync(TimeSpan.FromSeconds(90.2));

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(captured!.VisibilityTimeout).IsEqualTo(91);
    }

    [Test]
    public async Task ExtendVisibilityAsync_WhenChangeFails_ReturnsError()
    {
        var mock = new Mock<ISqsClient>();
        mock.Setup(c => c.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Goa.SQS.MessageNotInflight", "not in flight"));

        var result = await new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage()).ExtendVisibilityAsync(TimeSpan.FromSeconds(30));

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("Goa.SQS.MessageNotInflight");
    }

    [Test]
    [Arguments(-1d)]
    [Arguments(43201d)]
    public async Task ExtendVisibilityAsync_WithOutOfRangeTimeout_ReturnsValidationErrorWithoutSending(double seconds)
    {
        var mock = new Mock<ISqsClient>(MockBehavior.Strict);

        var handle = new SqsReceivedMessage(mock.Object, QueueUrl, CreateMessage());
        var extend = await handle.ExtendVisibilityAsync(TimeSpan.FromSeconds(seconds));
        var abandon = await handle.AbandonAsync(TimeSpan.FromSeconds(seconds));

        await Assert.That(extend.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(abandon.FirstError.Type).IsEqualTo(ErrorType.Validation);
        mock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Properties_ExposeMessageAndSystemAttributes()
    {
        var handle = new SqsReceivedMessage(new Mock<ISqsClient>().Object, QueueUrl, CreateMessage(new Dictionary<string, string>
        {
            ["ApproximateReceiveCount"] = "4",
            ["MessageGroupId"] = "group-1"
        }));

        await Assert.That(handle.MessageId).IsEqualTo("message-1");
        await Assert.That(handle.Body).IsEqualTo("body");
        await Assert.That(handle.ApproximateReceiveCount).IsEqualTo(4);
        await Assert.That(handle.MessageGroupId).IsEqualTo("group-1");
    }

    [Test]
    public async Task Properties_WhenSystemAttributesNotRequested_AreNull()
    {
        var handle = new SqsReceivedMessage(new Mock<ISqsClient>().Object, QueueUrl, CreateMessage());

        await Assert.That(handle.ApproximateReceiveCount).IsNull();
        await Assert.That(handle.MessageGroupId).IsNull();
    }
}
