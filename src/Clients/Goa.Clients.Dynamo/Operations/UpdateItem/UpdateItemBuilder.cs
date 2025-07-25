using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Core;
using System.Globalization;

namespace Goa.Clients.Dynamo.Operations.UpdateItem;

/// <summary>
/// Fluent builder for constructing DynamoDB UpdateItem requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to update the item in.</param>
public class UpdateItemBuilder(string tableName)
{
    private Lazy<List<string>> _expressions = new(() => new List<string>());
    private readonly UpdateItemRequest _request = new()
    {
        TableName = tableName,
        Key = new Dictionary<string, AttributeValue>()
    };

    /// <summary>
    /// Adds a key attribute to identify the item to update. Supports implicit conversions to AttributeValue
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder WithKey(string attributeName, AttributeValue value)
    {
        _request.Key[attributeName] = value;
        return this;
    }

    /// <summary>
    /// Set the specified attribute to the specified value
    /// </summary>
    /// <param name="attributeName">The name of the key attribute.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Set(string attributeName, AttributeValue value)
    {
        var condition = Condition.Equals(attributeName, value);
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        _expressions.Value.Add($"SET {condition.Expression}");

        return this;
    }

    /// <summary>
    /// Set via a raw expression
    /// </summary>
    /// <param name="expression">The expression to set the record.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Set(string expression)
    {
        if (expression.StartsWith("SET ", StringComparison.OrdinalIgnoreCase))
        {
            _expressions.Value.Add(expression);
        }
        else
        {
            _expressions.Value.Add($"SET {expression}");
        }

        return this;
    }

    /// <summary>
    /// Set an attribute using if_not_exists function - only sets if the attribute doesn't already exist
    /// </summary>
    /// <param name="attributeName">The name of the attribute to set.</param>
    /// <param name="defaultValue">The value to set if the attribute doesn't exist.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder SetIfNotExists(string attributeName, AttributeValue defaultValue)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = defaultValue;

        _expressions.Value.Add($"SET {nameKey} = if_not_exists({nameKey}, {valueKey})");
        return this;
    }

    /// <summary>
    /// Set an attribute by appending to a list using list_append function
    /// </summary>
    /// <param name="attributeName">The name of the list attribute.</param>
    /// <param name="newElements">The elements to append to the list.</param>
    /// <param name="appendToEnd">True to append to end, false to prepend to start.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder SetListAppend(string attributeName, AttributeValue newElements, bool appendToEnd = true)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = newElements;

        if (appendToEnd)
        {
            _expressions.Value.Add($"SET {nameKey} = list_append({nameKey}, {valueKey})");
        }
        else
        {
            _expressions.Value.Add($"SET {nameKey} = list_append({valueKey}, {nameKey})");
        }
        return this;
    }

    /// <summary>
    /// Set an attribute by performing arithmetic operation (addition/subtraction)
    /// </summary>
    /// <param name="attributeName">The name of the number attribute.</param>
    /// <param name="value">The value to add (positive) or subtract (negative).</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder SetArithmetic(string attributeName, AttributeValue value)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = value;

        _expressions.Value.Add($"SET {nameKey} = {nameKey} + {valueKey}");
        return this;
    }

    /// <summary>
    /// Remove one or more attributes from an item
    /// </summary>
    /// <param name="attributeName">The name of the attribute to remove.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Remove(string attributeName)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _expressions.Value.Add($"REMOVE {nameKey}");
        return this;
    }

    /// <summary>
    /// Remove multiple attributes from an item
    /// </summary>
    /// <param name="attributeNames">The names of the attributes to remove.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Remove(params string[] attributeNames)
    {
        if (attributeNames?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            var nameKeys = attributeNames.Select(name =>
            {
                var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
                _request.ExpressionAttributeNames[nameKey] = name;
                return nameKey;
            });
            _expressions.Value.Add($"REMOVE {string.Join(", ", nameKeys)}");
        }
        return this;
    }

    /// <summary>
    /// Add a value to an attribute. For numbers, adds mathematically. For sets, adds to the set.
    /// Creates the attribute with value 0 if it doesn't exist (for numbers).
    /// </summary>
    /// <param name="attributeName">The name of the attribute to add to.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Add(string attributeName, AttributeValue value)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = value;

        _expressions.Value.Add($"ADD {nameKey} {valueKey}");
        return this;
    }

    /// <summary>
    /// Add via a raw expression
    /// </summary>
    /// <param name="expression">The expression to add values.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Add(string expression)
    {
        if (expression.StartsWith("ADD ", StringComparison.OrdinalIgnoreCase))
        {
            _expressions.Value.Add(expression);
        }
        else
        {
            _expressions.Value.Add($"ADD {expression}");
        }
        return this;
    }

    /// <summary>
    /// Delete elements from a set attribute
    /// </summary>
    /// <param name="attributeName">The name of the set attribute.</param>
    /// <param name="values">The values to delete from the set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Delete(string attributeName, AttributeValue values)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = values;

        _expressions.Value.Add($"DELETE {nameKey} {valueKey}");
        return this;
    }

    /// <summary>
    /// Delete via a raw expression
    /// </summary>
    /// <param name="expression">The expression to delete from sets.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder Delete(string expression)
    {
        if (expression.StartsWith("DELETE ", StringComparison.OrdinalIgnoreCase))
        {
            _expressions.Value.Add(expression);
        }
        else
        {
            _expressions.Value.Add($"DELETE {expression}");
        }
        return this;
    }

    /// <summary>
    /// Sets a condition expression that must be satisfied for the update operation to succeed.
    /// </summary>
    /// <param name="condition">The condition that must be met.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder WithCondition(Condition condition)
    {
        _request.ConditionExpression = condition.Expression;
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeNames.Merge(condition.ExpressionNames);
        _request.ExpressionAttributeValues.Merge(condition.ExpressionValues);
        return this;
    }

    /// <summary>
    /// Determines the level of detail about consumed capacity to return.
    /// </summary>
    /// <param name="returnConsumedCapacity">The level of consumed capacity information to return.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder WithReturnConsumedCapacity(ReturnConsumedCapacity returnConsumedCapacity)
    {
        _request.ReturnConsumedCapacity = returnConsumedCapacity;
        return this;
    }

    /// <summary>
    /// Determines what item attributes to return in the response.
    /// </summary>
    /// <param name="returnValues">The return values setting.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder WithReturnValues(ReturnValues returnValues)
    {
        _request.ReturnValues = returnValues;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured UpdateItemRequest.
    /// </summary>
    /// <returns>The configured UpdateItemRequest instance.</returns>
    public UpdateItemRequest Build()
    {
        if (_expressions.IsValueCreated)
        {
            _request.UpdateExpression = string.Join(",", _expressions.Value);
        }

        return _request;
    }
}
