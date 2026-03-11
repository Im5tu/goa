using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents a DynamoDB attribute value with type information for proper serialization.
/// Uses a union layout to avoid heap allocations for each attribute value.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
[JsonConverter(typeof(AttributeValueJsonConverter))]
public readonly struct AttributeValue
{
    [FieldOffset(0)] private readonly object? _referenceValue;  // string, List<>, Dictionary<>
    [FieldOffset(8)] private readonly AttributeType _type;
    [FieldOffset(9)] private readonly bool _boolValue;

    /// <summary>
    /// Gets the type of this attribute value.
    /// </summary>
    public AttributeType Type => _type;

    private AttributeValue(object? value, AttributeType type)
    {
        _referenceValue = value;
        _type = type;
        _boolValue = false;
    }

    private AttributeValue(bool value)
    {
        _referenceValue = null;
        _type = AttributeType.Bool;
        _boolValue = value;
    }

    /// <summary>Creates an AttributeValue of type String.</summary>
    public static AttributeValue String(string value) => new(value, AttributeType.String);

    /// <summary>Creates an AttributeValue of type Number.</summary>
    public static AttributeValue Number(string value) => new(value, AttributeType.Number);

    /// <summary>Creates an AttributeValue of type Bool.</summary>
    public static AttributeValue Bool(bool value) => new(value);

    /// <summary>Creates an AttributeValue of type Null.</summary>
    public static AttributeValue Null() => new(null, AttributeType.Null);

    /// <summary>Creates an AttributeValue from a String Set.</summary>
    public static AttributeValue FromStringSet(List<string> value) => new(value, AttributeType.StringSet);

    /// <summary>Creates an AttributeValue from a Number Set.</summary>
    public static AttributeValue FromNumberSet(List<string> value) => new(value, AttributeType.NumberSet);

    /// <summary>Creates an AttributeValue from a List of AttributeValues.</summary>
    public static AttributeValue FromList(List<AttributeValue> value) => new(value, AttributeType.List);

    /// <summary>Creates an AttributeValue from a Map of string to AttributeValue.</summary>
    public static AttributeValue FromMap(Dictionary<string, AttributeValue> value) => new(value, AttributeType.Map);

    /// <summary>Creates an AttributeValue of type Binary.</summary>
    public static AttributeValue FromBinary(byte[] value) => new(value, AttributeType.Binary);

    /// <summary>Creates an AttributeValue from a Binary Set.</summary>
    public static AttributeValue FromBinarySet(List<byte[]> value) => new(value, AttributeType.BinarySet);

    /// <summary>
    /// An attribute of type String. Returns null if this is not a String attribute.
    /// </summary>
    public string? S => _type == AttributeType.String ? (string?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Number. Returns null if this is not a Number attribute.
    /// </summary>
    public string? N => _type == AttributeType.Number ? (string?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Boolean. Returns null if this is not a Bool attribute.
    /// </summary>
    public bool? BOOL => _type == AttributeType.Bool ? _boolValue : null;

    /// <summary>
    /// An attribute of type Binary. Returns null if this is not a Binary attribute.
    /// </summary>
    public byte[]? B => _type == AttributeType.Binary ? (byte[]?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Binary Set. Returns null if this is not a BinarySet attribute.
    /// </summary>
    public List<byte[]>? BS => _type == AttributeType.BinarySet ? (List<byte[]>?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Null. Returns true if this is a Null attribute, null otherwise.
    /// </summary>
    public bool? NULL => _type == AttributeType.Null ? true : null;

    /// <summary>
    /// An attribute of type String Set. Returns null if this is not a StringSet attribute.
    /// </summary>
    public List<string>? SS => _type == AttributeType.StringSet ? (List<string>?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Number Set. Returns null if this is not a NumberSet attribute.
    /// </summary>
    public List<string>? NS => _type == AttributeType.NumberSet ? (List<string>?)_referenceValue : null;

    /// <summary>
    /// An attribute of type List. Returns null if this is not a List attribute.
    /// </summary>
    public List<AttributeValue>? L => _type == AttributeType.List ? (List<AttributeValue>?)_referenceValue : null;

    /// <summary>
    /// An attribute of type Map. Returns null if this is not a Map attribute.
    /// </summary>
    public Dictionary<string, AttributeValue>? M => _type == AttributeType.Map ? (Dictionary<string, AttributeValue>?)_referenceValue : null;

    /// <summary>Implicitly converts a string to an AttributeValue with S type.</summary>
    public static implicit operator AttributeValue(string value) => String(value);

    /// <summary>Implicitly converts an int to an AttributeValue with N type.</summary>
    public static implicit operator AttributeValue(int value) => Number(value.ToString());

    /// <summary>Implicitly converts a long to an AttributeValue with N type.</summary>
    public static implicit operator AttributeValue(long value) => Number(value.ToString());

    /// <summary>Implicitly converts a double to an AttributeValue with N type.</summary>
    public static implicit operator AttributeValue(double value) => Number(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Implicitly converts a decimal to an AttributeValue with N type.</summary>
    public static implicit operator AttributeValue(decimal value) => Number(value.ToString(CultureInfo.InvariantCulture));

    /// <summary>Implicitly converts a bool to an AttributeValue with BOOL type.</summary>
    public static implicit operator AttributeValue(bool value) => Bool(value);

    /// <summary>Implicitly converts a byte array to an AttributeValue with B type.</summary>
    public static implicit operator AttributeValue(byte[] value) => FromBinary(value);

    /// <summary>Implicitly converts a List of byte arrays to an AttributeValue with BS type.</summary>
    public static implicit operator AttributeValue(List<byte[]> value) => FromBinarySet(value);

    /// <summary>Implicitly converts a List&lt;string&gt; to an AttributeValue with SS type.</summary>
    public static implicit operator AttributeValue(List<string> value) => FromStringSet(value);

    /// <summary>
    /// Converts the AttributeValue to a strongly-typed value of the specified type.
    /// </summary>
    /// <typeparam name="T">The target type to convert to.</typeparam>
    /// <returns>The converted value, or default(T) if conversion fails or the value is NULL.</returns>
    public T? ToValue<T>()
    {
        if (_type == AttributeType.Null) return default;

        return typeof(T) switch
        {
            var t when t == typeof(string) => (T?)(object?)S,
            var t when t == typeof(int) => int.TryParse(N, out var i) ? (T?)(object?)i : default,
            var t when t == typeof(long) => long.TryParse(N, out var l) ? (T?)(object?)l : default,
            var t when t == typeof(double) => double.TryParse(N, out var d) ? (T?)(object?)d : default,
            var t when t == typeof(decimal) => decimal.TryParse(N, out var m) ? (T?)(object?)m : default,
            var t when t == typeof(bool) => (T?)(object?)BOOL,
            var t when t == typeof(List<string>) => (T?)(object?)SS,
            var t when t == typeof(byte[]) => (T?)(object?)B,
            var t when t == typeof(List<byte[]>) => (T?)(object?)BS,
            _ => default
        };
    }
}

/// <summary>
/// JSON converter for AttributeValue that handles the DynamoDB JSON format.
/// </summary>
public sealed class AttributeValueJsonConverter : JsonConverter<AttributeValue>
{
    /// <inheritdoc />
    public override AttributeValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            return default;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
                continue;

            if (reader.ValueTextEquals("S"u8))
            {
                reader.Read();
                var v = reader.GetString()!;
                reader.Read(); // EndObject
                return AttributeValue.String(v);
            }
            if (reader.ValueTextEquals("N"u8))
            {
                reader.Read();
                var v = reader.GetString()!;
                reader.Read(); // EndObject
                return AttributeValue.Number(v);
            }
            if (reader.ValueTextEquals("BOOL"u8))
            {
                reader.Read();
                var v = reader.GetBoolean();
                reader.Read(); // EndObject
                return AttributeValue.Bool(v);
            }
            if (reader.ValueTextEquals("NULL"u8))
            {
                reader.Read();
                reader.GetBoolean();
                reader.Read(); // EndObject
                return AttributeValue.Null();
            }
            if (reader.ValueTextEquals("SS"u8))
            {
                reader.Read();
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(reader.GetString()!);
                reader.Read(); // EndObject
                return AttributeValue.FromStringSet(list);
            }
            if (reader.ValueTextEquals("NS"u8))
            {
                reader.Read();
                var list = new List<string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(reader.GetString()!);
                reader.Read(); // EndObject
                return AttributeValue.FromNumberSet(list);
            }
            if (reader.ValueTextEquals("L"u8))
            {
                reader.Read();
                var list = new List<AttributeValue>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(Read(ref reader, typeToConvert, options));
                reader.Read(); // EndObject
                return AttributeValue.FromList(list);
            }
            if (reader.ValueTextEquals("M"u8))
            {
                reader.Read();
                var map = new Dictionary<string, AttributeValue>(8);
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var key = reader.GetString()!;
                    reader.Read();
                    map[key] = Read(ref reader, typeToConvert, options);
                }
                reader.Read(); // EndObject
                return AttributeValue.FromMap(map);
            }
            if (reader.ValueTextEquals("B"u8))
            {
                reader.Read();
                var v = Convert.FromBase64String(reader.GetString()!);
                reader.Read(); // EndObject
                return AttributeValue.FromBinary(v);
            }
            if (reader.ValueTextEquals("BS"u8))
            {
                reader.Read();
                var list = new List<byte[]>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(Convert.FromBase64String(reader.GetString()!));
                reader.Read(); // EndObject
                return AttributeValue.FromBinarySet(list);
            }

            // Skip unknown properties
            reader.Read();
            reader.Skip();
        }

        return default;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AttributeValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value.Type)
        {
            case AttributeType.String:
                writer.WriteString("S"u8, value.S);
                break;
            case AttributeType.Number:
                writer.WriteString("N"u8, value.N);
                break;
            case AttributeType.Bool:
                writer.WriteBoolean("BOOL"u8, value.BOOL.GetValueOrDefault());
                break;
            case AttributeType.Null:
                writer.WriteBoolean("NULL"u8, true);
                break;
            case AttributeType.StringSet:
                writer.WriteStartArray("SS"u8);
                foreach (var s in value.SS!)
                    writer.WriteStringValue(s);
                writer.WriteEndArray();
                break;
            case AttributeType.NumberSet:
                writer.WriteStartArray("NS"u8);
                foreach (var s in value.NS!)
                    writer.WriteStringValue(s);
                writer.WriteEndArray();
                break;
            case AttributeType.List:
                writer.WriteStartArray("L"u8);
                foreach (var item in value.L!)
                    Write(writer, item, options);
                writer.WriteEndArray();
                break;
            case AttributeType.Map:
                writer.WriteStartObject("M"u8);
                foreach (var kvp in value.M!)
                {
                    writer.WritePropertyName(kvp.Key);
                    Write(writer, kvp.Value, options);
                }
                writer.WriteEndObject();
                break;
            case AttributeType.Binary:
                writer.WriteBase64String("B"u8, value.B);
                break;
            case AttributeType.BinarySet:
                writer.WriteStartArray("BS"u8);
                foreach (var b in value.BS!)
                    writer.WriteBase64StringValue(b);
                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }
}
