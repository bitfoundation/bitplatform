using System.Collections;

namespace Bit.BlazorHeadlessUI;

/// <summary>A value paired with its position in a list (used for virtualized option lists).</summary>
/// <typeparam name="T">The value type.</typeparam>
public readonly record struct BitBhIndexed<T>(int Index, T Value);

/// <summary>
/// A zero-allocation, index-preserving view over a list, used to feed
/// <c>Virtualize</c> while keeping each item's position (for ids and
/// <c>aria-posinset</c>).
/// </summary>
internal sealed class BitBhIndexedList<T> : IReadOnlyList<BitBhIndexed<T>>, ICollection<BitBhIndexed<T>>
{
    private readonly IReadOnlyList<T> _items;

    public BitBhIndexedList(IReadOnlyList<T> items) => _items = items;

    public int Count => _items.Count;

    public bool IsReadOnly => true;

    public BitBhIndexed<T> this[int index] => new(index, _items[index]);

    public IEnumerator<BitBhIndexed<T>> GetEnumerator()
    {
        for (var i = 0; i < _items.Count; i++)
            yield return new BitBhIndexed<T>(i, _items[i]);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Contains(BitBhIndexed<T> item) => item.Index >= 0 && item.Index < _items.Count;

    public void CopyTo(BitBhIndexed<T>[] array, int arrayIndex)
    {
        for (var i = 0; i < _items.Count; i++)
            array[arrayIndex + i] = new BitBhIndexed<T>(i, _items[i]);
    }

    public void Add(BitBhIndexed<T> item) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Remove(BitBhIndexed<T> item) => throw new NotSupportedException();
}
