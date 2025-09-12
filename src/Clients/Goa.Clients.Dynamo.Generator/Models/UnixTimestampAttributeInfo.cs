namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Represents Unix timestamp attribute information.
/// </summary>
public class UnixTimestampAttributeInfo : AttributeInfo
{
    public UnixTimestampFormat Format { get; set; }
}