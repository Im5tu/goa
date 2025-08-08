using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "REPORT#<Id>", SK = "METADATA")]
public class Report : BaseDocument
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime? GeneratedAt { get; set; }
    public int PageCount { get; set; }
    public List<string> Recipients { get; set; } = new();
}