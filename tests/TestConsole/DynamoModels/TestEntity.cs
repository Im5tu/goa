using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA", PKName = "PK", SKName = "SK")]
public record TestEntity(string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive,
    IEnumerable<ThirdEntity> ListOfType,
    SecondaryEntity Something) : BaseEntity(Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsActive);