using Goa.Clients.Dynamo.Models;
using Goa.Core;

namespace Goa.Clients.Dynamo.Operations;

/// <summary>
/// Represents a condition expression for DynamoDB operations with associated attribute names and values.
/// </summary>
public class Condition
{
    /// <summary>
    /// Gets the condition expression string.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Gets the expression attribute names used in the condition.
    /// </summary>
    public IReadOnlyDictionary<string, string> ExpressionNames { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the expression attribute values used in the condition.
    /// </summary>
    public IReadOnlyDictionary<string, AttributeValue> ExpressionValues { get; } = new Dictionary<string, AttributeValue>();

    /// <summary>
    /// Initializes a new instance of the Condition class with the specified expression.
    /// </summary>
    /// <param name="expression">The condition expression string.</param>
    public Condition(string expression)
    {
        Expression = expression;
    }

    /// <summary>
    /// Initializes a new instance of the Condition class with expression, names, and values.
    /// </summary>
    /// <param name="expression">The condition expression string.</param>
    /// <param name="expressionNames">The expression attribute names.</param>
    /// <param name="expressionValues">The expression attribute values.</param>
    public Condition(string expression, IEnumerable<KeyValuePair<string, string>> expressionNames, IEnumerable<KeyValuePair<string, AttributeValue>> expressionValues)
    {
        Expression = expression;
        ExpressionNames = expressionNames.ToDictionary();
        ExpressionValues = expressionValues.ToDictionary();
    }

    /// <summary>
    /// Creates an equality condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the equality comparison.</returns>
    public static Condition Equals(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} = :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}",  value)]);
    }

    /// <summary>
    /// Creates a not-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the not-equals comparison.</returns>
    public static Condition NotEquals(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} <> :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a greater-than condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the greater-than comparison.</returns>
    public static Condition GreaterThan(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} > :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a greater-than-or-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the greater-than-or-equals comparison.</returns>
    public static Condition GreaterThanOrEquals(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} >= :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a less-than condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the less-than comparison.</returns>
    public static Condition LessThan(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} < :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a less-than-or-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A condition representing the less-than-or-equals comparison.</returns>
    public static Condition LessThanOrEquals(string attributeName, AttributeValue value)
    {
        return new Condition($"#{attributeName} <= :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a between condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value1">The lower bound value.</param>
    /// <param name="value2">The upper bound value.</param>
    /// <returns>A condition representing the between comparison.</returns>
    public static Condition Between(string attributeName, AttributeValue value1, AttributeValue value2)
    {
        return new Condition($"#{attributeName} BETWEEN :{attributeName}1 AND :{attributeName}2", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}1", value1), new KeyValuePair<string, AttributeValue>($":{attributeName}2", value2)
        ]);
    }

    /// <summary>
    /// Creates a begins-with condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The prefix value to match.</param>
    /// <returns>A condition representing the begins-with function.</returns>
    public static Condition BeginsWith(string attributeName, string value)
    {
        return new Condition($"begins_with(#{attributeName}, :{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a contains condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>A condition representing the contains function.</returns>
    public static Condition Contains(string attributeName, AttributeValue value)
    {
        return new Condition($"contains(#{attributeName}, :{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a not-contains condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>A condition representing the not-contains function.</returns>
    public static Condition NotContains(string attributeName, AttributeValue value)
    {
        return new Condition($"NOT contains(#{attributeName}, :{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", value)]);
    }

    /// <summary>
    /// Creates a size-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size equals comparison.</returns>
    public static Condition SizeEquals(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) = :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates a size-not-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size not-equals comparison.</returns>
    public static Condition SizeNotEquals(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) <> :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates a size-greater-than condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size greater-than comparison.</returns>
    public static Condition SizeGreaterThan(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) > :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates a size-greater-than-or-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size greater-than-or-equals comparison.</returns>
    public static Condition SizeGreaterThanOrEquals(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) >= :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates a size-less-than condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size less-than comparison.</returns>
    public static Condition SizeLessThan(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) < :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates a size-less-than-or-equals condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="size">The size to compare against.</param>
    /// <returns>A condition representing the size less-than-or-equals comparison.</returns>
    public static Condition SizeLessThanOrEquals(string attributeName, int size)
    {
        return new Condition($"size(#{attributeName}) <= :{attributeName}", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { N = size.ToString() })]);
    }

    /// <summary>
    /// Creates an attribute-exists condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A condition representing the attribute-exists function.</returns>
    public static Condition AttributeExists(string attributeName)
    {
        return new Condition($"attribute_exists(#{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], []);
    }

    /// <summary>
    /// Creates an attribute-not-exists condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A condition representing the attribute-not-exists function.</returns>
    public static Condition AttributeNotExists(string attributeName)
    {
        return new Condition($"attribute_not_exists(#{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], []);
    }

    /// <summary>
    /// Creates an attribute-type condition for the specified attribute.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="type">The type to check for.</param>
    /// <returns>A condition representing the attribute-type function.</returns>
    public static Condition AttributeType(string attributeName, string type)
    {
        return new Condition($"attribute_type(#{attributeName}, :{attributeName})", [new KeyValuePair<string, string>($"#{attributeName}", attributeName)
        ], [new KeyValuePair<string, AttributeValue>($":{attributeName}", new AttributeValue { S = type })]);
    }

    /// <summary>
    /// Combines two conditions with a logical AND operator.
    /// </summary>
    /// <param name="left">The left condition.</param>
    /// <param name="right">The right condition.</param>
    /// <returns>A condition representing the logical AND of the two conditions.</returns>
    public static Condition And(Condition left, Condition right)
    {
        var expression = $"{left.Expression} AND {right.Expression}";
        var expressionNames = left.ExpressionNames.Merge(right.ExpressionNames);
        var expressionValues = left.ExpressionValues.Merge(right.ExpressionValues);
        return new Condition(expression, expressionNames, expressionValues);
    }

    /// <summary>
    /// Combines multiple conditions with logical AND operators.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns>A condition representing the logical AND of all conditions.</returns>
    public static Condition And(params Condition[] conditions)
    {
        var expression = string.Join(" AND ", conditions.Select(c => c.Expression));
        var expressionNames = conditions.SelectMany(c => c.ExpressionNames).ToDictionary();
        var expressionValues = conditions.SelectMany(c => c.ExpressionValues).ToDictionary();
        return new Condition(expression, expressionNames, expressionValues);
    }

    /// <summary>
    /// Combines two conditions with a logical OR operator.
    /// </summary>
    /// <param name="left">The left condition.</param>
    /// <param name="right">The right condition.</param>
    /// <returns>A condition representing the logical OR of the two conditions.</returns>
    public static Condition Or(Condition left, Condition right)
    {
        var expression = $"{left.Expression} OR {right.Expression}";
        var expressionNames = left.ExpressionNames.Merge(right.ExpressionNames);
        var expressionValues = left.ExpressionValues.Merge(right.ExpressionValues);
        return new Condition(expression, expressionNames, expressionValues);
    }

    /// <summary>
    /// Combines multiple conditions with logical OR operators.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns>A condition representing the logical OR of all conditions.</returns>
    public static Condition Or(params Condition[] conditions)
    {
        var expression = string.Join(" OR ", conditions.Select(c => c.Expression));
        var expressionNames = conditions.SelectMany(c => c.ExpressionNames).ToDictionary();
        var expressionValues = conditions.SelectMany(c => c.ExpressionValues).ToDictionary();
        return new Condition(expression, expressionNames, expressionValues);
    }

    /// <summary>
    /// Creates an IN condition for the specified attribute with multiple string values.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="values">The values to match against.</param>
    /// <returns>A condition representing the IN comparison.</returns>
    public static Condition In(string attributeName, params AttributeValue[] values)
    {
        if (values is null || values.Length == 0)
            throw new ArgumentException("At least one value must be provided for IN condition", nameof(values));

        var valueParams = values.Select((_, i) => $":{attributeName}{i}").ToArray();
        var expression = $"#{attributeName} IN ({string.Join(", ", valueParams)})";
        var expressionNames = new[] { new KeyValuePair<string, string>($"#{attributeName}", attributeName) };
        var expressionValues = values.Select((value, i) => new KeyValuePair<string, AttributeValue>($":{attributeName}{i}",  value));
        return new Condition(expression, expressionNames, expressionValues);
    }
}
