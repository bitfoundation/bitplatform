namespace System.Collections.Generic;

public static partial class ICollectionExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> items, CancellationToken cancellationToken)
    {
        var results = new List<T>();
        await foreach (var item in items.WithCancellation(cancellationToken))
        {
            results.Add(item);
        }
        return results;
    }
}
