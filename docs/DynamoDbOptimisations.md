# DynamoDB Client Optimisations: Source-Generated Direct JSON Serialization

## The Problem

The original Goa DynamoDB client used a two-pass deserialization strategy inherited from the standard System.Text.Json (STJ) approach:

```
HTTP Response bytes
    -> STJ deserializes to QueryResponse { List<DynamoRecord> }     (Pass 1)
    -> DynamoMapper converts DynamoRecord to Entity                  (Pass 2)
```

Each `DynamoRecord` is a `Dictionary<string, AttributeValue>`, and each `AttributeValue` is a class with properties for every possible DynamoDB type (S, N, BOOL, NULL, M, L, SS, NS, etc.). For a query returning 1000 items with 11 properties each, this creates:

- 1000 `DynamoRecord` dictionary allocations
- 11,000+ `AttributeValue` object allocations
- Thousands of `string` allocations for dictionary keys
- All of this intermediate data is immediately discarded after mapping to entities

Benchmarks confirmed the impact — Goa was **4.4x slower** than EfficientDynamoDb and allocated **6.8x more memory** at 1000 entities.

## The Solution: Source-Generated Direct JSON Readers

The core insight: DynamoDB's JSON wire format is well-defined and predictable. Every attribute value is wrapped in a type descriptor:

```json
{
  "Items": [
    {
      "pk": {"S": "user#123"},
      "sk": {"S": "profile"},
      "age": {"N": "30"},
      "active": {"BOOL": true},
      "tags": {"SS": ["admin", "user"]},
      "metadata": {"M": {"key": {"S": "value"}}},
      "scores": {"L": [{"N": "95"}, {"N": "87"}]}
    }
  ],
  "Count": 1,
  "ScannedCount": 1
}
```

Instead of letting STJ parse this into generic dictionaries, we source-generate `Utf8JsonReader` code that reads directly from the JSON bytes into strongly-typed entity properties — **zero intermediate allocations**.

### Architecture

```
HTTP Response bytes
    |
    +-- QueryAsync()    -> STJ -> QueryResponse { List<DynamoRecord> }     (existing, unchanged)
    |
    +-- QueryAsync<T>() -> Generated Utf8JsonReader code:
                            1. DynamoResponseReader parses envelope (Items/Count/ScannedCount/LastEvaluatedKey)
                            2. For each item -> DynamoJsonMapper.T.ReadFromJson(ref reader)
                            3. Returns QueryResult<T> { List<T>, ... }
```

The existing untyped path remains completely unchanged. The new typed path is opt-in via a different overload.

### Usage

```csharp
// Existing path (unchanged) - returns DynamoRecord dictionaries
var result = await client.QueryAsync(request);
List<DynamoRecord> items = result.Value.Items;
var entities = items.Select(r => DynamoMapper.MyEntity.FromDynamoRecord(r)).ToList();

// New typed path - direct JSON to entity, zero intermediate allocations
var result = await client.QueryAsync<MyEntity>(
    request,
    DynamoJsonMapper.MyEntity.ReadFromJson);
List<MyEntity> items = result.Value.Items;
```

## Implementation Details

### 1. Source Generator: `JsonMapperGenerator`

**File:** `src/Clients/Goa.Clients.Dynamo.Generator/CodeGeneration/JsonMapperGenerator.cs`

For each `[DynamoModel]` entity, the generator produces a `DynamoJsonMapper.{EntityName}` static class with:

- `ReadFromJson(ref Utf8JsonReader reader)` — reads DynamoDB JSON directly into the entity
- `WriteToJson(Utf8JsonWriter writer, T model)` — writes the entity directly as DynamoDB JSON

Example generated code for a simple entity:

```csharp
public static class DynamoJsonMapper
{
    public static class MyEntity
    {
        public static MyEntity ReadFromJson(ref Utf8JsonReader reader)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                reader.Read();

            var result = new MyEntity();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;

                if (reader.ValueTextEquals("pk"u8))
                {
                    reader.Read(); // StartObject of {"S": "..."}
                    reader.Read(); // "S"
                    reader.Read(); // value
                    result.Pk = reader.GetString()!;
                    reader.Read(); // EndObject
                }
                else if (reader.ValueTextEquals("n"u8))
                {
                    reader.Read(); reader.Read(); reader.Read();
                    result.N = int.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
                    reader.Read();
                }
                else
                {
                    reader.Read();
                    reader.Skip();
                }
            }
            return result;
        }
    }
}
```

The generator handles all DynamoDB types:

| DynamoDB Type | C# Types | Wire Format |
|---|---|---|
| S | `string`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `char`, enums | `{"S": "value"}` |
| N | `int`, `long`, `double`, `decimal`, `float`, `byte`, `short`, and unsigned variants | `{"N": "123"}` |
| BOOL | `bool` | `{"BOOL": true}` |
| NULL | nullable types | `{"NULL": true}` |
| M | complex types (nested `[DynamoModel]` entities), `Dictionary<string, T>` | `{"M": {...}}` |
| L | `List<T>`, `IList<T>`, `IReadOnlyList<T>` | `{"L": [...]}` |
| SS | `HashSet<string>`, `ISet<string>`, `IReadOnlySet<string>` | `{"SS": [...]}` |
| NS | `HashSet<int>`, `HashSet<long>`, etc. | `{"NS": [...]}` |

Additional features:
- **Inheritance / abstract types**: Uses a type discriminator field to dispatch to the correct concrete type's reader via `Utf8JsonReader` copy + peek
- **`[SerializedName]`**: Respects custom attribute name mappings
- **`[Ignore]`**: Skips properties marked for read/write ignore
- **`[UnixTimestamp]`**: Converts `DateTime`/`DateTimeOffset` to/from Unix epoch (seconds or milliseconds)
- **Nullable types**: Checks for `"NULL"` type descriptor before attempting to parse

### 2. Response Envelope Reader

**File:** `src/Clients/Goa.Clients.Dynamo/Internal/DynamoResponseReader.cs`

A hand-written (not generated) utility that parses DynamoDB response envelopes using `Utf8JsonReader`:

```csharp
internal static class DynamoResponseReader
{
    public static QueryResult<T> ReadQueryResponse<T>(
        ReadOnlySpan<byte> utf8Json,
        DynamoJsonReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        // Parses Items array, Count, ScannedCount, LastEvaluatedKey
        // Delegates each item to the generated itemReader
    }
}
```

This cleanly separates envelope parsing (shared across all entity types) from entity deserialization (generated per type).

### 3. Typed Client API

**File:** `src/Clients/Goa.Clients.Dynamo/IDynamoClient.cs`

```csharp
public interface IDynamoClient
{
    // Existing (unchanged)
    Task<ErrorOr<QueryResponse>> QueryAsync(QueryRequest request, CancellationToken ct = default);

    // New typed overload
    Task<ErrorOr<QueryResult<T>>> QueryAsync<T>(
        QueryRequest request,
        DynamoJsonReader<T> itemReader,
        CancellationToken cancellationToken = default);
}
```

The implementation in `DynamoServiceClient` uses `SendRawRequestAsync` to get the raw `HttpResponseMessage`, reads the bytes, and passes them to `DynamoResponseReader` — bypassing STJ entirely.

### 4. UTF-8 Property Name Matching

**Optimisation applied to:** `JsonMapperGenerator.GenerateConcreteReadFromJson`

The initial implementation used `reader.GetString()` to read property names, which allocates a new `string` for every property on every item:

```csharp
// BEFORE: allocates a string per property per item
var propName = reader.GetString();
switch (propName)
{
    case "pk": ...
    case "sk": ...
}
```

Replaced with `Utf8JsonReader.ValueTextEquals()` which compares directly against UTF-8 byte sequences — **zero allocation per property name**:

```csharp
// AFTER: zero-allocation property matching using UTF-8 literals
if (reader.ValueTextEquals("pk"u8))
{ ... }
else if (reader.ValueTextEquals("sk"u8))
{ ... }
```

For a query returning 1000 items with 11 properties each, this eliminates **11,000 string allocations** per query. The `u8` suffix (C# 11+) creates a `ReadOnlySpan<byte>` at compile time with no runtime cost.

This optimisation extends beyond property names to **type descriptor comparisons** inside each property handler. The DynamoDB type wrappers (`"M"`, `"L"`, `"SS"`, `"NS"`, `"NULL"`) are also matched with `ValueTextEquals` instead of `GetString()`:

```csharp
// BEFORE: allocates a string for every type descriptor check
var typeDesc = reader.GetString();
if (typeDesc == "M") { ... }

// AFTER: zero-allocation type descriptor matching
if (reader.ValueTextEquals("M"u8)) { ... }
```

For nullable types, collections, maps, and nested objects, this eliminates an additional string allocation per property per item.

### 5. Pooled Response Buffers

**File:** `src/Clients/Goa.Clients.Dynamo/DynamoServiceClient.cs`

The original implementation used `ReadAsByteArrayAsync` which allocates a new `byte[]` for every HTTP response. For a query returning 1000 items, the response JSON can be 200-500+ KB — allocated and immediately eligible for GC after deserialization.

Replaced with `ArrayPool<byte>.Shared` rented buffers:

```csharp
var contentLength = (int)(response.Content.Headers.ContentLength ?? 4096);
var rentedBuffer = ArrayPool<byte>.Shared.Rent(contentLength);
try
{
    using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
    // Read into rented buffer with dynamic growth if needed
    var result = DynamoResponseReader.ReadQueryResponse(
        rentedBuffer.AsSpan(0, bytesRead), itemReader);
    return result;
}
finally
{
    ArrayPool<byte>.Shared.Return(rentedBuffer);
}
```

The `Content-Length` header (always present in DynamoDB responses) allows right-sizing the initial rent, avoiding growth in the common case. The buffer is returned to the pool after deserialization, producing **zero GC pressure** from response buffering.

## Benchmark Results

All benchmarks use a mixed entity type with strings, numbers, booleans, nested objects, lists of objects (3 lists), string sets, and number sets — 11 properties total.

### Before: Goa (mapped) vs EfficientDynamoDb

| Method | Entities | Mean | Allocated |
|---|---|---|---|
| EfficientDynamoDb | 10 | 18.55 us | 16.97 KB |
| Goa (mapped) | 10 | 38.30 us | 88.61 KB |
| EfficientDynamoDb | 100 | 141.12 us | 119.63 KB |
| Goa (mapped) | 100 | 318.86 us | 789.92 KB |
| EfficientDynamoDb | 1000 | 1,368.03 us | 1,146.20 KB |
| Goa (mapped) | 1000 | 6,031.58 us | 7,804.96 KB |

**Gap at 1000 entities:** 4.4x slower, 6.8x more memory.

### After: Goa (typed) vs EfficientDynamoDb

Includes UTF-8 property name matching (optimisation #4). Pooled buffers and type descriptor `ValueTextEquals` (#5) applied after this benchmark run.

| Method | Entities | Mean | Median | Allocated |
|---|---|---|---|---|
| EfficientDynamoDb | 10 | 18.84 us | 18.76 us | 16.97 KB |
| Goa (mapped) | 10 | 39.94 us | 39.77 us | 88.61 KB |
| **Goa (typed)** | **10** | **20.47 us** | **20.51 us** | **34.20 KB** |
| EfficientDynamoDb | 100 | 141.93 us | 141.99 us | 119.63 KB |
| Goa (mapped) | 100 | 323.26 us | 321.01 us | 789.92 KB |
| **Goa (typed)** | **100** | **136.92 us** | **136.05 us** | **262.34 KB** |
| EfficientDynamoDb | 1000 | 1,427.12 us | 1,425.08 us | 1,146.20 KB |
| Goa (mapped) | 1000 | 5,760.03 us | 5,419.53 us | 7,805.15 KB |
| **Goa (typed)** | **1000** | **1,582.85 us** | **1,568.38 us** | **2,543.50 KB** |

### Improvement: Goa (typed) vs Goa (mapped)

| Entities | Goa (mapped) | Goa (typed) | Speedup | Memory Reduction |
|---|---|---|---|---|
| 10 | 39.94 us / 88.61 KB | 20.47 us / 34.20 KB | **1.95x faster** | **2.59x less** |
| 100 | 323.26 us / 789.92 KB | 136.92 us / 262.34 KB | **2.36x faster** | **3.01x less** |
| 1000 | 5,760.03 us / 7,805.15 KB | 1,582.85 us / 2,543.50 KB | **3.64x faster** | **3.07x less** |

The improvement scales with entity count — at 1000 entities, the typed path is **3.6x faster** with **3x less memory**.

### vs EfficientDynamoDb

| Entities | EfficientDynamoDb | Goa (typed) | Latency ratio | Memory ratio |
|---|---|---|---|---|
| 10 | 18.84 us / 16.97 KB | 20.47 us / 34.20 KB | 1.09x slower | 2.02x more |
| 100 | 141.93 us / 119.63 KB | 136.92 us / 262.34 KB | **0.96x (faster!)** | 2.19x more |
| 1000 | 1,427.12 us / 1,146.20 KB | 1,582.85 us / 2,543.50 KB | 1.11x slower | 2.22x more |

At 100 entities, Goa (typed) is actually **faster** than EfficientDynamoDb. At 1000 entities, latency is within 11%. The remaining ~2.2x memory gap is addressed by pooled response buffers (optimisation #5, applied after this benchmark run).

## What Changed (File Summary)

| File | Action | Description |
|---|---|---|
| `Goa.Clients.Dynamo.Generator/CodeGeneration/JsonMapperGenerator.cs` | New | Source generator for `ReadFromJson`/`WriteToJson` methods |
| `Goa.Clients.Dynamo.Generator/DynamoMapperIncrementalGenerator.cs` | Modified | Wires `JsonMapperGenerator` into the incremental generator pipeline |
| `Goa.Clients.Core/AwsServiceClient.cs` | Modified | Added `SendRawRequestAsync` for raw HTTP response access |
| `Goa.Clients.Core/JsonAwsServiceClient.cs` | Modified | Changed helper methods to `protected` for reuse |
| `Goa.Clients.Dynamo/IDynamoClient.cs` | Modified | Added `QueryAsync<T>` overload |
| `Goa.Clients.Dynamo/DynamoServiceClient.cs` | Modified | Implemented typed `QueryAsync<T>` with direct deserialization |
| `Goa.Clients.Dynamo/Serialization/DynamoJsonReader.cs` | New | `DynamoJsonReader<T>` and `DynamoJsonWriter<T>` delegate types |
| `Goa.Clients.Dynamo/Operations/Query/QueryResultOfT.cs` | New | `QueryResult<T>` — typed query result without `DynamoRecord` |
| `Goa.Clients.Dynamo/Internal/DynamoResponseReader.cs` | New | Response envelope parser using `Utf8JsonReader` |

## Design Decisions

### Why delegates instead of a generic constraint?

The typed `QueryAsync<T>` takes a `DynamoJsonReader<T>` delegate rather than using a generic constraint like `where T : IDynamoEntity`. This avoids:

1. Requiring entities to implement an interface (source generator produces static methods)
2. Virtual dispatch overhead on every item deserialization
3. Boxing for value types (though unlikely for DynamoDB entities)

The delegate approach also allows the compiler to inline the generated reader at the call site.

### Why not replace the existing path?

The untyped `QueryAsync()` returning `DynamoRecord` is still valuable for:

- Dynamic queries where the schema isn't known at compile time
- Debugging and inspection of raw DynamoDB responses
- Backwards compatibility with existing code

Both paths coexist with zero interference.

### Why `ref Utf8JsonReader` instead of `ReadOnlySpan<byte>`?

The `DynamoJsonReader<T>` delegate takes `ref Utf8JsonReader` because:

1. The reader maintains position state as it traverses nested objects
2. The caller (`DynamoResponseReader`) positions the reader at each item's `StartObject` and the reader continues from where the entity reader leaves off
3. This enables single-pass parsing of the entire response with no backtracking

## Future Optimisations

### Remaining Memory Gap

After pooled response buffers, the remaining memory allocations come from:

1. **`List<T>` growth** — The items list starts empty and grows via array doubling. Pre-allocating based on `Count` from the response would eliminate resize copies, but DynamoDB doesn't guarantee field ordering (Count may appear after Items).
2. **String value allocations** — `reader.GetString()` for property *values* (not names or type descriptors — those are already optimised) still allocates. This is inherent for string properties but could be reduced for numeric parsing by using `reader.GetInt32()` directly on the N value where the number fits.
3. **Collection allocations** — Each `List<T>`, `HashSet<T>` per entity property allocates. Could use pooled collections or array-backed storage.

### Planned Features

- **Typed `PutItemAsync<T>`** — Use generated `WriteToJson` for zero-allocation request serialization
- **Typed `ScanAsync<T>`, `GetItemAsync<T>`** — Same pattern as `QueryAsync<T>`, minimal additional code
- **Generated request serialization** — Bypass STJ for `QueryRequest` serialization too
- **`Utf8JsonReader.GetInt32()` for N type** — Skip the string allocation for numeric DynamoDB values when the target type matches directly
