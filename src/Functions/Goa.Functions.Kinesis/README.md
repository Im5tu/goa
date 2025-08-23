# Goa.Functions.Kinesis

Process Amazon Kinesis Data Streams events in AWS Lambda with high performance and native AOT support. This package provides streamlined real-time data processing capabilities with flexible batch or single-record processing modes, automatic base64 decoding, and built-in error handling for reliable stream processing at scale.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.kinesis -n "MyKinesisFunction"
```

## Features

- **Flexible Processing Modes**: Choose between processing records one at a time or as a batch
- **Automatic Data Decoding**: Built-in base64 decoding of Kinesis record payloads
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **Error Handling**: Mark individual records as failed for partial batch failure reporting
- **Stream Metadata Access**: Full access to partition keys, sequence numbers, and arrival timestamps
- **Dependency Injection**: Full integration with .NET's dependency injection container
- **High Throughput**: Efficient batch processing for high-volume streaming data

## Basic Usage

```csharp
using Goa.Functions.Core;
using Goa.Functions.Kinesis;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForKinesis()
    .ProcessOneAtATime()
    .HandleWith<IDataProcessor>(async (processor, record) =>
    {
        // Decode the base64-encoded data
        var decodedBytes = Convert.FromBase64String(record.Kinesis!.Data!);
        var jsonData = Encoding.UTF8.GetString(decodedBytes);
        
        // Deserialize to your custom type
        var message = JsonSerializer.Deserialize(jsonData, AppJsonContext.Default.StreamMessage);
        
        // Process the message
        await processor.ProcessMessage(message!);
    })
    .RunAsync();

// Define your message types
public record StreamMessage(string Id, string Type, DateTime Timestamp, object Payload);

public interface IDataProcessor
{
    Task ProcessMessage(StreamMessage message);
}

// JSON source generation for AOT compatibility
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(StreamMessage))]
public partial class AppJsonContext : JsonSerializerContext;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
