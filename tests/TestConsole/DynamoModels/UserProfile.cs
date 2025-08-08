using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[GlobalSecondaryIndex(Name = "EmailIndex", PK = "EMAIL#<Email>", SK = "USER#<Id>")]
[GlobalSecondaryIndex(Name = "StatusIndex", PK = "STATUS#<Status>", SK = "USER#<Id>", PKName = "StatusPK", SKName = "StatusSK")]
public record UserProfile(
    string Id,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    bool IsActive,
    string Email,
    string FirstName,
    string LastName,
    UserStatus Status,
    int LoginCount,
    DateTime? LastLoginAt
) : BaseEntity(Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsActive);