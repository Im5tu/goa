namespace Goa.Clients.Dynamo.Generator.Models;

/// <summary>
/// Represents Global Secondary Index attribute information.
/// </summary>
public class GSIAttributeInfo : AttributeInfo
{
    public string IndexName { get; set; } = string.Empty;
    public string PK { get; set; } = string.Empty;
    public string SK { get; set; } = string.Empty;
    public string? PKName { get; set; }
    public string? SKName { get; set; }
}