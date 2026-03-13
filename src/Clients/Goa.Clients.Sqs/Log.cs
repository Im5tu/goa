using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to send message to SQS queue {QueueUrl}")]
    public static partial void SendMessageFailed(this ILogger logger, Exception exception, string queueUrl);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to send message batch to SQS queue {QueueUrl}")]
    public static partial void SendMessageBatchFailed(this ILogger logger, Exception exception, string queueUrl);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to receive messages from SQS queue {QueueUrl}")]
    public static partial void ReceiveMessageFailed(this ILogger logger, Exception exception, string queueUrl);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to delete message from SQS queue {QueueUrl}")]
    public static partial void DeleteMessageFailed(this ILogger logger, Exception exception, string queueUrl);
}
