using Goa.Functions.Core;
using Goa.Functions.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForS3()
//#if (processingType == "single")
    .ProcessOneAtATime()
    .HandleWith<ILoggerFactory>((handler, record) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the S3 events
        var logger = handler.CreateLogger("S3EventHandler");
        
        // Access S3 event details
        var bucketName = record.S3?.Bucket?.Name;
        var objectKey = record.S3?.Object?.Key;
        var eventName = record.EventName;
        
        logger.LogInformation("Processing S3 event: {EventName} for object {ObjectKey} in bucket {BucketName}", 
            eventName, objectKey, bucketName);

        // TODO :: Add your S3 event processing logic here
        // TODO :: If you want to fail this record from processing, call record.MarkAsFailed();

        return Task.CompletedTask;
    })
//#else
    .ProcessAsBatch()
    .HandleWith<ILoggerFactory>((handler, batch) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the S3 events
        var logger = handler.CreateLogger("S3EventHandler");
        
        logger.LogInformation("Processing batch of {Count} S3 events", batch.Count());

        foreach (var record in batch)
        {
            // Access S3 event details
            var bucketName = record.S3?.Bucket?.Name;
            var objectKey = record.S3?.Object?.Key;
            var eventName = record.EventName;
            
            logger.LogInformation("Processing S3 event: {EventName} for object {ObjectKey} in bucket {BucketName}", 
                eventName, objectKey, bucketName);

            // TODO :: Add your S3 event processing logic here
        }

        // TODO :: If you want to fail one or more records from processing, call record.MarkAsFailed();

        return Task.CompletedTask;
    })
//#endif
    .RunAsync();