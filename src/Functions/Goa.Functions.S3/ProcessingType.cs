namespace Goa.Functions.S3;

/// <summary>
/// Defines how S3 events should be processed
/// </summary>
public enum ProcessingType
{
    /// <summary>
    /// Process S3 events one at a time with individual error handling
    /// </summary>
    Single,

    /// <summary>
    /// Process S3 events as a batch with collective error handling
    /// </summary>
    Batch
}