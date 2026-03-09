using System.Text.Json;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Hand-written Utf8JsonReader-based ApiError deserialization.
/// </summary>
internal static class ApiErrorReader
{
    /// <summary>
    /// Reads an ApiError from a JSON string using Utf8JsonReader.
    /// Matches fields case-insensitively: "message", "__type", "code".
    /// </summary>
    /// <param name="content">The JSON string to deserialize.</param>
    /// <returns>The deserialized ApiError, or null if the input is malformed or empty.</returns>
    public static ApiError? ReadApiError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(content));

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return null;

            string? message = null;
            string? type = null;
            string? code = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                if (reader.ValueTextEquals("message"u8) || reader.ValueTextEquals("Message"u8))
                {
                    reader.Read();
                    message = reader.GetString();
                }
                else if (reader.ValueTextEquals("__type"u8) || reader.ValueTextEquals("__Type"u8))
                {
                    reader.Read();
                    type = reader.GetString();
                }
                else if (reader.ValueTextEquals("code"u8) || reader.ValueTextEquals("Code"u8))
                {
                    reader.Read();
                    code = reader.GetString();
                }
                else
                {
                    reader.Skip();
                }
            }

            if (message is null && type is null && code is null)
                return null;

            return new ApiError(message ?? string.Empty, type, code);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
