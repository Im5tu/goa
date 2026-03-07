# DynamoDB Typed API

The Goa DynamoDB client provides a **zero-copy typed deserialization path** that bypasses intermediate `DynamoRecord` allocations. This is the recommended approach for all DynamoDB operations in performance-sensitive applications.

## Table of Contents

- [Getting Started](#getting-started)
- [Defining Models](#defining-models)
- [Custom Converters](#custom-converters)
- [Reading Items](#reading-items)
- [Writing Items](#writing-items)
- [Pagination](#pagination)
- [Registry-Based Usage](#registry-based-usage)
- [Migration from Untyped API](#migration-from-untyped-api)

---

## Getting Started

### 1. Define your model

```csharp
using Goa.Clients.Dynamo;

[DynamoModel(PK = "USER#<Id>", SK = "PROFILE#<Email>")]
public record UserProfile(string Id, string Email, string DisplayName, int Age);
```

### 2. Initialize the registry at startup

```csharp
// Call once during application startup (e.g., in Program.cs)
DynamoReaderRegistration.Initialize();
```

This triggers the source-generated static constructor that registers all `[DynamoModel]` types with `DynamoItemReaderRegistry` and `DynamoItemWriterRegistry`.

### 3. Use the typed API

```csharp
// Read
var result = await client.QueryAsync(request, DynamoItemReaderRegistry.Get<UserProfile>());

// Write
await client.PutItemAsync("Users", user, DynamoItemWriterRegistry.Get<UserProfile>());
```

---

## Defining Models

### Basic Model

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "PROFILE#<Email>")]
public record UserProfile(string Id, string Email, string DisplayName, int Age);
```

The `PK` and `SK` patterns use `<PropertyName>` placeholders that map to properties on the type.

### With Global Secondary Indexes

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "PROFILE#<Email>")]
[GlobalSecondaryIndex(Name = "EmailIndex", PK = "EMAIL#<Email>", SK = "USER#<Id>")]
public record UserProfile(string Id, string Email, string DisplayName, int Age);
```

### Inheritance

```csharp
[DynamoModel(PK = "ENTITY#<Id>", SK = "DATA#<Id>")]
public abstract record BaseEntity(string Id);

public record Order(string Id, string CustomerId, decimal Total) : BaseEntity(Id);
public record Invoice(string Id, string OrderId, DateTime IssuedAt) : BaseEntity(Id);
```

The generator automatically adds a type discriminator field for polymorphic deserialization.

### Property Attributes

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "DATA")]
public class User
{
    public string Id { get; set; } = "";

    [SerializedName("user_name")]       // Custom DynamoDB attribute name
    public string UserName { get; set; } = "";

    [UnixTimestamp]                      // Store as Unix timestamp (seconds)
    public DateTime CreatedAt { get; set; }

    [UnixTimestamp(Format = Format.Milliseconds)] // Higher precision
    public DateTime UpdatedAt { get; set; }

    [Ignore]                            // Exclude from serialization entirely
    public string ComputedField { get; set; } = "";

    [Ignore(Direction = Direction.WhenWriting)] // Read but don't write
    public string ReadOnlyField { get; set; } = "";
}
```

---

## Custom Converters

For properties that need custom serialization (compressed data, MemoryPack, binary formats), use `[DynamoConverter]`.

### Define a converter

Implement `IDynamoPropertyConverter<T>`:

```csharp
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Goa.Clients.Dynamo;

public class CompressedStringConverter : IDynamoPropertyConverter<string>
{
    public string Read(ref Utf8JsonReader reader)
    {
        // Read from DynamoDB wire format: {"B": "base64data"}
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.GetString() == "B")
            {
                reader.Read();
                var base64 = reader.GetString()!;
                var compressed = Convert.FromBase64String(base64);
                using var input = new MemoryStream(compressed);
                using var brotli = new BrotliStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                brotli.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
            reader.Skip();
        }
        return "";
    }

    public void Write(Utf8JsonWriter writer, string value)
    {
        // Write as DynamoDB binary: {"B": "base64data"}
        var bytes = Encoding.UTF8.GetBytes(value);
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Fastest))
        {
            brotli.Write(bytes);
        }
        writer.WriteStartObject();
        writer.WriteString("B", Convert.ToBase64String(output.ToArray()));
        writer.WriteEndObject();
    }
}
```

### Apply to a property

```csharp
[DynamoModel(PK = "PAGE#<Id>", SK = "CONTENT")]
public class PageContent
{
    public string Id { get; set; } = "";

    [DynamoConverter(typeof(CompressedStringConverter))]
    public string HtmlBody { get; set; } = "";

    [DynamoConverter(typeof(MemoryPackConverter<Metadata>))]
    public Metadata Meta { get; set; } = new();
}
```

The source generator emits converter calls instead of inline serialization for these properties — no reflection, fully AOT-compatible.

### Entity-level override

For full control over an entire type's serialization, register a custom reader/writer after initialization:

```csharp
DynamoReaderRegistration.Initialize();

// Replace the generated reader for a specific type
DynamoItemReaderRegistry.Register<MyCustomEntity>(MyCustomReader.Read);
DynamoItemWriterRegistry.Register<MyCustomEntity>(MyCustomWriter.Write);
```

---

## Reading Items

### GetItem

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

var user = await client.GetItemAsync(
    new GetItemRequest
    {
        TableName = "Users",
        Key = DynamoKeyFactory.UserProfile.PK("123").WithSK(DynamoKeyFactory.UserProfile.SK("john@example.com"))
    },
    reader);

if (user.IsError)
{
    // Handle error
    return;
}

var profile = user.Value; // UserProfile? — null if not found
```

### Query

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

var result = await client.QueryAsync(
    new QueryRequest
    {
        TableName = "Users",
        KeyConditionExpression = "PK = :pk",
        ExpressionAttributeValues = new()
        {
            [":pk"] = new AttributeValue { S = "USER#123" }
        }
    },
    reader);

foreach (var profile in result.Value.Items)
{
    Console.WriteLine(profile.DisplayName);
}

// Check for more pages
if (result.Value.HasMoreResults)
{
    // Use result.Value.LastEvaluatedKey for next page
}
```

### Scan

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

var result = await client.ScanAsync(
    new ScanRequest { TableName = "Users" },
    reader);
```

### BatchGetItem

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

var result = await client.BatchGetItemAsync(
    new BatchGetItemRequest
    {
        RequestItems = new()
        {
            ["Users"] = new BatchGetRequestItem
            {
                Keys = new List<Dictionary<string, AttributeValue>>
                {
                    new() { ["PK"] = new() { S = "USER#1" }, ["SK"] = new() { S = "PROFILE#a@b.com" } },
                    new() { ["PK"] = new() { S = "USER#2" }, ["SK"] = new() { S = "PROFILE#c@d.com" } }
                }
            }
        }
    },
    reader);

// Items grouped by table name
foreach (var (tableName, items) in result.Value.Responses)
{
    foreach (var item in items)
    {
        Console.WriteLine(item.DisplayName);
    }
}

// Check for unprocessed keys
if (result.Value.HasUnprocessedKeys)
{
    // Retry with result.Value.UnprocessedKeys
}
```

### TransactGetItems

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

var result = await client.TransactGetItemsAsync(
    new TransactGetRequest
    {
        TransactItems = new List<TransactGetItem>
        {
            new() { Get = new TransactGetRequest { TableName = "Users", Key = key1 } },
            new() { Get = new TransactGetRequest { TableName = "Users", Key = key2 } }
        }
    },
    reader);

// Items in same order as request — null if not found
foreach (var item in result.Value.Items)
{
    if (item is not null)
        Console.WriteLine(item.DisplayName);
}
```

---

## Writing Items

### PutItem (typed)

```csharp
var writer = DynamoItemWriterRegistry.Get<UserProfile>();

var user = new UserProfile("123", "john@example.com", "John", 30);

var result = await client.PutItemAsync("Users", user, writer);
```

This serializes the item directly to DynamoDB JSON wire format — no intermediate `DynamoRecord` allocation.

---

## Pagination

### Auto-paginating Query

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

await foreach (var profile in client.QueryAllAsync("Users", reader, b => b
    .WithKeyCondition("PK = :pk")
    .WithExpressionValue(":pk", "USER#123")))
{
    Console.WriteLine(profile.DisplayName);
}
```

### Auto-paginating Scan

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

await foreach (var profile in client.ScanAllAsync("Users", reader, b => b
    .WithFilterExpression("Age > :minAge")
    .WithExpressionValue(":minAge", "18")))
{
    Console.WriteLine(profile.DisplayName);
}
```

### Auto-paginating BatchGet

```csharp
var reader = DynamoItemReaderRegistry.Get<UserProfile>();

await foreach (var (tableName, profile) in client.BatchGetAllAsync(reader, b => b
    .AddTable("Users", keys)))
{
    Console.WriteLine(profile.DisplayName);
}
```

This automatically retries unprocessed keys until all items are retrieved.

---

## Registry-Based Usage

The source generator automatically registers readers and writers for all `[DynamoModel]` types when you call `DynamoReaderRegistration.Initialize()`.

```csharp
// At startup
DynamoReaderRegistration.Initialize();

// Later — get reader/writer from registry
var reader = DynamoItemReaderRegistry.Get<UserProfile>();
var writer = DynamoItemWriterRegistry.Get<UserProfile>();

// Use them
var result = await client.QueryAsync(request, reader);
await client.PutItemAsync("Users", user, writer);
```

### Custom overrides

You can replace any generated converter by calling `Register<T>` after initialization:

```csharp
DynamoReaderRegistration.Initialize();

// Override with custom implementation
DynamoItemReaderRegistry.Register<UserProfile>(CustomUserProfileReader.Read);
DynamoItemWriterRegistry.Register<UserProfile>(CustomUserProfileWriter.Write);
```

---

## Migration from Untyped API

The untyped API (`QueryAsync` returning `QueryResponse` with `List<DynamoRecord>`) remains fully supported. You can migrate incrementally.

### Before (untyped)

```csharp
var response = await client.QueryAsync(request);
foreach (var record in response.Value.Items)
{
    var name = record["DisplayName"]?.S;
    var age = int.Parse(record["Age"]?.N ?? "0");
}
```

### After (typed)

```csharp
var result = await client.QueryAsync(request, DynamoItemReaderRegistry.Get<UserProfile>());
foreach (var profile in result.Value.Items)
{
    var name = profile.DisplayName;  // Strongly typed
    var age = profile.Age;           // Already an int
}
```

### Key differences

| Aspect | Untyped | Typed |
|--------|---------|-------|
| Return type | `QueryResponse` with `List<DynamoRecord>` | `QueryResult<T>` with `List<T>` |
| Allocations | Intermediate `DynamoRecord` dictionaries | Zero-copy, direct to object |
| Type safety | Manual string-keyed dictionary access | Compile-time checked properties |
| Performance | Good | Best (26x fewer allocations) |
| Schema | Flexible/dynamic | Requires `[DynamoModel]` |
