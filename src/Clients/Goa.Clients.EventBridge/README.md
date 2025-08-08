# Goa.Clients.EventBridge

EventBridge client for event routing in high-performance AWS Lambda functions. This package provides a lightweight, AOT-ready EventBridge client optimized for minimal cold start times.

## Installation

```bash
dotnet add package Goa.Clients.EventBridge
```

## Features

- Native AOT support for faster Lambda cold starts
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Type-safe event publishing
- Support for custom event buses and rules
- Batch event publishing (up to 10 events per request)

## Usage

### Basic Setup

```csharp
using Goa.Clients.EventBridge;
using Microsoft.Extensions.DependencyInjection;

// Register EventBridge client
services.AddEventBridge();

// Or with custom configuration
services.AddEventBridge(config =>
{
    config.ServiceUrl = "http://localhost:4566"; // For LocalStack
    config.Region = "us-west-2";
    config.LogLevel = LogLevel.Debug;
});
```

### Publishing Events

```csharp
using System.Text.Json;
using ErrorOr;
using Goa.Clients.EventBridge;
using Goa.Clients.EventBridge.Models;
using Goa.Clients.EventBridge.Operations.PutEvents;

public class OrderService
{
    private readonly IEventBridgeClient _eventBridge;
    
    public OrderService(IEventBridgeClient eventBridge)
    {
        _eventBridge = eventBridge;
    }
    
    public async Task<ErrorOr<Success>> PublishOrderCreatedAsync(Order order)
    {
        var eventDetail = new
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            Total = order.Total
        };
        
        var request = new PutEventsRequest
        {
            Entries = new List<EventEntry>
            {
                new EventEntry
                {
                    Source = "myapp.orders",
                    DetailType = "Order Created",
                    Detail = JsonSerializer.Serialize(eventDetail),
                    Time = DateTime.UtcNow
                }
            }
        };
        
        var result = await _eventBridge.PutEventsAsync(request);
        return result.IsError ? result.FirstError : Result.Success;
    }
}
```

### Custom Event Bus

```csharp
public async Task<ErrorOr<Success>> PublishToCustomBusAsync()
{
    var eventDetail = new { ProductId = "123", Quantity = 50 };
    
    var request = new PutEventsRequest
    {
        Entries = new List<EventEntry>
        {
            new EventEntry
            {
                Source = "myapp.inventory",
                DetailType = "Stock Updated",
                Detail = JsonSerializer.Serialize(eventDetail),
                EventBusName = "my-custom-bus",
                Time = DateTime.UtcNow
            }
        }
    };
    
    var result = await _eventBridge.PutEventsAsync(request);
    return result.IsError ? result.FirstError : Result.Success;
}
```

### Batch Events

```csharp
public async Task<ErrorOr<Success>> PublishMultipleEventsAsync()
{
    var request = new PutEventsRequest
    {
        Entries = new List<EventEntry>
        {
            new EventEntry
            {
                Source = "myapp.orders",
                DetailType = "Order Created",
                Detail = JsonSerializer.Serialize(new { OrderId = "123" }),
                Time = DateTime.UtcNow
            },
            new EventEntry
            {
                Source = "myapp.inventory", 
                DetailType = "Stock Updated",
                Detail = JsonSerializer.Serialize(new { ProductId = "456" }),
                Time = DateTime.UtcNow
            },
            new EventEntry
            {
                Source = "myapp.notifications",
                DetailType = "Email Sent", 
                Detail = JsonSerializer.Serialize(new { UserId = "789" }),
                Time = DateTime.UtcNow
            }
        }
    };
    
    var result = await _eventBridge.PutEventsAsync(request);
    return result.IsError ? result.FirstError : Result.Success;
}
```

### Error Handling

```csharp
var result = await _eventBridge.PutEventsAsync(request);

if (result.IsError)
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"EventBridge Error: {error.Description}");
    }
    return;
}

// Check for failed entries in the response
var response = result.Value;
if (response.FailedEntryCount > 0)
{
    foreach (var failedEntry in response.Entries.Where(e => e.ErrorCode != null))
    {
        Console.WriteLine($"Failed to publish event: {failedEntry.ErrorCode} - {failedEntry.ErrorMessage}");
    }
}
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).