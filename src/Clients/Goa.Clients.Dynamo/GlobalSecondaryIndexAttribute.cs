namespace Goa.Clients.Dynamo;

/// <summary>
/// Defines a Global Secondary Index for a DynamoDB model. Multiple GSIs can be defined per model (maximum 5).
/// This attribute is inherited, allowing base classes to define common GSI patterns.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class GlobalSecondaryIndexAttribute : Attribute
{
    /// <summary>
    /// The name of the Global Secondary Index in DynamoDB.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The GSI partition key pattern. Supports placeholders like "STATUS#&lt;PropertyName&gt;" 
    /// where PropertyName will be replaced with the actual property value.
    /// </summary>
    public required string PK { get; init; }
    
    /// <summary>
    /// The GSI sort key pattern. Supports placeholders like "DATE#&lt;PropertyName&gt;" 
    /// where PropertyName will be replaced with the actual property value.
    /// </summary>
    public required string SK { get; init; }
    
    /// <summary>
    /// The DynamoDB attribute name for the GSI partition key. 
    /// Defaults to "GSI_{IndexNumber}_PK" where IndexNumber is determined by declaration order.
    /// </summary>
    public string? PKName { get; init; }
    
    /// <summary>
    /// The DynamoDB attribute name for the GSI sort key. 
    /// Defaults to "GSI_{IndexNumber}_SK" where IndexNumber is determined by declaration order.
    /// </summary>
    public string? SKName { get; init; }
}