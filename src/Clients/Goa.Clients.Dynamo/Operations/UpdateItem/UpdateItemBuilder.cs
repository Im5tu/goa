using System.Globalization;
using System.Text.RegularExpressions;
using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations.UpdateItem;

/// <summary>
/// Fluent builder for constructing DynamoDB UpdateItem requests with a user-friendly API.
/// </summary>
/// <param name="tableName">The name of the table to update the item in.</param>
public partial class UpdateItemBuilder(string tableName)
{
    // Categorized action lists for proper expression consolidation
    private readonly List<string> _setActions = new();
    private readonly List<string> _removeActions = new();
    private readonly List<string> _addActions = new();
    private readonly List<string> _deleteActions = new();

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

    #region SET Operations

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
        _setActions.Add(condition.Expression);

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
            _setActions.Add(expression[4..]); // Strip "SET " prefix
        }
        else
        {
            _setActions.Add(expression);
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

        _setActions.Add($"{nameKey} = if_not_exists({nameKey}, {valueKey})");
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
            _setActions.Add($"{nameKey} = list_append({nameKey}, {valueKey})");
        }
        else
        {
            _setActions.Add($"{nameKey} = list_append({valueKey}, {nameKey})");
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

        _setActions.Add($"{nameKey} = {nameKey} + {valueKey}");
        return this;
    }

    /// <summary>
    /// Set a specific element in a list by index
    /// </summary>
    /// <param name="attributeName">The name of the list attribute.</param>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder SetListElement(string attributeName, int index, AttributeValue value)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = value;

        _setActions.Add($"{nameKey}[{index}] = {valueKey}");
        return this;
    }

    /// <summary>
    /// Set a nested attribute using dot notation path (e.g., "address.city" or "items[0].name")
    /// </summary>
    /// <param name="path">The dot notation path to the nested attribute.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder SetPath(string path, AttributeValue value)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var expressionPath = BuildPathExpression(path);
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";
        _request.ExpressionAttributeValues[valueKey] = value;

        _setActions.Add($"{expressionPath} = {valueKey}");
        return this;
    }

    #endregion

    #region Increment/Decrement Operations

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, byte value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, sbyte value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, short value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, ushort value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, int value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, uint value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, long value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, ulong value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, float value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, double value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Increment a numeric attribute. The attribute must already exist.
    /// Use IncrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Increment(string attributeName, decimal value) => IncrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    private UpdateItemBuilder IncrementInternal(string attributeName, string valueString)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = new AttributeValue { N = valueString };

        _setActions.Add($"{nameKey} = {nameKey} + {valueKey}");
        return this;
    }

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, byte value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, sbyte value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, short value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, ushort value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, int value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, uint value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, long value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, ulong value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, float value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, double value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Decrement a numeric attribute. The attribute must already exist.
    /// Use DecrementAtomically if the attribute might not exist.
    /// </summary>
    public UpdateItemBuilder Decrement(string attributeName, decimal value) => DecrementInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    private UpdateItemBuilder DecrementInternal(string attributeName, string valueString)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = new AttributeValue { N = valueString };

        _setActions.Add($"{nameKey} = {nameKey} - {valueKey}");
        return this;
    }

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, byte value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, sbyte value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, short value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, ushort value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, int value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, uint value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, long value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, ulong value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, float value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, double value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically increment a numeric attribute. Creates the attribute with the specified value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder IncrementAtomically(string attributeName, decimal value) => IncrementAtomicallyInternal(attributeName, value.ToString(CultureInfo.InvariantCulture));

    private UpdateItemBuilder IncrementAtomicallyInternal(string attributeName, string valueString)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = new AttributeValue { N = valueString };

        _addActions.Add($"{nameKey} {valueKey}");
        return this;
    }

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, byte value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, sbyte value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, short value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, ushort value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, int value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, uint value) => DecrementAtomicallyInternal(attributeName, (-(long)value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, long value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, ulong value) => DecrementAtomicallyInternal(attributeName, (-(decimal)value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, float value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, double value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Atomically decrement a numeric attribute. Creates the attribute with the negative value if it doesn't exist.
    /// Uses the ADD action which is atomic but not idempotent.
    /// </summary>
    public UpdateItemBuilder DecrementAtomically(string attributeName, decimal value) => DecrementAtomicallyInternal(attributeName, (-value).ToString(CultureInfo.InvariantCulture));

    private UpdateItemBuilder DecrementAtomicallyInternal(string attributeName, string negatedValueString)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _request.ExpressionAttributeValues[valueKey] = new AttributeValue { N = negatedValueString };

        _addActions.Add($"{nameKey} {valueKey}");
        return this;
    }

    #endregion

    #region REMOVE Operations

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
        _removeActions.Add(nameKey);
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
            foreach (var name in attributeNames)
            {
                var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
                _request.ExpressionAttributeNames[nameKey] = name;
                _removeActions.Add(nameKey);
            }
        }
        return this;
    }

    /// <summary>
    /// Remove a specific element from a list by index
    /// </summary>
    /// <param name="attributeName">The name of the list attribute.</param>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder RemoveListElement(string attributeName, int index)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
        var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
        _request.ExpressionAttributeNames[nameKey] = attributeName;
        _removeActions.Add($"{nameKey}[{index}]");
        return this;
    }

    /// <summary>
    /// Remove multiple elements from a list by their indices
    /// </summary>
    /// <param name="attributeName">The name of the list attribute.</param>
    /// <param name="indices">The zero-based indices of the elements to remove.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder RemoveListElements(string attributeName, params int[] indices)
    {
        if (indices?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
            _request.ExpressionAttributeNames[nameKey] = attributeName;

            foreach (var index in indices)
            {
                _removeActions.Add($"{nameKey}[{index}]");
            }
        }
        return this;
    }

    /// <summary>
    /// Remove a nested attribute using dot notation path (e.g., "address.city")
    /// </summary>
    /// <param name="path">The dot notation path to the nested attribute.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder RemovePath(string path)
    {
        var expressionPath = BuildPathExpression(path);
        _removeActions.Add(expressionPath);
        return this;
    }

    #endregion

    #region ADD Operations

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

        _addActions.Add($"{nameKey} {valueKey}");
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
            _addActions.Add(expression[4..]); // Strip "ADD " prefix
        }
        else
        {
            _addActions.Add(expression);
        }
        return this;
    }

    /// <summary>
    /// Add elements to a string set. Creates the set if it doesn't exist.
    /// </summary>
    /// <param name="attributeName">The name of the string set attribute.</param>
    /// <param name="values">The string values to add to the set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder AddToStringSet(string attributeName, params string[] values)
    {
        if (values?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

            var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
            var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

            _request.ExpressionAttributeNames[nameKey] = attributeName;
            _request.ExpressionAttributeValues[valueKey] = new AttributeValue { SS = values.ToList() };

            _addActions.Add($"{nameKey} {valueKey}");
        }
        return this;
    }

    /// <summary>
    /// Add elements to a number set. Creates the set if it doesn't exist.
    /// </summary>
    /// <param name="attributeName">The name of the number set attribute.</param>
    /// <param name="values">The numeric values to add to the set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder AddToNumberSet(string attributeName, params decimal[] values)
    {
        if (values?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

            var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
            var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

            _request.ExpressionAttributeNames[nameKey] = attributeName;
            _request.ExpressionAttributeValues[valueKey] = new AttributeValue
            {
                NS = values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList()
            };

            _addActions.Add($"{nameKey} {valueKey}");
        }
        return this;
    }

    #endregion

    #region DELETE Operations

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

        _deleteActions.Add($"{nameKey} {valueKey}");
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
            _deleteActions.Add(expression[7..]); // Strip "DELETE " prefix
        }
        else
        {
            _deleteActions.Add(expression);
        }
        return this;
    }

    /// <summary>
    /// Remove elements from a string set.
    /// </summary>
    /// <param name="attributeName">The name of the string set attribute.</param>
    /// <param name="values">The string values to remove from the set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder RemoveFromStringSet(string attributeName, params string[] values)
    {
        if (values?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

            var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
            var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

            _request.ExpressionAttributeNames[nameKey] = attributeName;
            _request.ExpressionAttributeValues[valueKey] = new AttributeValue { SS = values.ToList() };

            _deleteActions.Add($"{nameKey} {valueKey}");
        }
        return this;
    }

    /// <summary>
    /// Remove elements from a number set.
    /// </summary>
    /// <param name="attributeName">The name of the number set attribute.</param>
    /// <param name="values">The numeric values to remove from the set.</param>
    /// <returns>The UpdateItemBuilder instance for method chaining.</returns>
    public UpdateItemBuilder RemoveFromNumberSet(string attributeName, params decimal[] values)
    {
        if (values?.Length > 0)
        {
            _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);
            _request.ExpressionAttributeValues ??= new(StringComparer.OrdinalIgnoreCase);

            var nameKey = $"#n{_request.ExpressionAttributeNames.Count}";
            var valueKey = $":v{_request.ExpressionAttributeValues.Count}";

            _request.ExpressionAttributeNames[nameKey] = attributeName;
            _request.ExpressionAttributeValues[valueKey] = new AttributeValue
            {
                NS = values.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList()
            };

            _deleteActions.Add($"{nameKey} {valueKey}");
        }
        return this;
    }

    #endregion

    #region Condition and Configuration

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

    #endregion

    #region Build

    /// <summary>
    /// Builds and returns the configured UpdateItemRequest.
    /// </summary>
    /// <returns>The configured UpdateItemRequest instance.</returns>
    public UpdateItemRequest Build()
    {
        var parts = new List<string>();

        if (_setActions.Count > 0)
        {
            parts.Add($"SET {string.Join(", ", _setActions)}");
        }

        if (_removeActions.Count > 0)
        {
            parts.Add($"REMOVE {string.Join(", ", _removeActions)}");
        }

        if (_addActions.Count > 0)
        {
            parts.Add($"ADD {string.Join(", ", _addActions)}");
        }

        if (_deleteActions.Count > 0)
        {
            parts.Add($"DELETE {string.Join(", ", _deleteActions)}");
        }

        if (parts.Count > 0)
        {
            _request.UpdateExpression = string.Join(" ", parts);
        }

        return _request;
    }

    #endregion

    #region Path Expression Helpers

    /// <summary>
    /// Regex to match path segments: attribute names and array indices
    /// </summary>
    [GeneratedRegex(@"([^\.\[\]]+)|\[(\d+)\]")]
    private static partial Regex PathSegmentRegex();

    /// <summary>
    /// Builds a DynamoDB-safe expression path from dot notation
    /// </summary>
    private string BuildPathExpression(string path)
    {
        _request.ExpressionAttributeNames ??= new(StringComparer.OrdinalIgnoreCase);

        var matches = PathSegmentRegex().Matches(path);
        var expressionParts = new List<string>();

        foreach (Match match in matches)
        {
            if (match.Groups[1].Success)
            {
                // Attribute name
                var attributeName = match.Groups[1].Value;
                var nameKey = $"#p{_request.ExpressionAttributeNames.Count}";
                _request.ExpressionAttributeNames[nameKey] = attributeName;
                expressionParts.Add(nameKey);
            }
            else if (match.Groups[2].Success)
            {
                // Array index - append to previous part
                var index = match.Groups[2].Value;
                if (expressionParts.Count > 0)
                {
                    expressionParts[^1] += $"[{index}]";
                }
            }
        }

        return string.Join(".", expressionParts);
    }

    #endregion
}
