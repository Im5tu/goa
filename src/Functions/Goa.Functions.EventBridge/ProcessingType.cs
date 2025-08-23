namespace Goa.Functions.EventBridge;

/// <summary>
/// Represents the processing status of an EventBridge event
/// </summary>
internal enum ProcessingType
{
    /// <summary>
    /// Event processing succeeded
    /// </summary>
    Success,

    /// <summary>
    /// Event processing failed
    /// </summary>
    Failure
}