using Goa.Functions.Core;
using Goa.Functions.CloudWatchLogs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForCloudWatchLogs()
//#if (processingType == "single")
    .ProcessOneAtATime()
    .HandleWith<ILoggerFactory>((loggerFactory, logEvent, context) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the CloudWatch Logs events
        var logger = loggerFactory.CreateLogger("CloudWatchLogsHandler");

        // Access log metadata from the context
        var logGroup = context.LogGroup;
        var logStream = context.LogStream;
        var filters = context.SubscriptionFilters;

        logger.LogInformation("Processing log event from {LogGroup}/{LogStream}", logGroup, logStream);
        logger.LogInformation("Event ID: {EventId}, Timestamp: {Timestamp}", logEvent.Id, logEvent.TimestampDateTime);
        logger.LogInformation("Message: {Message}", logEvent.Message);

        // Access extracted fields if your subscription filter has a pattern
        if (logEvent.ExtractedFields?.Count > 0)
        {
            foreach (var field in logEvent.ExtractedFields)
            {
                logger.LogInformation("Extracted field: {Key} = {Value}", field.Key, field.Value);
            }
        }

        // TODO :: Add your log processing logic here
        // Examples:
        // - Forward logs to external systems (Elasticsearch, Datadog, etc.)
        // - Parse and analyze log patterns
        // - Trigger alerts based on log content
        // - Store processed logs in S3 or DynamoDB

        // TODO :: If processing fails and you want to retry the entire batch,
        // throw an exception. CloudWatch Logs does not support partial batch failures.

        return Task.CompletedTask;
    })
//#else
    .ProcessAsBatch()
    .HandleWith<ILoggerFactory>((loggerFactory, logsEvent) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the CloudWatch Logs events
        var logger = loggerFactory.CreateLogger("CloudWatchLogsHandler");

        // Check for control messages (connectivity checks from CloudWatch)
        if (logsEvent.IsControlMessage)
        {
            logger.LogDebug("Received control message, skipping processing");
            return Task.CompletedTask;
        }

        // Access log metadata
        var logGroup = logsEvent.LogGroup;
        var logStream = logsEvent.LogStream;
        var eventCount = logsEvent.LogEvents?.Count ?? 0;

        logger.LogInformation("Processing batch of {Count} log events from {LogGroup}/{LogStream}",
            eventCount, logGroup, logStream);

        foreach (var logEvent in logsEvent.LogEvents ?? [])
        {
            logger.LogInformation("Event ID: {EventId}, Timestamp: {Timestamp}",
                logEvent.Id, logEvent.TimestampDateTime);
            logger.LogInformation("Message: {Message}", logEvent.Message);

            // Access extracted fields if your subscription filter has a pattern
            if (logEvent.ExtractedFields?.Count > 0)
            {
                foreach (var field in logEvent.ExtractedFields)
                {
                    logger.LogInformation("Extracted field: {Key} = {Value}", field.Key, field.Value);
                }
            }
        }

        // TODO :: Add your batch log processing logic here
        // Examples:
        // - Batch insert logs into a database
        // - Aggregate metrics from log data
        // - Forward entire batches to external systems

        // TODO :: If processing fails and you want to retry the entire batch,
        // throw an exception. CloudWatch Logs does not support partial batch failures.

        return Task.CompletedTask;
    })
//#endif
    .RunAsync();
