namespace Goa.Clients.Dynamo.Enums;

/// <summary>
/// The attributes to be returned in the result. You can retrieve all item attributes, 
/// specific item attributes, the count of matching items, or in the case of an index, some or all of the attributes projected into the index.
/// </summary>
public enum Select
{
    /// <summary>
    /// Returns all of the item attributes from the specified table or index.
    /// </summary>
    ALL_ATTRIBUTES,
    
    /// <summary>
    /// Allowed only when querying an index. Returns all of the attributes that have been projected into that index.
    /// </summary>
    ALL_PROJECTED_ATTRIBUTES,
    
    /// <summary>
    /// Returns only the attributes listed in ProjectionExpression.
    /// </summary>
    SPECIFIC_ATTRIBUTES,
    
    /// <summary>
    /// Returns the number of matching items, rather than the matching items themselves.
    /// </summary>
    COUNT
}