using System.Text.Json;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Internal;

/// <summary>
/// Wraps a PropertyNameCache for use with the DynamoItemReader&lt;DynamoRecord&gt; delegate,
/// enabling attribute name deduplication across items in a single response.
/// </summary>
internal sealed class CachedDynamoRecordReader
{
    private PropertyNameCache _cache = new(16);

    public DynamoRecord Read(ref Utf8JsonReader reader)
    {
        return DynamoResponseReader.ReadDynamoRecordItemCached(ref reader, ref _cache);
    }
}
