namespace System.Collections.Generic;

public static partial class ICollectionExtensions
{
    extension<T>(IAsyncEnumerable<T> items)
    {
        public async Task<List<T>> ToListAsync(CancellationToken cancellationToken)
        {
            var results = new List<T>();
            await foreach (var item in items.WithCancellation(cancellationToken))
            {
                results.Add(item);
            }
            return results;
        }
    }

    extension<T>(IEnumerable<T> source)
    {
        public IEnumerable<(T item, int index)> Indexed()
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
