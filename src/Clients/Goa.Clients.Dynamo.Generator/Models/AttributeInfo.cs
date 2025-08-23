using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Base class for attribute information.
/// </summary>
public abstract class AttributeInfo
{
    public AttributeData AttributeData { get; set; } = null!;
    public string AttributeTypeName { get; set; } = string.Empty;
}

/// <summary>
/// Represents DynamoDB model attribute information.
/// </summary>
public class DynamoModelAttributeInfo : AttributeInfo
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string PKName { get; set; } = string.Empty;
    public string SKName { get; set; } = string.Empty;
}

/// <summary>
/// Represents Global Secondary Index attribute information.
/// </summary>
public class GSIAttributeInfo : AttributeInfo
{
    public string IndexName { get; set; } = string.Empty;
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string? PKName { get; set; }
    public string? SKName { get; set; }
}

/// <summary>
/// Represents Unix timestamp attribute information.
/// </summary>
public class UnixTimestampAttributeInfo : AttributeInfo
{
    public UnixTimestampFormat Format { get; set; }
}

/// <summary>
/// Unix timestamp format enumeration.
/// </summary>
public enum UnixTimestampFormat
{
    Seconds = 0,
    Milliseconds = 1
}