using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "CLASS#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public class NormalClassModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Count { get; set; }
    public bool IsActive { get; set; }
}