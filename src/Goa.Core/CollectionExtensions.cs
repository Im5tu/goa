namespace Goa.Core;

// TODO :: Fix comments in project

/// <summary>
/// Provides extension methods for collections, enabling additional functionality.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Merges the specified key-value pairs into the source dictionary, overriding existing keys.
    /// </summary>
    /// <param name="source">The source dictionary to merge into.</param>
    /// <param name="items">The key-value pairs to merge into the source dictionary.</param>
    /// <typeparam name="TDictionary">The type of the source dictionary, which must implement <see cref="IReadOnlyDictionary{TKey, TValue}"/>.</typeparam>
    /// <typeparam name="TKey">The type of the keys in the dictionary. Keys must be non-null.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <returns>A new dictionary containing the merged key-value pairs.</returns>
    public static IReadOnlyDictionary<TKey, TValue> Merge<TDictionary, TKey, TValue>(this TDictionary source, IEnumerable<KeyValuePair<TKey, TValue>> items)
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        if (source is not Dictionary<TKey, TValue> result)
        {
            result = new Dictionary<TKey, TValue>(source);
        }

        foreach (var (key, value) in items)
        {
            result[key] = value;
        }
        return result;
    }
}
