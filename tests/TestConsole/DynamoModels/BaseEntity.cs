using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK", TypeName = "EntityType")]
public abstract record BaseEntity(
    string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive
);

// TODO :: Add in the type field. Add an attribute to override the name of the type field just in case users want to call a property "Type" or something

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record TestEntity(string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive,
    IEnumerable<ThirdEntity> ListOfType,
    SecondaryEntity Something) : BaseEntity(Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsActive);

public record SecondaryEntity(string Test, ThirdEntity Third);

public record ThirdEntity(string Test);

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record TestModel(string? Id, bool? IsDeleted, IEnumerable<Test>? Destinations, Test? MyObject, Dictionary<string, string>? MyDataTest);

public record Test(long? Id, Dictionary<string, string>? Data);
