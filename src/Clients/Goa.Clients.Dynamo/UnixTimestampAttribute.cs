namespace Goa.Clients.Dynamo;

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
