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

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to change message visibility in SQS queue {QueueUrl}")]
    public static partial void ChangeMessageVisibilityFailed(this ILogger logger, Exception exception, string queueUrl);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Failed to change message visibility batch in SQS queue {QueueUrl}")]
    public static partial void ChangeMessageVisibilityBatchFailed(this ILogger logger, Exception exception, string queueUrl);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Receive from SQS queue {QueueUrl} failed with {ErrorCode}: {ErrorDescription}. Retrying in {RetryDelayMs}ms (consecutive failures: {ConsecutiveFailures})")]
    public static partial void ReceiveRetrying(this ILogger logger, string queueUrl, string errorCode, string errorDescription, double retryDelayMs, int consecutiveFailures);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Receive from SQS queue {QueueUrl} threw an unexpected exception")]
    public static partial void ReceiveThrew(this ILogger logger, Exception exception, string queueUrl);
}
