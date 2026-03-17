using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Goa.Clients.Dynamo.Internal;

/// <summary>
/// Per-response cache that deduplicates attribute name strings during JSON deserialization.
/// For 100 items × 5 attributes, reduces 500 string allocations to 5.
/// </summary>
internal struct PropertyNameCache
{
    private readonly Dictionary<ulong, string> _cache;

    public PropertyNameCache() : this(15)
    {
    }

    public PropertyNameCache(int capacity)
    {
        _cache = new Dictionary<ulong, string>(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string GetOrAdd(ref Utf8JsonReader reader)
    {
        var key = ComputeKey(reader.ValueSpan);
        if (_cache.TryGetValue(key, out var cached) &&
            reader.ValueTextEquals(cached))
            return cached;

        var value = reader.GetString()!;
        _cache[key] = value;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeKey(ReadOnlySpan<byte> name)
    {
        // Pack first 7 bytes + length into a ulong for fast lookup.
        // Collisions are handled by the ValueTextEquals verification.
        var length = name.Length;
        ulong key = (uint)length;

        var toCopy = Math.Min(length, 7);
        for (var i = 0; i < toCopy; i++)
        {
            key |= (ulong)name[i] << ((i + 1) * 8);
        }

        return key;
    }
}
