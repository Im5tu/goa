using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record TestModel(string? Id, bool? IsDeleted, IEnumerable<Test>? Destinations, Test? MyObject, Dictionary<string, string>? MyDataTest);