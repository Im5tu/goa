# Goa.Clients.Sns

SNS client for messaging in high-performance AWS Lambda functions. This package provides a lightweight, AOT-ready SNS client optimized for minimal cold start times.

## Installation

```bash
dotnet add package Goa.Clients.Sns
```

## Features

- Native AOT support for faster Lambda cold starts
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Support for topics, SMS, and push notifications
- Message attributes and FIFO topic support
- Type-safe message publishing

## Usage

### Basic Setup

```csharp
using Goa.Clients.Sns;
using Microsoft.Extensions.DependencyInjection;

// Register SNS client
services.AddSns();

// Or with custom configuration
services.AddSns(config =>
{
    config.ServiceUrl = "http://localhost:4566"; // For LocalStack
    config.Region = "us-west-2";
    config.LogLevel = LogLevel.Debug;
});
```

### Publishing Messages

```csharp
using System.Text.Json;
using ErrorOr;
using Goa.Clients.Sns;
using Goa.Clients.Sns.Operations.Publish;

public class NotificationService
{
    private readonly ISnsClient _sns;
    
    public NotificationService(ISnsClient sns)
    {
        _sns = sns;
    }
    
    public async Task<ErrorOr<Success>> SendNotificationAsync(string message)
    {
        var request = new PublishRequest
        {
            TopicArn = "arn:aws:sns:us-east-1:123456789012:notifications",
            Message = message
        };
            
        var result = await _sns.PublishAsync(request);
        return result.IsError ? result.FirstError : ErrorOr.Success;
    }
}
```

### Publishing with Attributes

```csharp
using Goa.Clients.Sns.Models;

public async Task<ErrorOr<Success>> SendOrderNotificationAsync(Order order)
{
    var request = new PublishRequest
    {
        TopicArn = "arn:aws:sns:us-east-1:123456789012:orders",
        Message = JsonSerializer.Serialize(order),
        Subject = "New Order Received",
        MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
        {
            { "OrderId", SnsMessageAttributeValue.Create(order.Id) },
            { "Priority", SnsMessageAttributeValue.Create("High") },
            { "Amount", SnsMessageAttributeValue.Create(order.Total) }
        }
    };
    
    var result = await _sns.PublishAsync(request);
    return result.IsError ? result.FirstError : ErrorOr.Success;
}
```

### SMS Messaging

```csharp
public async Task<ErrorOr<Success>> SendSmsAsync(string phoneNumber, string message)
{
    var request = new PublishRequest
    {
        PhoneNumber = phoneNumber,
        Message = message
    };
        
    var result = await _sns.PublishAsync(request);
    
    if (result.IsError)
    {
        Console.WriteLine($"Failed to send SMS: {result.FirstError}");
        return result.FirstError;
    }
    
    Console.WriteLine($"SMS sent successfully. Message ID: {result.Value.MessageId}");
    return ErrorOr.Success;
}
```

### Push Notification to Mobile Endpoint

```csharp
public async Task<ErrorOr<Success>> SendPushNotificationAsync(string targetArn, object notification)
{
    var request = new PublishRequest
    {
        TargetArn = targetArn, // Mobile platform endpoint ARN
        Message = JsonSerializer.Serialize(notification),
        MessageStructure = "json" // For structured notifications
    };
        
    var result = await _sns.PublishAsync(request);
    return result.IsError ? result.FirstError : ErrorOr.Success;
}
```

### FIFO Topic Publishing

```csharp
public async Task<ErrorOr<Success>> SendFifoMessageAsync(string message, string groupId)
{
    var request = new PublishRequest
    {
        TopicArn = "arn:aws:sns:us-east-1:123456789012:orders.fifo",
        Message = message,
        MessageGroupId = groupId,
        MessageDeduplicationId = Guid.NewGuid().ToString() // Or use content-based deduplication
    };
        
    var result = await _sns.PublishAsync(request);
    return result.IsError ? result.FirstError : ErrorOr.Success;
}
```

### Custom Message Attributes

```csharp
public async Task<ErrorOr<Success>> SendWithCustomAttributesAsync()
{
    var binaryData = System.Text.Encoding.UTF8.GetBytes("Hello World");
    
    var request = new PublishRequest
    {
        TopicArn = "arn:aws:sns:us-east-1:123456789012:notifications",
        Message = "Message with custom attributes",
        MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
        {
            { "StringAttr", SnsMessageAttributeValue.Create("Value") },
            { "NumberAttr", SnsMessageAttributeValue.Create(42) },
            { "BinaryAttr", SnsMessageAttributeValue.Create(binaryData) },
            { "CustomType", SnsMessageAttributeValue.Create("CustomValue", "String.Custom") }
        }
    };
    
    var result = await _sns.PublishAsync(request);
    return result.IsError ? result.FirstError : ErrorOr.Success;
}
```

### Error Handling

```csharp
var result = await _sns.PublishAsync(request);

if (result.IsError)
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"SNS Error: {error.Description}");
    }
    return;
}

var response = result.Value;
Console.WriteLine($"Message published successfully. Message ID: {response.MessageId}");

// For FIFO topics, you can also access the sequence number
if (!string.IsNullOrEmpty(response.SequenceNumber))
{
    Console.WriteLine($"FIFO Sequence Number: {response.SequenceNumber}");
}
```

## Available Message Attribute Types

The SNS client supports various message attribute data types:

- **String**: `SnsMessageAttributeValue.Create("value")` or `AddStringAttribute("name", "value")`
- **Number**: `SnsMessageAttributeValue.Create(42)`
- **Binary**: `SnsMessageAttributeValue.Create(byteArray)` or `SnsMessageAttributeValue.CreateBase64("base64data")`
- **Custom Types**: `SnsMessageAttributeValue.Create("value", "String.Custom")`

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).