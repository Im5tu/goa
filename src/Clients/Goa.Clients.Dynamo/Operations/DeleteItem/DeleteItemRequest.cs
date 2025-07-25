﻿using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations.DeleteItem;

/// <summary>
/// Request for deleting an item from DynamoDB.
/// </summary>
public class DeleteItemRequest
{
    /// <summary>
    /// The name of the table from which to delete the item.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// A map of attribute names to AttributeValue objects representing the primary key of the item to delete.
    /// </summary>
    public Dictionary<string, AttributeValue> Key { get; set; } = new();

    /// <summary>
    /// A condition that must be satisfied in order for a conditional DeleteItem to succeed.
    /// </summary>
    public string? ConditionExpression { get; set; }

    /// <summary>
    /// One or more values that can be substituted in an expression.
    /// </summary>
    public Dictionary<string, AttributeValue>? ExpressionAttributeValues { get; set; }

    /// <summary>
    /// One or more substitution tokens for attribute names in an expression.
    /// </summary>
    public Dictionary<string, string>? ExpressionAttributeNames { get; set; }

    /// <summary>
    /// Use ReturnValues if you want to get the item attributes as they appeared before they were deleted.
    /// For DeleteItem operations, only NONE and ALL_OLD values are supported.
    /// </summary>
    public ReturnValues ReturnValues { get; set; } = ReturnValues.NONE;
}
