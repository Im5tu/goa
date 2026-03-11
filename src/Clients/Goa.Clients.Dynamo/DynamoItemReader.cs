namespace Goa.Clients.Dynamo;

/// <summary>
/// Delegate that reads a single DynamoDB item from a UTF-8 JSON reader.
/// </summary>
/// <typeparam name="T">The type to deserialize the item into.</typeparam>
/// <param name="reader">The UTF-8 JSON reader positioned at the start of the item object.</param>
/// <returns>The deserialized item.</returns>
public delegate T DynamoItemReader<out T>(ref System.Text.Json.Utf8JsonReader reader);
