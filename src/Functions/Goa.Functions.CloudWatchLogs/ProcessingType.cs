namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Represents the processing state of a log event
/// </summary>
internal enum ProcessingType
{
    /// <summary>
    /// Log event was processed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Log event processing failed
    /// </summary>
    Failure = 1
}
