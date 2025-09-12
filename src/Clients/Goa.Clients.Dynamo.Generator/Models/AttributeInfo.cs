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