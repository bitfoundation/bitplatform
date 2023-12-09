namespace System.Collections.Generic;

public static class ICollectionExtensions
{
    // Basically a Polyfill since we now expose IList instead of List
    // which is better but IList doesn't have AddRange
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(list);
        ArgumentNullException.ThrowIfNull(items);

        if (list is List<T> asList)
        {
            asList.AddRange(items);
            return;
        }

        foreach (T item in items)
        {
            list.Add(item);
        }
    }

    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results;
    }
}
