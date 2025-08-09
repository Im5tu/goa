# Goa.Functions.Dynamo

DynamoDB stream processing for high-performance AWS Lambda functions. This package provides streamlined processing of DynamoDB streams with type-safe event handling and minimal overhead.

## Installation

```bash
dotnet add package Goa.Functions.Dynamo
```

## Features

- Native AOT support for faster Lambda cold starts
- Type-safe DynamoDB stream event processing
- Built-in error handling and retry logic
- Support for single and batch record processing
- Integration with Goa.Clients.Dynamo for seamless data access
- Minimal dependencies and optimized performance

## Usage

### Basic Stream Processing

```csharp
using Goa.Functions.Dynamo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureGoaFunction(services =>
    {
        services.AddGoaDynamoFunction();
        services.AddScoped<IUserProcessor, UserProcessor>();
    });

var app = builder.Build();
await app.RunGoaDynamoFunctionAsync<UserStreamHandler>();

public class UserStreamHandler : ISingleRecordHandler<User>
{
    private readonly IUserProcessor _processor;
    private readonly ILogger<UserStreamHandler> _logger;
    
    public UserStreamHandler(IUserProcessor processor, ILogger<UserStreamHandler> logger)
    {
        _processor = processor;
        _logger = logger;
    }
    
    public async Task<ProcessResult> HandleAsync(
        DynamoDbStreamRecord<User> record, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing {Operation} for user {UserId}", 
            record.EventName, record.Dynamodb.NewImage?.Id);
            
        return record.EventName switch
        {
            DynamoStreamOperation.Insert => await _processor.HandleInsertAsync(record.Dynamodb.NewImage),
            DynamoStreamOperation.Modify => await _processor.HandleUpdateAsync(
                record.Dynamodb.OldImage, record.Dynamodb.NewImage),
            DynamoStreamOperation.Remove => await _processor.HandleDeleteAsync(record.Dynamodb.OldImage),
            _ => ProcessResult.Success()
        };
    }
}
```

### Batch Processing

```csharp
public class OrderBatchHandler : IMultipleRecordHandler<Order>
{
    private readonly IOrderProcessor _processor;
    
    public OrderBatchHandler(IOrderProcessor processor)
    {
        _processor = processor;
    }
    
    public async Task<BatchProcessResult> HandleAsync(
        IReadOnlyList<DynamoDbStreamRecord<Order>> records, 
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProcessResult>();
        
        foreach (var record in records)
        {
            var result = await ProcessSingleRecord(record, cancellationToken);
            results.Add(result);
        }
        
        return new BatchProcessResult(results);
    }
    
    private async Task<ProcessResult> ProcessSingleRecord(
        DynamoDbStreamRecord<Order> record, 
        CancellationToken cancellationToken)
    {
        // Process individual record
        return ProcessResult.Success();
    }
}
```

### Using Function Builder

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = new DynamoDbFunctionBuilder()
            .ConfigureServices(services =>
            {
                services.AddScoped<IInventoryService, InventoryService>();
            })
            .UseSingleRecordProcessing<Product, ProductHandler>()
            .WithErrorHandling(options =>
            {
                options.MaxRetries = 3;
                options.RetryDelay = TimeSpan.FromSeconds(5);
            });
            
        await builder.RunAsync();
    }
}
```

### Stream View Types

The package supports all DynamoDB stream view types:

```csharp
// Handle different view types
public async Task<ProcessResult> HandleAsync(DynamoDbStreamRecord<User> record, CancellationToken cancellationToken)
{
    return record.Dynamodb.StreamViewType switch
    {
        StreamViewType.KeysOnly => await HandleKeysOnly(record),
        StreamViewType.NewImage => await HandleNewImage(record),
        StreamViewType.OldImage => await HandleOldImage(record),
        StreamViewType.NewAndOldImages => await HandleBothImages(record),
        _ => ProcessResult.Success()
    };
}
```

## Event Processing Options

- **Single Record Processing**: Process one record at a time with `ISingleRecordHandler<T>`
- **Batch Processing**: Process multiple records together with `IMultipleRecordHandler<T>`
- **Processing Type**: Configure sequential or parallel processing modes

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).