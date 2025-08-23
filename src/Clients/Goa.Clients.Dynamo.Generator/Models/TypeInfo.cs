using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Represents analyzed type information for code generation.
/// Provides a strongly-typed alternative to string-based reflection.
/// </summary>
public class DynamoTypeInfo
{
    public INamedTypeSymbol Symbol { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsAbstract { get; set; }
    public bool IsRecord { get; set; }
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<AttributeInfo> Attributes { get; set; } = new();
    public DynamoTypeInfo? BaseType { get; set; }
    
    /// <summary>
    /// Gets the normalized class name safe for code generation.
    /// </summary>
    public string NormalizedName => Name.Replace(".", "_").Replace("`", "_");
}