namespace Goa.Clients.Dynamo.Models;

/// <summary>
/// Represents a DynamoDB record with strongly-typed attribute values.
/// </summary>
public class DynamoRecord : Dictionary<string, AttributeValue>
{
    /// <summary>
    /// Initializes a new, empty instance of the <see cref="DynamoRecord"/> class.
    /// </summary>
    public DynamoRecord()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamoRecord"/> class
    /// by copying all key-value pairs from the specified dictionary.
    /// </summary>
    /// <param name="record">A dictionary containing DynamoDB attribute names and their values.</param>
    public DynamoRecord(Dictionary<string, AttributeValue> record)
    {
        foreach (var item in record)
            this[item.Key] = item.Value;
    }

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
