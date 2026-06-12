# Goa.Clients.S3

S3 client for object storage operations in high-performance AWS Lambda functions. This package provides a lightweight, AOT-ready S3 client optimized for minimal cold start times.

## Installation

```bash
dotnet add package Goa.Clients.S3
```

## Features

- Native AOT support for faster Lambda cold starts
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Virtual-host and path-style addressing (path-style is used automatically for custom endpoints such as LocalStack)
- Server-side encryption support (SSE-KMS)
- User-defined object metadata
- Ranged downloads

## Usage

### Basic Setup

```csharp
using Goa.Clients.S3;
using Microsoft.Extensions.DependencyInjection;

// Register S3 client
services.AddS3();

// Or with a custom endpoint (e.g. LocalStack)
services.AddS3(config =>
{
    config.ServiceUrl = "http://localhost:4566";
    config.Region = "us-east-1";
});
```

### Uploading Objects

```csharp
using Goa.Clients.S3.Operations.PutObject;
using System.Text;

public class StorageService
{
    private readonly IS3Client _s3;

    public StorageService(IS3Client s3)
    {
        _s3 = s3;
    }

    public async Task<string?> UploadAsync(string bucket, string key, string json)
    {
        var request = new PutObjectRequest
        {
            Bucket = bucket,
            Key = key,
            Body = Encoding.UTF8.GetBytes(json),
            ContentType = "application/json"
        };

        var result = await _s3.PutObjectAsync(request);
        return result.IsError ? null : result.Value.ETag;
    }
}
```

Or with the fluent builder, including SSE-KMS encryption and metadata:

```csharp
var request = new PutObjectBuilder()
    .WithBucket("my-bucket")
    .WithKey("documents/report.pdf")
    .WithBody(fileBytes)
    .WithContentType("application/pdf")
    .WithServerSideEncryption("aws:kms", "alias/my-key")
    .AddMetadata("uploaded-by", "lambda")
    .Build();

var result = await _s3.PutObjectAsync(request);
```

### Downloading Objects

```csharp
using Goa.Clients.S3.Operations.GetObject;

public async Task<byte[]?> DownloadAsync(string bucket, string key)
{
    var result = await _s3.GetObjectAsync(new GetObjectRequest
    {
        Bucket = bucket,
        Key = key
    });

    if (result.IsError)
    {
        Console.WriteLine($"Download failed: {result.FirstError}");
        return null;
    }

    return result.Value.Body;
}

// Ranged download - first 64 bytes only
var rangeResult = await _s3.GetObjectAsync(new GetObjectRequest
{
    Bucket = "my-bucket",
    Key = "large-file.bin",
    Range = "bytes=0-63"
});
```

> **Memory note:** `GetObjectAsync` buffers the entire object body into memory before returning.
> For large objects, use the `Range` property to fetch the object in bounded slices rather than
> downloading the whole object in a single call.

### Checking Object Metadata

```csharp
using Goa.Clients.S3.Operations.HeadObject;

var head = await _s3.HeadObjectAsync(new HeadObjectRequest
{
    Bucket = "my-bucket",
    Key = "documents/report.pdf"
});

if (!head.IsError)
{
    Console.WriteLine($"Size: {head.Value.ContentLength}, ETag: {head.Value.ETag}");
}
```

### Deleting Objects

```csharp
using Goa.Clients.S3.Operations.DeleteObject;

var result = await _s3.DeleteObjectAsync(new DeleteObjectRequest
{
    Bucket = "my-bucket",
    Key = "documents/report.pdf"
});

// Deleting a missing key also succeeds - S3 deletes are idempotent on unversioned buckets.
```

### Error Handling

Missing objects are surfaced as `ErrorType.NotFound` rather than exceptions:

```csharp
var result = await _s3.GetObjectAsync(new GetObjectRequest { Bucket = "my-bucket", Key = "missing" });

if (result.IsError && result.FirstError.Type == ErrorType.NotFound)
{
    // Handle missing object
}
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
