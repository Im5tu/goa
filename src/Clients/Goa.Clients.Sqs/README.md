# Goa.Clients.Sqs

SQS client for queue operations in high-performance AWS Lambda functions. This package provides a lightweight, AOT-ready SQS client optimized for minimal cold start times.

## Installation

```bash
dotnet add package Goa.Clients.Sqs
```

## Features

- Native AOT support for faster Lambda cold starts
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Support for standard and FIFO queues
- Batch operations for improved performance
- Dead letter queue support
- Long-polling `IAsyncEnumerable` consumer with per-message complete/abandon/extend handles
- Message visibility control (`ChangeMessageVisibility` and `ChangeMessageVisibilityBatch`)

## Usage

### Basic Setup

```csharp
using Goa.Clients.Sqs;
using Microsoft.Extensions.DependencyInjection;

// Register SQS client
services.AddSqs();
```

### Sending Messages

```csharp
using Goa.Clients.Sqs.Operations.SendMessage;
using System.Text.Json;

public class QueueService
{
    private readonly ISqsClient _sqs;
    
    public QueueService(ISqsClient sqs)
    {
        _sqs = sqs;
    }
    
    public async Task<bool> SendMessageAsync(string queueUrl, object message)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(message)
        };
            
        var result = await _sqs.SendMessageAsync(request);
        return !result.IsError;
    }
}
```

### Receiving Messages

```csharp
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Models;

public async Task<List<SqsMessage>> ReceiveMessagesAsync(string queueUrl)
{
    var request = new ReceiveMessageRequest
    {
        QueueUrl = queueUrl,
        MaxNumberOfMessages = 10,
        WaitTimeSeconds = 20,
        MessageSystemAttributeNames = ["ApproximateReceiveCount", "MessageGroupId"]
    };
    
    var result = await _sqs.ReceiveMessageAsync(request);
    return result.IsError ? new List<SqsMessage>() : result.Value.Messages ?? new List<SqsMessage>();
}
```

> The SQS client's `HttpTimeout` defaults to 30 seconds so that 20 second long polls complete. If you lower it,
> keep it above the `WaitTimeSeconds` you use.

### Consuming a Queue

`ConsumeAsync` long-polls a queue until its cancellation token is cancelled and yields each message with a handle
to settle it. It is designed for long-running workers (for example a `BackgroundService`):

- Cancellation ends the enumeration cleanly; no `OperationCanceledException` escapes.
- Failed receives (throttling, network errors, service errors) are logged and retried with exponential backoff and
  jitter (`InitialRetryDelay`, doubling up to `MaxRetryDelay`), so transient errors never fault the stream.
- Invalid options throw an `ArgumentException` when `ConsumeAsync` is called.
- Messages are never deleted automatically. A message that is not completed, including any message from a received
  batch that had not been yielded when enumeration stopped, is redelivered once its visibility timeout expires and
  moves to the dead-letter queue after the queue's `maxReceiveCount`.

```csharp
using Goa.Clients.Sqs.Consumer;

var options = new SqsConsumerOptions
{
    QueueUrl = queueUrl,
    MaxNumberOfMessages = 10,                      // 1-10, default 10
    WaitTime = TimeSpan.FromSeconds(20),           // 0-20s, default 20s
    VisibilityTimeout = TimeSpan.FromSeconds(60),  // default: the queue's setting
    MessageAttributeNames = ["traceparent"],
    MessageSystemAttributeNames = ["MessageGroupId", "ApproximateReceiveCount"]
};

await foreach (var message in _sqs.ConsumeAsync(options, logger, stoppingToken))
{
    var outcome = await HandleAsync(message.Body, message.ApproximateReceiveCount, stoppingToken);

    if (outcome == Outcome.Success)
    {
        await message.CompleteAsync(stoppingToken);          // DeleteMessage
    }
    else
    {
        await message.AbandonAsync(stoppingToken);           // no request: redelivered when the visibility timeout expires
        // or: await message.AbandonAsync(TimeSpan.Zero);    // ChangeMessageVisibility: redeliver now (or after a delay)
    }
}
```

For long-running work, call `ExtendVisibilityAsync` before the visibility timeout expires to keep the message hidden
from other consumers:

```csharp
await message.ExtendVisibilityAsync(TimeSpan.FromMinutes(2), cancellationToken);
```

Each settle operation returns `ErrorOr<Success>`, so failures (for example an expired receipt handle) are reported
rather than thrown.

### Changing Message Visibility

```csharp
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;

var result = await _sqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
{
    QueueUrl = queueUrl,
    ReceiptHandle = message.ReceiptHandle!,
    VisibilityTimeout = 0 // seconds, 0-43200; 0 makes the message visible immediately
});
```

`ChangeMessageVisibilityBatchAsync` changes up to 10 messages at once and reports per-entry `Successful` and
`Failed` results.

### Multiple Messages

```csharp
public async Task SendMultipleMessagesAsync(string queueUrl, List<OrderEvent> orders)
{
    foreach (var order in orders)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = JsonSerializer.Serialize(order)
        };
            
        var result = await _sqs.SendMessageAsync(request);
        
        if (result.IsError)
        {
            Console.WriteLine($"Send failed: {result.FirstError}");
        }
    }
}
```

### FIFO Queue Support

```csharp
public async Task SendFifoMessageAsync(string queueUrl, object message, string groupId)
{
    var request = new SendMessageRequest
    {
        QueueUrl = queueUrl,
        MessageBody = JsonSerializer.Serialize(message),
        MessageGroupId = groupId,
        MessageDeduplicationId = Guid.NewGuid().ToString()
    };
        
    var result = await _sqs.SendMessageAsync(request);
    
    if (result.IsError)
    {
        Console.WriteLine($"Send failed: {result.FirstError}");
    }
}
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).