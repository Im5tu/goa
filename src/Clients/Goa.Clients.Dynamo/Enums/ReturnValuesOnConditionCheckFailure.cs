namespace Goa.Clients.Dynamo.Enums;

/// <summary>
/// Specifies how to return attribute values when a condition check fails.
/// </summary>
public enum ReturnValuesOnConditionCheckFailure
{
    /// <summary>
    /// Nothing is returned. (This is the default.)
    /// </summary>
    NONE,

    /// <summary>
    /// Returns all attributes of the item as they appeared before the operation.
    /// </summary>
    ALL_OLD
}
