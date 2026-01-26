namespace Goa.Clients.Bedrock.Conversation.Dynamo;

/// <summary>
/// Configuration for the DynamoDB-backed conversation store.
/// </summary>
public class DynamoConversationStoreConfiguration
{
    /// <summary>
    /// The name of the DynamoDB table to store conversations.
    /// </summary>
    public string TableName { get; set; } = "conversations";

    /// <summary>
    /// The name of the partition key attribute.
    /// </summary>
    public string PartitionKeyName { get; set; } = "PK";

    /// <summary>
    /// The name of the sort key attribute.
    /// </summary>
    public string SortKeyName { get; set; } = "SK";

    /// <summary>
    /// The name of the TTL attribute for automatic expiration.
    /// </summary>
    public string TtlAttributeName { get; set; } = "TTL";

    /// <summary>
    /// Function to format the partition key for a conversation.
    /// </summary>
    public Func<string, string> ConversationPkFormat { get; set; } = id => $"Conversation#{id}";

    /// <summary>
    /// The sort key value for conversation metadata records.
    /// </summary>
    public string ConversationSkValue { get; set; } = "_";

    /// <summary>
    /// Function to format the sort key for a message.
    /// </summary>
    public Func<string, int, string> MessageSkFormat => (_, seq) => $"{MessageSkPrefix}{seq:D10}";

    /// <summary>
    /// The prefix used for message sort keys, for querying with begins_with.
    /// Must match the prefix used in MessageSkFormat.
    /// </summary>
    public string MessageSkPrefix { get; set; } = "message#";

    /// <summary>
    /// Default TTL for conversations. If null, conversations do not expire.
    /// </summary>
    public TimeSpan? DefaultTtl { get; set; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Default limit for message retrieval when not specified.
    /// </summary>
    public int DefaultMessageLimit { get; set; } = 50;
}
