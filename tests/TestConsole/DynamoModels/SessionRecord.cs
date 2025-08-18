using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "SESSION#<SessionId>", SK = "TIMESTAMP#<ExpiresAt>")]
public record SessionRecord(
    string SessionId,

    [property: UnixTimestamp(Format = UnixTimestampFormat.Seconds)]
    DateTime ExpiresAt,

    string UserId
);
