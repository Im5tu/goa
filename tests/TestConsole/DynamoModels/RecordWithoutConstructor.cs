using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "RECORD#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record RecordWithoutConstructor
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int Count { get; init; }
    public bool IsActive { get; init; }
}