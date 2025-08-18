using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "CACHE#<Key>", SK = "ITEM")]
public record CacheRecord(
    string Key,
    string Value,

    [property: UnixTimestamp] // Defaults to seconds - perfect for TTL
    DateTime TTL
);
