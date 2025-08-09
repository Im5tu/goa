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
        WaitTimeSeconds = 20
    };
    
    var result = await _sqs.ReceiveMessageAsync(request);
    return result.IsError ? new List<SqsMessage>() : result.Value.Messages ?? new List<SqsMessage>();
}
```

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