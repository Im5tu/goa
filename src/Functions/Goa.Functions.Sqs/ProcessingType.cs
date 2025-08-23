namespace Goa.Functions.Sqs;

/// <summary>
/// Represents the processing state of an SQS message
/// </summary>
internal enum ProcessingType
{
    /// <summary>
    /// Message was processed successfully
    /// </summary>
    Success = 0,

    /// <summary>
    /// Message processing failed
    /// </summary>
    Failure = 1
}