using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Operations;

/// <summary>
/// Extension methods for Condition class to provide fluent API for building complex conditions.
/// </summary>
public static class ConditionExtensions
{
    /// <summary>
    /// Combines this condition with another using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="other">The other condition to combine with.</param>
    /// <returns>A new condition representing the AND operation.</returns>
    public static Condition And(this Condition condition, Condition other)
    {
        return Condition.And(condition, other);
    }

    /// <summary>
    /// Combines this condition with an equality condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with an equality condition.</returns>
    public static Condition AndEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.Equals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a not-equals condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with a not-equals condition.</returns>
    public static Condition AndNotEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.NotEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a greater-than condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with a greater-than condition.</returns>
    public static Condition AndGreaterThan(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.GreaterThan(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a greater-than-or-equals condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with a greater-than-or-equals condition.</returns>
    public static Condition AndGreaterThanOrEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.GreaterThanOrEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a less-than condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with a less-than condition.</returns>
    public static Condition AndLessThan(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.LessThan(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a less-than-or-equals condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the AND operation with a less-than-or-equals condition.</returns>
    public static Condition AndLessThanOrEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.LessThanOrEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a between condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value1">The lower bound value.</param>
    /// <param name="value2">The upper bound value.</param>
    /// <returns>A new condition representing the AND operation with a between condition.</returns>
    public static Condition AndBetween(this Condition condition, string attributeName, AttributeValue value1, AttributeValue value2)
    {
        return Condition.And(condition, Condition.Between(attributeName, value1, value2));
    }

    /// <summary>
    /// Combines this condition with a begins-with condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The prefix value to match.</param>
    /// <returns>A new condition representing the AND operation with a begins-with condition.</returns>
    public static Condition AndBeginsWith(this Condition condition, string attributeName, string value)
    {
        return Condition.And(condition, Condition.BeginsWith(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a contains condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>A new condition representing the AND operation with a contains condition.</returns>
    public static Condition AndContains(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.And(condition, Condition.Contains(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with an attribute-exists condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A new condition representing the AND operation with an attribute-exists condition.</returns>
    public static Condition AndAttributeExists(this Condition condition, string attributeName)
    {
        return Condition.And(condition, Condition.AttributeExists(attributeName));
    }

    /// <summary>
    /// Combines this condition with an attribute-not-exists condition using logical AND.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A new condition representing the AND operation with an attribute-not-exists condition.</returns>
    public static Condition AndAttributeNotExists(this Condition condition, string attributeName)
    {
        return Condition.And(condition, Condition.AttributeNotExists(attributeName));
    }

    /// <summary>
    /// Combines this condition with another using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="other">The other condition to combine with.</param>
    /// <returns>A new condition representing the OR operation.</returns>
    public static Condition Or(this Condition condition, Condition other)
    {
        return Condition.Or(condition, other);
    }

    /// <summary>
    /// Combines this condition with an equality condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with an equality condition.</returns>
    public static Condition OrEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.Equals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a not-equals condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with a not-equals condition.</returns>
    public static Condition OrNotEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.NotEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a greater-than condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with a greater-than condition.</returns>
    public static Condition OrGreaterThan(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.GreaterThan(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a greater-than-or-equals condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with a greater-than-or-equals condition.</returns>
    public static Condition OrGreaterThanOrEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.GreaterThanOrEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a less-than condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with a less-than condition.</returns>
    public static Condition OrLessThan(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.LessThan(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a less-than-or-equals condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to compare against.</param>
    /// <returns>A new condition representing the OR operation with a less-than-or-equals condition.</returns>
    public static Condition OrLessThanOrEquals(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.LessThanOrEquals(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a between condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value1">The lower bound value.</param>
    /// <param name="value2">The upper bound value.</param>
    /// <returns>A new condition representing the OR operation with a between condition.</returns>
    public static Condition OrBetween(this Condition condition, string attributeName, AttributeValue value1, AttributeValue value2)
    {
        return Condition.Or(condition, Condition.Between(attributeName, value1, value2));
    }

    /// <summary>
    /// Combines this condition with a begins-with condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The prefix value to match.</param>
    /// <returns>A new condition representing the OR operation with a begins-with condition.</returns>
    public static Condition OrBeginsWith(this Condition condition, string attributeName, string value)
    {
        return Condition.Or(condition, Condition.BeginsWith(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with a contains condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <param name="value">The value to search for.</param>
    /// <returns>A new condition representing the OR operation with a contains condition.</returns>
    public static Condition OrContains(this Condition condition, string attributeName, AttributeValue value)
    {
        return Condition.Or(condition, Condition.Contains(attributeName, value));
    }

    /// <summary>
    /// Combines this condition with an attribute-exists condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A new condition representing the OR operation with an attribute-exists condition.</returns>
    public static Condition OrAttributeExists(this Condition condition, string attributeName)
    {
        return Condition.Or(condition, Condition.AttributeExists(attributeName));
    }

    /// <summary>
    /// Combines this condition with an attribute-not-exists condition using logical OR.
    /// </summary>
    /// <param name="condition">The current condition.</param>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>A new condition representing the OR operation with an attribute-not-exists condition.</returns>
    public static Condition OrAttributeNotExists(this Condition condition, string attributeName)
    {
        return Condition.Or(condition, Condition.AttributeNotExists(attributeName));
    }
}