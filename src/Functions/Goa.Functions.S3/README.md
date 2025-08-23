# Goa.Functions.S3

Process Amazon S3 events in AWS Lambda with high performance and native AOT support. This package provides comprehensive S3 event handling capabilities including object creation, deletion, replication, lifecycle transitions, and intelligent tiering events with flexible processing modes and built-in error handling for reliable object storage workflows.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.s3 -n "MyS3Function"
```

## Features

- **Comprehensive Event Support**: Handle all S3 event types including ObjectCreated, ObjectRemoved, Restore, Replication, and Lifecycle events
- **Flexible Processing Modes**: Process S3 event records individually or as a batch
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **Rich Event Metadata**: Access detailed information about S3 objects, buckets, and user identity
- **Error Handling**: Mark individual records as failed for partial batch failure reporting
- **URL Decoding**: Automatic handling of URL-encoded object keys
- **Dependency Injection**: Full integration with .NET's dependency injection container

## Basic Usage

```csharp
using Goa.Functions.Core;
using Goa.Functions.S3;
using Microsoft.Extensions.Hosting;
using System.Web;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForS3()
    .ProcessOneAtATime()
    .HandleWith<IImageProcessor>(async (processor, record) =>
    {
        // Get S3 object details
        var bucketName = record.S3!.Bucket!.Name;
        var objectKey = HttpUtility.UrlDecode(record.S3.Object!.Key);
        var objectSize = record.S3.Object.Size;
        
        // Process based on event type
        if (record.EventName!.StartsWith("ObjectCreated"))
        {
            await processor.ProcessNewImage(bucketName!, objectKey, objectSize!.Value);
        }
        else if (record.EventName.StartsWith("ObjectRemoved"))
        {
            await processor.HandleImageDeletion(bucketName!, objectKey);
        }
    })
    .RunAsync();

public interface IImageProcessor
{
    Task ProcessNewImage(string bucket, string key, long size);
    Task HandleImageDeletion(string bucket, string key);
}
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
