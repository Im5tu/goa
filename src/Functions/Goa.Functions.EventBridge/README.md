# Goa.Functions.EventBridge

Process Amazon EventBridge events in AWS Lambda with high performance and native AOT support. This package provides a streamlined way to handle EventBridge custom events, scheduled events, and event patterns with flexible processing modes and built-in error handling for enterprise event-driven architectures.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.eventbridge -n "MyEventFunction"
```

## Features

- **Flexible Event Processing**: Handle custom events, scheduled events, and AWS service events
- **Type-Safe Event Handling**: Strongly-typed event deserialization with full IntelliSense support
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **Error Handling**: Mark individual events as failed for proper error tracking
- **Dependency Injection**: Full integration with .NET's dependency injection container
- **JSON Deserialization**: Built-in support for deserializing Detail property to custom types
- **Event Pattern Support**: Process events from multiple sources and detail types

## Basic Usage

```csharp
using Goa.Functions.Core;
using Goa.Functions.EventBridge;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForEventBridge()
    .ProcessOneAtATime()
    .HandleWith<IOrderService>(async (service, evt) =>
    {
        // Process events based on source and detail type
        if (evt.Source == "myapp.orders" && evt.DetailType == "Order Placed")
        {
            // Deserialize the Detail property to your custom type
            var orderDetail = JsonSerializer.Deserialize(
                JsonSerializer.Serialize(evt.Detail), 
                AppJsonContext.Default.OrderPlacedEvent);
            
            await service.ProcessOrder(orderDetail!);
        }
    })
    .RunAsync();

// Define your event detail types
public record OrderPlacedEvent(string OrderId, string CustomerId, decimal Amount);

public interface IOrderService
{
    Task ProcessOrder(OrderPlacedEvent order);
}

// JSON source generation for AOT compatibility
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(OrderPlacedEvent))]
public partial class AppJsonContext : JsonSerializerContext;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
