namespace Goa.Clients.Dynamo;

/// <summary>
/// Specifies the direction in which to ignore a property during DynamoDB mapping.
/// </summary>
public enum IgnoreDirection
{
    /// <summary>
    /// Ignore the property in both directions (reading and writing). This is the default.
    /// </summary>
    Always = 0,
    
    /// <summary>
    /// Ignore the property only when reading from DynamoDB records.
    /// The property will be written to DynamoDB but not read from it.
    /// </summary>
    WhenReading = 1,
    
    /// <summary>
    /// Ignore the property only when writing to DynamoDB records.
    /// The property will be read from DynamoDB but not written to it.
    /// </summary>
    WhenWriting = 2
}