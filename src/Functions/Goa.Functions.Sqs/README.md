# Goa.Functions.Sqs

Process Amazon SQS messages in AWS Lambda with high performance and native AOT support. This package provides robust message processing capabilities with flexible batch or single-message handling, automatic error tracking for partial batch failures, and seamless integration with SQS features like message attributes, dead-letter queues, and visibility timeouts for reliable distributed messaging workflows.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.sqs -n "MySqsFunction"
```

## Features

- **Flexible Processing Modes**: Process messages individually or as a batch for optimal throughput
- **Partial Batch Failures**: Mark individual messages as failed for proper retry handling
- **Message Attributes**: Access both system and user-defined message attributes
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **Error Handling**: Built-in support for dead-letter queue integration
- **Dependency Injection**: Full integration with .NET's dependency injection container
- **JSON Deserialization**: Type-safe message body deserialization with source generation

## Basic Usage

```csharp
using Goa.Functions.Core;
using Goa.Functions.Sqs;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForSQS()
    .ProcessOneAtATime()
    .HandleWith<IOrderProcessor>(async (processor, message) =>
    {
        // Deserialize the message body
        var order = JsonSerializer.Deserialize(message.Body!, AppJsonContext.Default.OrderMessage);
        
        // Process the order
        await processor.ProcessOrder(order!);
        
        // Access message attributes if needed
        var priority = message.MessageAttributes?["Priority"]?.StringValue;
        if (priority == "High")
        {
            await processor.HandleHighPriorityOrder(order!);
        }
    })
    .RunAsync();

// Define your message types
public record OrderMessage(string OrderId, string CustomerId, decimal Total, List<OrderItem> Items);
public record OrderItem(string ProductId, int Quantity, decimal Price);

public interface IOrderProcessor
{
    Task ProcessOrder(OrderMessage order);
    Task HandleHighPriorityOrder(OrderMessage order);
}

// JSON source generation for AOT compatibility
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(OrderMessage))]
[JsonSerializable(typeof(OrderItem))]
public partial class AppJsonContext : JsonSerializerContext;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
