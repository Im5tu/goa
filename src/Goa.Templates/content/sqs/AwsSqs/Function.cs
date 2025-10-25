using Goa.Functions.Core;
using Goa.Functions.Sqs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForSQS()
//#if (processingType == "single")
    .ProcessOneAtATime()
    .HandleWith<ILoggerFactory>((handler, message) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the SQS messages
        var logger = handler.CreateLogger("SqsMessageHandler");

        // Access SQS message details
        var messageId = message.MessageId;
        var body = message.Body;
        var attributes = message.MessageAttributes;

        logger.LogInformation("Processing SQS message: {MessageId}", messageId);
        logger.LogInformation("Message body: {Body}", body);

        // TODO :: Add your SQS message processing logic here
        // Example: Deserialize JSON message
        // var data = JsonSerializer.Deserialize<YourMessageType>(body);

        // TODO :: If you want to fail this message from processing, call message.MarkAsFailed();

        return Task.CompletedTask;
    })
//#else
    .ProcessAsBatch()
    .HandleWith<ILoggerFactory>((handler, batch) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that you want to handle the SQS messages
        var logger = handler.CreateLogger("SqsMessageHandler");

        logger.LogInformation("Processing batch of {Count} SQS messages", batch.Count());

        foreach (var message in batch)
        {
            // Access SQS message details
            var messageId = message.MessageId;
            var body = message.Body;
            var attributes = message.MessageAttributes;

            logger.LogInformation("Processing SQS message: {MessageId}", messageId);
            logger.LogInformation("Message body: {Body}", body);

            // TODO :: Add your SQS message processing logic here
            // Example: Deserialize JSON message
            // var data = JsonSerializer.Deserialize<YourMessageType>(body);
        }

        // TODO :: If you want to fail one or more messages from processing, call message.MarkAsFailed();

        return Task.CompletedTask;
    })
//#endif
    .RunAsync();
