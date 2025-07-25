namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents a DynamoDB record with strongly-typed attribute values.
/// </summary>
public class DynamoRecord : Dictionary<string, AttributeValue>
{
    /// <summary>
    /// Gets or sets an attribute value by name.
    /// </summary>
    /// <param name="attributeName">The name of the attribute.</param>
    /// <returns>The attribute value or null if not found.</returns>
    public new AttributeValue? this[string attributeName]
    {
        get => base.TryGetValue(attributeName, out var attributeValue) ? attributeValue : null;
        set
        {
            if (value == null)
                Remove(attributeName);
            else
                base[attributeName] = value;
        }
    }
}
