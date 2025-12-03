# Goa.Functions.CloudWatchLogs

Process CloudWatch Logs subscription filter events in AWS Lambda with high performance and native AOT support. This package provides automatic base64+gzip decompression, flexible batch or single-event handling, and seamless integration with CloudWatch Logs features like extracted fields and subscription filters for real-time log processing workflows.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.cloudwatchlogs -n "MyCloudWatchLogsFunction"
```

## Features

- **Automatic Decompression**: Base64 decode and gzip decompress handled automatically
- **Flexible Processing Modes**: Process log events individually or as a batch
- **Control Message Filtering**: Automatically skip connectivity check messages in single-event mode
- **Extracted Fields**: Access fields extracted by subscription filter patterns
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **Dependency Injection**: Full integration with .NET's dependency injection container

## Basic Usage

### Process Events One at a Time

```csharp
using Goa.Functions.Core;
using Goa.Functions.CloudWatchLogs;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForCloudWatchLogs()
    .ProcessOneAtATime()
    .HandleWith<ILoggerFactory>((loggerFactory, logEvent, context) =>
    {
        var logger = loggerFactory.CreateLogger("CloudWatchLogsHandler");

        // Access log metadata from context
        var logGroup = context.LogGroup;
        var logStream = context.LogStream;

        // Process the individual log event
        logger.LogInformation("[{Timestamp}] {Message}", logEvent.TimestampDateTime, logEvent.Message);

        // Access extracted fields if configured in subscription filter
        if (logEvent.ExtractedFields?.TryGetValue("level", out var level) == true)
        {
            if (level == "ERROR")
            {
                // Handle error logs specially
            }
        }

        return Task.CompletedTask;
    })
    .RunAsync();
```

### Process Events as a Batch

```csharp
await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForCloudWatchLogs()
    .ProcessAsBatch()
    .HandleWith<ILoggerFactory>((loggerFactory, logsEvent) =>
    {
        var logger = loggerFactory.CreateLogger("CloudWatchLogsHandler");

        // Check for control messages (connectivity checks from CloudWatch)
        if (logsEvent.IsControlMessage)
        {
            logger.LogDebug("Received control message, skipping processing");
            return Task.CompletedTask;
        }

        logger.LogInformation("Processing {Count} log events from {LogGroup}/{LogStream}",
            logsEvent.LogEvents?.Count ?? 0, logsEvent.LogGroup, logsEvent.LogStream);

        foreach (var logEvent in logsEvent.LogEvents ?? [])
        {
            // Process each log event
        }

        return Task.CompletedTask;
    })
    .RunAsync();
```

## CloudWatch Logs Event Structure

When Lambda receives events from CloudWatch Logs subscription filters, the data arrives base64-encoded and gzip-compressed. This package handles decompression automatically.

### CloudWatchLogsEvent Properties

| Property | Type | Description |
|----------|------|-------------|
| `Owner` | `string?` | AWS account ID that owns the log data |
| `LogGroup` | `string?` | Log group name |
| `LogStream` | `string?` | Log stream name |
| `SubscriptionFilters` | `IList<string>?` | Subscription filter names that matched |
| `MessageType` | `string?` | "DATA_MESSAGE" or "CONTROL_MESSAGE" |
| `PolicyLevel` | `string?` | Policy level (e.g., "ACCOUNT_LEVEL") |
| `LogEvents` | `IList<CloudWatchLogEvent>?` | The log events |
| `IsControlMessage` | `bool` | True if this is a connectivity check |
| `IsDataMessage` | `bool` | True if this contains actual log data |

### CloudWatchLogEvent Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Unique identifier for this log event |
| `Timestamp` | `long` | Unix timestamp in milliseconds |
| `Message` | `string?` | The log message content |
| `ExtractedFields` | `Dictionary<string, string>?` | Fields extracted by filter pattern |
| `TimestampDateTime` | `DateTimeOffset` | Timestamp as DateTimeOffset |

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
