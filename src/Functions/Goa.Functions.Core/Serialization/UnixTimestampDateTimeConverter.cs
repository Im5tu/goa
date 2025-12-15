using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Serialization;

/// <summary>
/// Converts Unix timestamps (seconds since epoch) to DateTime.
/// Handles scientific notation (e.g., 1.765812273E9) and decimal values.
/// </summary>
public sealed class UnixSecondsDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Use GetDouble to handle scientific notation (e.g., 1.765812273E9)
            var unixTimestamp = reader.GetDouble();
            var milliseconds = (long)(unixTimestamp * 1000);
            return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (double.TryParse(stringValue, out var timestamp))
            {
                var milliseconds = (long)(timestamp * 1000);
                return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            }
            return DateTime.Parse(stringValue!);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for DateTime");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTimestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unixTimestamp);
    }
}

/// <summary>
/// Converts Unix timestamps (milliseconds since epoch) to DateTime.
/// Handles scientific notation (e.g., 1.765812273E12).
/// </summary>
public sealed class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Use GetDouble to handle scientific notation
            var unixTimestamp = reader.GetDouble();
            return DateTimeOffset.FromUnixTimeMilliseconds((long)unixTimestamp).UtcDateTime;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (double.TryParse(stringValue, out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).UtcDateTime;
            }
            return DateTime.Parse(stringValue!);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for DateTime");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unixTimestamp = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        writer.WriteNumberValue(unixTimestamp);
    }
}
