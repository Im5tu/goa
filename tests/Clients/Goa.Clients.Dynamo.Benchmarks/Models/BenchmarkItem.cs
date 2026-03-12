using Goa.Clients.Dynamo;

namespace Goa.Clients.Dynamo.Benchmarks.Models;

[DynamoModel(PK = "<Pk>", SK = "<Sk>", PKName = "pk", SKName = "sk")]
public record BenchmarkItem(
    string Pk,
    string Sk,
    [property: SerializedName("data")] string Data,
    [property: SerializedName("number")] int Number,
    [property: SerializedName("status")] string Status
);
