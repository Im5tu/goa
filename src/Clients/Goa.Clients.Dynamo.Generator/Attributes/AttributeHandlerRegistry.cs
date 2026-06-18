using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Central registry for all attribute handlers.
/// Provides a plugin-based system for processing different attribute types.
/// </summary>
public class AttributeHandlerRegistry
{
    private readonly List<IAttributeHandler> _handlers = new();
    
    public void RegisterHandler(IAttributeHandler handler)
    {
        _handlers.Add(handler);
    }
    
    /// <summary>
    /// Processes all attributes on a symbol and returns strongly-typed attribute information.
    /// </summary>
    public List<AttributeInfo> ProcessAttributes(ISymbol symbol)
    {
        var result = new List<AttributeInfo>();
        
        foreach (var attributeData in symbol.GetAttributes())
        {
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(attributeData))
                {
                    var attributeInfo = handler.ParseAttribute(attributeData);
                    if (attributeInfo != null && IsAttributeApplicable(attributeInfo, symbol))
                    {
                        result.Add(attributeInfo);
                        break; // Only first handler that can process it
                    }
                }
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a parsed attribute is applicable to the given symbol.
    /// Filters out attributes that are semantically invalid for the target type.
    /// </summary>
    private static bool IsAttributeApplicable(AttributeInfo attributeInfo, ISymbol symbol)
    {
        // UnixTimestamp is only valid on DateTime/DateTimeOffset properties
        if (attributeInfo is UnixTimestampAttributeInfo && symbol is IPropertySymbol property)
        {
            var type = property.Type;

            // Only unwrap genuine Nullable<T>, not arbitrary generics such as List<DateTime>.
            if (type is INamedTypeSymbol named &&
                named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                named.TypeArguments.Length == 1)
            {
                type = named.TypeArguments[0];
            }

            return type.IsDateTimeOrDateTimeOffset();
        }

        return true;
    }

    /// <summary>
    /// Validates all attributes on a symbol and reports diagnostics.
    /// </summary>
    public void ValidateAttributes(ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        var attributes = ProcessAttributes(symbol);
        
        foreach (var attributeInfo in attributes)
        {
            var handler = _handlers.FirstOrDefault(h => h.AttributeTypeName == attributeInfo.AttributeTypeName);
            handler?.ValidateAttribute(attributeInfo, symbol, reportDiagnostic);
        }
    }
    
    /// <summary>
    /// Gets all attribute information of a specific type from a symbol.
    /// </summary>
    public List<T> GetAttributes<T>(ISymbol symbol) where T : AttributeInfo
    {
        return ProcessAttributes(symbol).OfType<T>().ToList();
    }
}