namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents the DynamoDB attribute value type.
/// </summary>
public enum AttributeType : byte
{
    /// <summary>An attribute of type String.</summary>
    String,
    /// <summary>An attribute of type Number (stored as string).</summary>
    Number,
    /// <summary>An attribute of type Boolean.</summary>
    Bool,
    /// <summary>An attribute of type Null.</summary>
    Null,
    /// <summary>An attribute of type String Set.</summary>
    StringSet,
    /// <summary>An attribute of type Number Set.</summary>
    NumberSet,
    /// <summary>An attribute of type List.</summary>
    List,
    /// <summary>An attribute of type Map.</summary>
    Map,
    /// <summary>An attribute of type Binary.</summary>
    Binary
}
