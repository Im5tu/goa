namespace Goa.Clients.Dynamo;

/// <summary>
/// Registry for DynamoItemReader delegates, populated by source generators.
/// </summary>
public static class DynamoItemReaderRegistry
{
    /// <summary>
    /// Registers a reader delegate for the specified type.
    /// </summary>
    /// <typeparam name="T">The type the reader deserializes.</typeparam>
    /// <param name="reader">The reader delegate.</param>
    public static void Register<T>(DynamoItemReader<T> reader) => Cache<T>.Reader = reader;

    /// <summary>
    /// Gets the registered reader delegate for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to get the reader for.</typeparam>
    /// <returns>The registered reader delegate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no reader is registered for the type.</exception>
    public static DynamoItemReader<T> Get<T>() => Cache<T>.Reader
        ?? throw new InvalidOperationException($"No DynamoItemReader registered for {typeof(T).Name}. Ensure the type has [DynamoModel] attribute and the source generator has run.");

    private static class Cache<T>
    {
        public static volatile DynamoItemReader<T>? Reader;
    }
}
