using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Interface for handling type conversion to/from DynamoDB AttributeValue.
/// </summary>
public interface ITypeHandler
{
    /// <summary>
    /// Determines if this handler can process the given property type.
    /// </summary>
    bool CanHandle(PropertyInfo propertyInfo);
    
    /// <summary>
    /// Gets the priority of this handler. Higher priority handlers are checked first.
    /// This allows attribute-specific handlers to override general type handlers.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Generates code to convert from model property to DynamoDB AttributeValue.
    /// </summary>
    string GenerateToAttributeValue(PropertyInfo propertyInfo);
    
    /// <summary>
    /// Generates code to convert from DynamoDB record to model property value.
    /// </summary>
    string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable);
    
    /// <summary>
    /// Generates code for formatting the property value in key patterns (PK/SK).
    /// For example, a DateTime might be formatted as "2023-12-01T10:30:00Z" in a key.
    /// </summary>
    string GenerateKeyFormatting(PropertyInfo propertyInfo);
}