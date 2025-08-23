namespace Goa.Functions.Kinesis;

/// <summary>
/// Represents the processing state of a Kinesis record
/// </summary>
internal enum ProcessingType
{
    /// <summary>
    /// Record was processed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Record processing failed
    /// </summary>
    Failure = 1
}