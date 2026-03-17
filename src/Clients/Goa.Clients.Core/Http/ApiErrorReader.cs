using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Hand-written Utf8JsonReader-based ApiError deserialization.
/// </summary>
internal static class ApiErrorReader
{
    /// <summary>
    /// Reads an ApiError from UTF-8 bytes using Utf8JsonReader.
    /// Matches fields case-insensitively: "message", "__type", "code".
    /// </summary>
    /// <param name="utf8Json">The UTF-8 encoded JSON bytes to deserialize.</param>
    /// <returns>The deserialized ApiError, or null if the input is malformed or empty.</returns>
    public static ApiError? ReadApiError(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
            return null;

        try
        {
            var reader = new Utf8JsonReader(utf8Json);

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
                    if (!reader.Read()) return null;
                    message = reader.GetString();
                }
                else if (reader.ValueTextEquals("__type"u8) || reader.ValueTextEquals("__Type"u8))
                {
                    if (!reader.Read()) return null;
                    type = reader.GetString();
                }
                else if (reader.ValueTextEquals("code"u8) || reader.ValueTextEquals("Code"u8))
                {
                    if (!reader.Read()) return null;
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
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads an ApiError from a JSON string using Utf8JsonReader.
    /// Uses stackalloc for small payloads, ArrayPool for larger ones.
    /// </summary>
    /// <param name="content">The JSON string to deserialize.</param>
    /// <returns>The deserialized ApiError, or null if the input is malformed or empty.</returns>
    public static ApiError? ReadApiError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var byteCount = Encoding.UTF8.GetByteCount(content);

        if (byteCount <= 1024)
        {
            Span<byte> buffer = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(content, buffer);
            return ReadApiError((ReadOnlySpan<byte>)buffer);
        }

        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(content, rented);
            return ReadApiError(rented.AsSpan(0, written));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
