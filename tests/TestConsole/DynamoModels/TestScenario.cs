using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

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

[DynamoModel(PK = "USER", SK = "_")]
[GlobalSecondaryIndex(Name = "GSI_1", PK = "<GSI_1_PK>", SK = "_")]
public abstract record BaseGsi()
{
    public virtual string? GSI_1_PK { get; }
}

public record DerivedGsi : BaseGsi
{
    public string Test => "Test";
    public override string? GSI_1_PK => "<Test>";
}
