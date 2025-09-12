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

[DynamoModel(PK = "USER#<PK>", SK = "<SK>")]
[GlobalSecondaryIndex(Name = "GSI_1", PK = "USER#<PK>", SK = "<SK2>")]
[GlobalSecondaryIndex(Name = "GSI_2", PK = "USER#<PK>", SK = "<SK3>")]
[GlobalSecondaryIndex(Name = "GSI_3", PK = "USER#<PK>", SK = "<SK4>")]
[GlobalSecondaryIndex(Name = "GSI_4", PK = "USER#<PK>", SK = "<SK5>")]
public record TestScenario
{
    public string PK { get; set; } = "";
    public DateTime? SK { get; set; }
    public DateTime SK2 { get; set; }
    [UnixTimestamp]
    public DateTime SK3 { get; set; }
    [UnixTimestamp(Format = UnixTimestampFormat.Milliseconds)]
    public DateTime SK4 { get; set; }
    [UnixTimestamp]
    public DateTime? SK5 { get; set; }
}
