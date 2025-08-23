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
                    if (attributeInfo != null)
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