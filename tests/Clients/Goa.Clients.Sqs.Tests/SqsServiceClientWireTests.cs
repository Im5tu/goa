using System.Net;
using System.Text.Json;
using ErrorOr;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Goa.Clients.Sqs.Tests;

/// <summary>
/// Wire-format unit tests: a real SqsServiceClient talks to a fake HTTP handler, so these do not require LocalStack.
/// </summary>
public class SqsServiceClientWireTests
{
    private const string QueueUrl = "http://localhost:4566/000000000000/test-queue";

    [Test]
    public async Task Configuration_DefaultHttpTimeout_IsTenSeconds()
    {
        await Assert.That(new SqsServiceClientConfiguration().HttpTimeout).IsEqualTo(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task AddSqs_HttpClientHasNoClientWideTimeout()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSqs(config => config.Region = "us-east-1");

        await using var provider = services.BuildServiceProvider();
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(FakeSqsClientFactory.HttpClientName);

        // Timeouts are applied per request so that long-polling receives can outlast HttpTimeout.
        await Assert.That(httpClient.Timeout).IsEqualTo(Timeout.InfiniteTimeSpan);
    }

    [Test]
    public async Task ReceiveMessageAsync_LongPollLongerThanHttpTimeout_Succeeds()
    {
        var handler = new FakeSqsHttpHandler("""{"Messages":[]}""", responseDelay: TimeSpan.FromMilliseconds(600));
        await using var provider = FakeSqsClientFactory.Create(handler, out var client, config => config.HttpTimeout = TimeSpan.FromMilliseconds(200));

        var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = QueueUrl, WaitTimeSeconds = 1 });

        await Assert.That(result.IsError).IsFalse();
    }

    [Test]
    public async Task ReceiveMessageAsync_SlowerThanWaitTimePlusHttpTimeout_ReturnsFailure()
    {
        var handler = new FakeSqsHttpHandler("""{"Messages":[]}""", responseDelay: TimeSpan.FromSeconds(5));
        await using var provider = FakeSqsClientFactory.Create(handler, out var client, config => config.HttpTimeout = TimeSpan.FromMilliseconds(200));

        var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = QueueUrl, WaitTimeSeconds = 0 });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("SQS.ReceiveMessage.Failed");
    }

    [Test]
    public async Task SendMessageAsync_SlowerThanHttpTimeout_ReturnsFailure()
    {
        var handler = new FakeSqsHttpHandler("""{"MessageId":"m1"}""", responseDelay: TimeSpan.FromSeconds(5));
        await using var provider = FakeSqsClientFactory.Create(handler, out var client, config => config.HttpTimeout = TimeSpan.FromMilliseconds(200));

        var result = await client.SendMessageAsync(new Operations.SendMessage.SendMessageRequest { QueueUrl = QueueUrl, MessageBody = "hi" });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("SQS.SendMessage.Failed");
    }

    [Test]
    [Arguments("{}")]
    [Arguments("")]
    public async Task ChangeMessageVisibilityAsync_SendsTargetAndBody_AndSucceeds(string responseBody)
    {
        var handler = new FakeSqsHttpHandler(responseBody);
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = "receipt-1",
            VisibilityTimeout = 120
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(handler.Requests).Count().IsEqualTo(1);
        await Assert.That(handler.Requests[0].Target).IsEqualTo("AmazonSQS.ChangeMessageVisibility");

        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        await Assert.That(body.RootElement.GetProperty("QueueUrl").GetString()).IsEqualTo(QueueUrl);
        await Assert.That(body.RootElement.GetProperty("ReceiptHandle").GetString()).IsEqualTo("receipt-1");
        await Assert.That(body.RootElement.GetProperty("VisibilityTimeout").GetInt32()).IsEqualTo(120);
    }

    [Test]
    public async Task ChangeMessageVisibilityAsync_WhenServiceReturnsError_ReturnsFailure()
    {
        var handler = new FakeSqsHttpHandler(
            """{"__type":"com.amazonaws.sqs#ReceiptHandleIsInvalid","message":"The receipt handle is invalid."}""",
            HttpStatusCode.BadRequest);
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = "receipt-1",
            VisibilityTimeout = 0
        });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Failure);
    }

    [Test]
    public async Task ChangeMessageVisibilityBatchAsync_SendsTargetAndParsesResults()
    {
        var handler = new FakeSqsHttpHandler(
            """{"Successful":[{"Id":"a"}],"Failed":[{"Id":"b","Code":"ReceiptHandleIsInvalid","Message":"bad","SenderFault":true}]}""");
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = QueueUrl,
            Entries =
            [
                new ChangeMessageVisibilityBatchRequestEntry { Id = "a", ReceiptHandle = "receipt-a", VisibilityTimeout = 0 },
                new ChangeMessageVisibilityBatchRequestEntry { Id = "b", ReceiptHandle = "receipt-b", VisibilityTimeout = 30 }
            ]
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(handler.Requests[0].Target).IsEqualTo("AmazonSQS.ChangeMessageVisibilityBatch");
        await Assert.That(result.Value.Successful!.Single().Id).IsEqualTo("a");
        await Assert.That(result.Value.HasFailures).IsTrue();
        await Assert.That(result.Value.Failed!.Single().Code).IsEqualTo("ReceiptHandleIsInvalid");

        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        var entries = body.RootElement.GetProperty("Entries");
        await Assert.That(entries.GetArrayLength()).IsEqualTo(2);
        await Assert.That(entries[1].GetProperty("ReceiptHandle").GetString()).IsEqualTo("receipt-b");
        await Assert.That(entries[1].GetProperty("VisibilityTimeout").GetInt32()).IsEqualTo(30);
    }

    [Test]
    public async Task ReceiveMessageAsync_SerializesMessageSystemAttributeNames_AndParsesAttributes()
    {
        var handler = new FakeSqsHttpHandler(
            """{"Messages":[{"MessageId":"m1","ReceiptHandle":"r1","Body":"hi","Attributes":{"ApproximateReceiveCount":"3","MessageGroupId":"g1"}}]}""");
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = QueueUrl,
            MessageSystemAttributeNames = ["ApproximateReceiveCount", "MessageGroupId"]
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(handler.Requests[0].Target).IsEqualTo("AmazonSQS.ReceiveMessage");
        await Assert.That(result.Value.Messages.Single().Attributes!["MessageGroupId"]).IsEqualTo("g1");

        using var body = JsonDocument.Parse(handler.Requests[0].Body);
        var names = body.RootElement.GetProperty("MessageSystemAttributeNames");
        await Assert.That(names.GetArrayLength()).IsEqualTo(2);
        await Assert.That(names[0].GetString()).IsEqualTo("ApproximateReceiveCount");
    }

    [Test]
    [Arguments(-1)]
    [Arguments(43201)]
    public async Task ChangeMessageVisibilityAsync_WithOutOfRangeTimeout_ReturnsValidationErrorWithoutSending(int visibilityTimeout)
    {
        var handler = new FakeSqsHttpHandler();
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = "receipt-1",
            VisibilityTimeout = visibilityTimeout
        });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(handler.Requests).IsEmpty();
    }

    [Test]
    public async Task ChangeMessageVisibilityAsync_WithoutReceiptHandle_ReturnsValidationError()
    {
        var handler = new FakeSqsHttpHandler();
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var result = await client.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = QueueUrl,
            ReceiptHandle = " ",
            VisibilityTimeout = 10
        });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("ChangeMessageVisibilityRequest.ReceiptHandle");
        await Assert.That(handler.Requests).IsEmpty();
    }

    [Test]
    public async Task ChangeMessageVisibilityBatchAsync_WithInvalidEntries_ReturnsValidationErrors()
    {
        var handler = new FakeSqsHttpHandler();
        await using var provider = FakeSqsClientFactory.Create(handler, out var client);

        var empty = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest { QueueUrl = QueueUrl, Entries = [] });
        var tooMany = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = QueueUrl,
            Entries = Enumerable.Range(0, 11)
                .Select(i => new ChangeMessageVisibilityBatchRequestEntry { Id = i.ToString(), ReceiptHandle = "r", VisibilityTimeout = 0 })
                .ToList()
        });
        var duplicate = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = QueueUrl,
            Entries =
            [
                new ChangeMessageVisibilityBatchRequestEntry { Id = "a", ReceiptHandle = "r1", VisibilityTimeout = 0 },
                new ChangeMessageVisibilityBatchRequestEntry { Id = "a", ReceiptHandle = "r2", VisibilityTimeout = 0 }
            ]
        });
        var outOfRange = await client.ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest
        {
            QueueUrl = QueueUrl,
            Entries = [new ChangeMessageVisibilityBatchRequestEntry { Id = "a", ReceiptHandle = "r1", VisibilityTimeout = 43201 }]
        });

        await Assert.That(empty.FirstError.Code).IsEqualTo("ChangeMessageVisibilityBatchRequest.Entries");
        await Assert.That(tooMany.FirstError.Code).IsEqualTo("ChangeMessageVisibilityBatchRequest.Entries");
        await Assert.That(duplicate.FirstError.Code).IsEqualTo("ChangeMessageVisibilityBatchRequestEntry.Id");
        await Assert.That(outOfRange.FirstError.Code).IsEqualTo("ChangeMessageVisibilityBatchRequestEntry.VisibilityTimeout");
        await Assert.That(handler.Requests).IsEmpty();
    }
}
