namespace Goa.Clients.Dynamo;

/// <summary>
/// Delegate that writes a single DynamoDB item to a UTF-8 JSON writer.
/// </summary>
/// <typeparam name="T">The type to serialize.</typeparam>
/// <param name="writer">The UTF-8 JSON writer to write to.</param>
/// <param name="item">The item to serialize.</param>
public delegate void DynamoItemWriter<in T>(System.Text.Json.Utf8JsonWriter writer, T item);
