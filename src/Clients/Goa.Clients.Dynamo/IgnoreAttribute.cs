namespace Goa.Clients.Dynamo;

/// <summary>
/// Marks a property to be ignored during DynamoDB mapping.
/// By default, ignores the property in both directions (reading and writing).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class IgnoreAttribute : Attribute
{
    /// <summary>
    /// The direction in which to ignore the property. Defaults to Always.
    /// </summary>
    public IgnoreDirection Direction { get; init; } = IgnoreDirection.Always;
}