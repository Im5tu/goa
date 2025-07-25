namespace Goa.Clients.Lambda.Models;

/// <summary>
/// The log type for Lambda function invocations.
/// </summary>
public enum LogType
{
    /// <summary>
    /// No logs are returned.
    /// </summary>
    None,

    /// <summary>
    /// Return the last 4KB of execution log in the response.
    /// </summary>
    Tail
}