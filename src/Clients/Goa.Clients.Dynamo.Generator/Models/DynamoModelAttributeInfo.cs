namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Represents DynamoDB model attribute information.
/// </summary>
public class DynamoModelAttributeInfo : AttributeInfo
{
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string PKName { get; set; } = string.Empty;
    public string SKName { get; set; } = string.Empty;
    public string TypeName { get; set; } = "Type";
}