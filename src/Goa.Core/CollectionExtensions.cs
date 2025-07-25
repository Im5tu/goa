namespace Goa.Core;

// TODO :: Fix comments in project

/// <summary>
///
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="source"></param>
    /// <param name="items"></param>
    /// <typeparam name="TDictionary"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
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
