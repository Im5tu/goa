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