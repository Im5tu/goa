namespace Goa.Clients.Dynamo.Enums;

/// <summary>
/// Use ReturnValues if you want to get the item attributes as they appeared before they were updated.
/// </summary>
public enum ReturnValues
{
    /// <summary>
    /// Nothing is returned. (This is the default.)
    /// </summary>
    NONE,
    
    /// <summary>
    /// The content of the old item is returned.
    /// </summary>
    ALL_OLD,
    
    /// <summary>
    /// The updated attributes are returned.
    /// </summary>
    UPDATED_OLD,
    
    /// <summary>
    /// All of the attributes of the new version of the item are returned.
    /// </summary>
    ALL_NEW,
    
    /// <summary>
    /// The new versions of only the updated attributes are returned.
    /// </summary>
    UPDATED_NEW
}