namespace Goa.Clients.Dynamo;

/// <summary>
/// Specifies the format for unix timestamp conversion
/// </summary>
public enum UnixTimestampFormat
{
    /// <summary>
    /// Unix timestamp in seconds since epoch (default, compatible with AWS TTL)
    /// </summary>
    Seconds = 0,
    
    /// <summary>
    /// Unix timestamp in milliseconds since epoch
    /// </summary>
    Milliseconds = 1
}

/// <summary>
/// Marks a DateTime or DateTimeOffset property to be stored as a unix timestamp.
/// Unix timestamps are stored as numeric values (N type) in DynamoDB.
/// When used in composite PK/SK patterns, they are formatted as strings.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UnixTimestampAttribute : Attribute
{
    /// <summary>
    /// The format for the unix timestamp. Defaults to Seconds (AWS TTL compatible).
    /// </summary>
    public UnixTimestampFormat Format { get; init; } = UnixTimestampFormat.Seconds;
}