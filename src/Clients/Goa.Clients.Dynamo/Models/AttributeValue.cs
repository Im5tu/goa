using System.Globalization;

namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents a DynamoDB attribute value with type information for proper serialization.
/// This class mirrors AWS DynamoDB AttributeValue structure but without SDK dependencies.
/// </summary>
public class AttributeValue
{
    /// <summary>
    /// An attribute of type String. Strings are UTF-8 encoded and can be up to 400 KB in size.
    /// </summary>
    public string? S { get; set; }

    /// <summary>
    /// An attribute of type Number. Numbers are sent across the network to DynamoDB as strings,
    /// to maximize compatibility across platforms and languages.
    /// </summary>
    public string? N { get; set; }

    /// <summary>
    /// An attribute of type Boolean. For example: "BOOL": true
    /// </summary>
    public bool? BOOL { get; set; }

    /// <summary>
    /// An attribute of type String Set. A set of Strings.
    /// </summary>
    public List<string>? SS { get; set; }

    /// <summary>
    /// An attribute of type Number Set. A set of Numbers.
    /// </summary>
    public List<string>? NS { get; set; }

    /// <summary>
    /// An attribute of type List. Lists are ordered collections of values.
    /// </summary>
    public List<AttributeValue>? L { get; set; }

    /// <summary>
    /// An attribute of type Map. Maps are unordered collections of key-value pairs.
    /// </summary>
    public Dictionary<string, AttributeValue>? M { get; set; }

    /// <summary>
    /// An attribute of type Null. When sending a null value, you must send the NULL attribute type.
    /// </summary>
    public bool? NULL { get; set; }

    /// <summary>
    /// Implicitly converts a string to an AttributeValue with S type.
    /// </summary>
    public static implicit operator AttributeValue(string value) => new() { S = value };

    /// <summary>
    /// Implicitly converts an int to an AttributeValue with N type.
    /// </summary>
    public static implicit operator AttributeValue(int value) => new() { N = value.ToString() };

    /// <summary>
    /// Implicitly converts a long to an AttributeValue with N type.
    /// </summary>
    public static implicit operator AttributeValue(long value) => new() { N = value.ToString() };

    /// <summary>
    /// Implicitly converts a double to an AttributeValue with N type.
    /// </summary>
    public static implicit operator AttributeValue(double value) => new() { N = value.ToString(CultureInfo.InvariantCulture) };

    /// <summary>
    /// Implicitly converts a decimal to an AttributeValue with N type.
    /// </summary>
    public static implicit operator AttributeValue(decimal value) => new() { N = value.ToString(CultureInfo.InvariantCulture) };

    /// <summary>
    /// Implicitly converts a bool to an AttributeValue with BOOL type.
    /// </summary>
    public static implicit operator AttributeValue(bool value) => new() { BOOL = value };

    /// <summary>
    /// Implicitly converts a List&lt;string&gt; to an AttributeValue with SS type.
    /// </summary>
    public static implicit operator AttributeValue(List<string> value) => new() { SS = value };

    /// <summary>
    /// Converts the AttributeValue to a strongly-typed value of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <returns>The converted value, or default(T) if conversion fails or the value is NULL.</returns>
    public T? ToValue<T>()
    {
        if (NULL.GetValueOrDefault()) return default;

        return typeof(T) switch
        {
            var t when t == typeof(string) => (T?)(object?)S,
            var t when t == typeof(int) => int.TryParse(N, out var i) ? (T?)(object?)i : default,
            var t when t == typeof(long) => long.TryParse(N, out var l) ? (T?)(object?)l : default,
            var t when t == typeof(double) => double.TryParse(N, out var d) ? (T?)(object?)d : default,
            var t when t == typeof(decimal) => decimal.TryParse(N, out var m) ? (T?)(object?)m : default,
            var t when t == typeof(bool) => (T?)(object?)BOOL,
            var t when t == typeof(List<string>) => (T?)(object?)SS,
            _ => default
        };
    }
}
