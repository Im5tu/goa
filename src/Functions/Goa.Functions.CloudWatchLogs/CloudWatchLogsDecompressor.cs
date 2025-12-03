using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Utility class for decompressing CloudWatch Logs subscription filter data
/// </summary>
internal static class CloudWatchLogsDecompressor
{
    /// <summary>
    /// Decodes and decompresses the base64+gzip CloudWatch Logs data
    /// </summary>
    /// <param name="base64GzipData">The base64-encoded, gzip-compressed data string</param>
    /// <param name="typeInfo">JSON type info for AOT-compatible deserialization</param>
    /// <returns>The decompressed and deserialized CloudWatchLogsEvent</returns>
    public static CloudWatchLogsEvent? Decompress(string base64GzipData, JsonTypeInfo<CloudWatchLogsEvent> typeInfo)
    {
        // Decode base64
        var compressedBytes = Convert.FromBase64String(base64GzipData);

        // Decompress gzip
        using var compressedStream = new MemoryStream(compressedBytes);
        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();
        gzipStream.CopyTo(decompressedStream);
        decompressedStream.Position = 0;

        // Deserialize JSON using AOT-compatible JsonTypeInfo
        return JsonSerializer.Deserialize(decompressedStream, typeInfo);
    }
}
