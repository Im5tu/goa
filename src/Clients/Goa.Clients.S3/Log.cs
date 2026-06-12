using Microsoft.Extensions.Logging;

namespace Goa.Clients.S3;

internal static partial class Log
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to put object {Key} to S3 bucket {Bucket}")]
    public static partial void PutObjectFailed(this ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to get object {Key} from S3 bucket {Bucket}")]
    public static partial void GetObjectFailed(this ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Failed to head object {Key} in S3 bucket {Bucket}")]
    public static partial void HeadObjectFailed(this ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Failed to delete object {Key} from S3 bucket {Bucket}")]
    public static partial void DeleteObjectFailed(this ILogger logger, Exception exception, string bucket, string key);
}
