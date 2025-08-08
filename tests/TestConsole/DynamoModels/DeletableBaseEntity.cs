namespace TestConsole.DynamoModels;

public abstract record DeletableBaseEntity(
    string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive,
    bool Deleted) : BaseEntity(Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsActive);