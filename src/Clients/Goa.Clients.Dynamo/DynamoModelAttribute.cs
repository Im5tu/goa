namespace Goa.Clients.Dynamo;

/// <summary>
/// Marks a record as a DynamoDB model and specifies its primary key structure.
/// This attribute is inherited, allowing base classes to define common PK/SK patterns.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DynamoModelAttribute : Attribute
{
    /// <summary>
    /// The partition key pattern. Supports placeholders like "ENTITY#&lt;PropertyName&gt;" 
    /// where PropertyName will be replaced with the actual property value.
    /// </summary>
    public required string PK { get; init; }
    
    /// <summary>
    /// The sort key pattern. Supports placeholders like "DATA#&lt;PropertyName&gt;" 
    /// where PropertyName will be replaced with the actual property value.
    /// </summary>
    public required string SK { get; init; }
    
    /// <summary>
    /// The DynamoDB attribute name for the partition key. Defaults to "PK".
    /// </summary>
    public string PKName { get; init; } = "PK";
    
    /// <summary>
    /// The DynamoDB attribute name for the sort key. Defaults to "SK".
    /// </summary>
    public string SKName { get; init; } = "SK";

    /// <summary>
    /// The DynamoDB attribute name for the type discriminator field used in inheritance scenarios.
    /// This field is automatically set on concrete types that inherit from abstract types and is used
    /// during deserialization to determine which concrete type to instantiate. Defaults to "Type".
    /// </summary>
    public string TypeName { get; init; } = "Type";
}