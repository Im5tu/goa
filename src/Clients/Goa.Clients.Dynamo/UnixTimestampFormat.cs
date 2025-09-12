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