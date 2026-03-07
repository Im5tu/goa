namespace Goa.Clients.Dynamo;

/// <summary>
/// Registry for DynamoItemWriter delegates, populated by source generators.
/// </summary>
public static class DynamoItemWriterRegistry
{
    /// <summary>
    /// Registers a writer delegate for the specified type.
    /// </summary>
    /// <typeparam name="T">The type the writer serializes.</typeparam>
    /// <param name="writer">The writer delegate.</param>
    public static void Register<T>(DynamoItemWriter<T> writer) => Cache<T>.Writer = writer;

    /// <summary>
    /// Gets the registered writer delegate for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get the writer for.</typeparam>
    /// <returns>The registered writer delegate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no writer is registered for the type.</exception>
    public static DynamoItemWriter<T> Get<T>() => Cache<T>.Writer
        ?? throw new InvalidOperationException($"No DynamoItemWriter registered for {typeof(T).Name}. Ensure the type has [DynamoModel] attribute and the source generator has run.");

    private static class Cache<T>
    {
        public static DynamoItemWriter<T>? Writer;
    }
}
