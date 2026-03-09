using EfficientDynamoDb.Attributes;

namespace Goa.Clients.Dynamo.Benchmarks.Models;

[DynamoDbTable("benchmark-table")]
public class BenchmarkEntity
{
    [DynamoDbProperty("pk", DynamoDbAttributeType.PartitionKey)]
    public string Pk { get; set; } = "";

    [DynamoDbProperty("sk", DynamoDbAttributeType.SortKey)]
    public string Sk { get; set; } = "";

    [DynamoDbProperty("data")]
    public string Data { get; set; } = "";

    [DynamoDbProperty("number")]
    public int Number { get; set; }

    [DynamoDbProperty("status")]
    public string Status { get; set; } = "";
}
