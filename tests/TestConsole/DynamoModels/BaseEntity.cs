using Goa.Clients.Dynamo;
using Goa.Clients.Dynamo.Extensions;
using Goa.Clients.Dynamo.Models;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public abstract record BaseEntity(
    string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive
);


[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record TestEntity(string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive,
    SecondaryEntity Something) : BaseEntity(Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsActive);

public record SecondaryEntity(string Test, ThirdEntity Third);

public record ThirdEntity(string Test);
