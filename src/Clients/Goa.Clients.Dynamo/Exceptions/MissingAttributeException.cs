namespace Goa.Clients.Dynamo.Exceptions;

/// <summary>
/// Exception thrown when a required DynamoDB attribute is missing or null.
/// </summary>
public class MissingAttributeException : Exception
{
    /// <summary>
    /// Gets the name of the missing attribute.
    /// </summary>
    public string AttributeName { get; }
    
    /// <summary>
    /// Gets the partition key value for context.
    /// </summary>
    public string? PartitionKey { get; }
    
    /// <summary>
    /// Gets the sort key value for context.
    /// </summary>
    public string? SortKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingAttributeException"/> class.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <param name="partitionKey">The partition key value for context.</param>
    /// <param name="sortKey">The sort key value for context.</param>
    public MissingAttributeException(string attributeName, string? partitionKey = null, string? sortKey = null)
        : base(FormatMessage(attributeName, partitionKey, sortKey))
    {
        AttributeName = attributeName;
        PartitionKey = partitionKey;
        SortKey = sortKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingAttributeException"/> class with a custom message.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="partitionKey">The partition key value for context.</param>
    /// <param name="sortKey">The sort key value for context.</param>
    public MissingAttributeException(string attributeName, string message, string? partitionKey = null, string? sortKey = null)
        : base(message)
    {
        AttributeName = attributeName;
        PartitionKey = partitionKey;
        SortKey = sortKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingAttributeException"/> class with an inner exception.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="partitionKey">The partition key value for context.</param>
    /// <param name="sortKey">The sort key value for context.</param>
    public MissingAttributeException(string attributeName, Exception innerException, string? partitionKey = null, string? sortKey = null)
        : base(FormatMessage(attributeName, partitionKey, sortKey), innerException)
    {
        AttributeName = attributeName;
        PartitionKey = partitionKey;
        SortKey = sortKey;
    }

    /// <summary>
    /// Throws a <see cref="MissingAttributeException"/> for a missing attribute without PK/SK context.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <returns>Never returns (always throws).</returns>
    /// <exception cref="MissingAttributeException">Always thrown.</exception>
    public static T Throw<T>(string attributeName)
    {
        throw new MissingAttributeException(attributeName);
    }

    /// <summary>
    /// Throws a <see cref="MissingAttributeException"/> for a missing attribute with PK/SK context.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <param name="partitionKey">The partition key value for context.</param>
    /// <param name="sortKey">The sort key value for context.</param>
    /// <returns>Never returns (always throws).</returns>
    /// <exception cref="MissingAttributeException">Always thrown.</exception>
    public static T Throw<T>(string attributeName, string? partitionKey, string? sortKey)
    {
        throw new MissingAttributeException(attributeName, partitionKey, sortKey);
    }

    /// <summary>
    /// Throws a <see cref="MissingAttributeException"/> for a missing attribute without PK/SK context.
    /// Non-generic version for void contexts.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <exception cref="MissingAttributeException">Always thrown.</exception>
    public static void Throw(string attributeName)
    {
        throw new MissingAttributeException(attributeName);
    }

    /// <summary>
    /// Throws a <see cref="MissingAttributeException"/> for a missing attribute with PK/SK context.
    /// Non-generic version for void contexts.
    /// </summary>
    /// <param name="attributeName">The name of the missing attribute.</param>
    /// <param name="partitionKey">The partition key value for context.</param>
    /// <param name="sortKey">The sort key value for context.</param>
    /// <exception cref="MissingAttributeException">Always thrown.</exception>
    public static void Throw(string attributeName, string? partitionKey, string? sortKey)
    {
        throw new MissingAttributeException(attributeName, partitionKey, sortKey);
    }

    private static string FormatMessage(string attributeName, string? partitionKey, string? sortKey)
    {
        var message = $"Required attribute '{attributeName}' is missing or null";
        
        if (!string.IsNullOrEmpty(partitionKey) || !string.IsNullOrEmpty(sortKey))
        {
            message += " in record";
            if (!string.IsNullOrEmpty(partitionKey))
                message += $" PK: {partitionKey}";
            if (!string.IsNullOrEmpty(sortKey))
                message += string.IsNullOrEmpty(partitionKey) ? $" SK: {sortKey}" : $", SK: {sortKey}";
        }
        
        return message + ".";
    }
}