namespace Bit.BlazorUI;

/// <summary>
/// A live <see cref="IList{BitMarkdownNode}"/> view over a <see cref="BitMarkdownListNode"/>'s strongly
/// typed <see cref="BitMarkdownListNode.Items"/>. Exposing the real collection (instead of a copy)
/// lets generic AST processors add, remove, or replace list items and have those edits
/// reflected on the source. Non-<see cref="BitMarkdownListItemNode"/> insertions are rejected.
/// </summary>
internal sealed class BitMarkdownListItemListView(List<BitMarkdownListItemNode> items) : IList<BitMarkdownNode>
{
    private static BitMarkdownListItemNode AsItem(BitMarkdownNode node)
        => node as BitMarkdownListItemNode ?? throw new ArgumentException($"A {nameof(BitMarkdownListNode)} can only contain {nameof(BitMarkdownListItemNode)} children.", nameof(node));

    public BitMarkdownNode this[int index]
    {
        get => items[index];
        set => items[index] = AsItem(value);
    }

    public int Count => items.Count;
    public bool IsReadOnly => false;
    public void Add(BitMarkdownNode item) => items.Add(AsItem(item));
    public void Clear() => items.Clear();
    public bool Contains(BitMarkdownNode item) => item is BitMarkdownListItemNode li && items.Contains(li);
    public void CopyTo(BitMarkdownNode[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < items.Count)
            throw new ArgumentException("The destination array has insufficient space to copy the list items.", nameof(array));
        foreach (var i in items) array[arrayIndex++] = i;
    }
    public int IndexOf(BitMarkdownNode item) => item is BitMarkdownListItemNode li ? items.IndexOf(li) : -1;
    public void Insert(int index, BitMarkdownNode item) => items.Insert(index, AsItem(item));
    public bool Remove(BitMarkdownNode item) => item is BitMarkdownListItemNode li && items.Remove(li);
    public void RemoveAt(int index) => items.RemoveAt(index);
    public IEnumerator<BitMarkdownNode> GetEnumerator() { foreach (var i in items) yield return i; }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
