using Goa.Clients.Dynamo;

namespace TestConsole.DynamoModels;

[DynamoModel(PK = "INVOICE#<Id>", SK = "METADATA")]
[GlobalSecondaryIndex(Name = "CustomerIndex", PK = "CUSTOMER#<CustomerId>", SK = "INVOICE#<Id>")]
public class Invoice : BaseDocument
{
    public string CustomerId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsPaid { get; set; }
    public List<string> LineItems { get; set; } = new();
}
