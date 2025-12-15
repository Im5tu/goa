using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Serialization;

/// <summary>
/// Converts Unix timestamps (seconds since epoch) to DateTime.
/// </summary>
public sealed class UnixSecondsDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var unixTimestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp).UtcDateTime;
        }

        // Fallback for string format (unlikely but safe)
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (long.TryParse(stringValue, out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
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
/// </summary>
public sealed class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var unixTimestamp = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime;
        }

        // Fallback for string format (unlikely but safe)
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (long.TryParse(stringValue, out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
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
