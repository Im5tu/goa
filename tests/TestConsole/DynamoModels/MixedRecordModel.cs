using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "MIXED#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record MixedRecordModel(string Id, string Name, DateTime CreatedAt)
{
    public int Count { get; init; }
    public bool IsActive { get; init; }
    public string? Description { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
