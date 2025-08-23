using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Represents analyzed property information for code generation.
/// </summary>
public class PropertyInfo
{
    public IPropertySymbol Symbol { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ITypeSymbol Type { get; set; } = null!;
    public bool IsNullable { get; set; }
    public bool IsCollection { get; set; }
    public bool IsDictionary { get; set; }
    public List<AttributeInfo> Attributes { get; set; } = new();
    
    /// <summary>
    /// For nullable value types (like int?), gets the underlying non-nullable type.
    /// For nullable reference types and non-nullable types, returns the same type.
    /// </summary>
    public ITypeSymbol UnderlyingType
    {
        get
        {
            // Handle null Type (should not happen in production but may occur in tests)
            if (Type == null)
                return null!;
                
            // Only unwrap nullable value types (Nullable<T>), not nullable reference types
            if (Type is INamedTypeSymbol nullableType && 
                nullableType.OriginalDefinition != null &&
                nullableType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                nullableType.TypeArguments.Length == 1)
            {
                return nullableType.TypeArguments[0];
            }
            return Type;
        }
    }
    
    /// <summary>
    /// For collection types, gets the element type.
    /// For non-collection types, returns null.
    /// </summary>
    public ITypeSymbol? ElementType { get; set; }
    
    /// <summary>
    /// For dictionary types, gets the key and value types.
    /// For non-dictionary types, returns null.
    /// </summary>
    public (ITypeSymbol KeyType, ITypeSymbol ValueType)? DictionaryTypes { get; set; }
}