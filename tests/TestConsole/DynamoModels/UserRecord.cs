using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "USER#<Id>", SK = "METADATA")]
public record UserRecord(
    string Id,
    string PK,
    string SK,

    [property: UnixTimestamp(Format = UnixTimestampFormat.Seconds)]
    DateTime CreatedAt,

    [property: UnixTimestamp(Format = UnixTimestampFormat.Milliseconds)]
    DateTime? UpdatedAt,

    [property: UnixTimestamp(Format = UnixTimestampFormat.Seconds)]
    DateTimeOffset ExpiresAt,

    [property: SerializedName("user_name")]
    string Name
)
{
    // Add a computed property that shouldn't be persisted
    [Ignore(Direction = IgnoreDirection.Always)]
    public string ComputedField => $"{Name}_{Id}";
};