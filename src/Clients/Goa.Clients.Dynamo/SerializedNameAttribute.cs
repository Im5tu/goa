namespace Goa.Clients.Dynamo;

/// <summary>
/// Overrides the property name used in DynamoDB records.
/// By default, property names are used as-is for DynamoDB attribute names.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SerializedNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the SerializedNameAttribute class.
    /// </summary>
    /// <param name="name">The name to use for this property in DynamoDB records.</param>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public SerializedNameAttribute(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Serialized name cannot be null or empty.", nameof(name));
            
        Name = name;
    }

    /// <summary>
    /// The name to use for this property in DynamoDB records.
    /// </summary>
    public string Name { get; }
}