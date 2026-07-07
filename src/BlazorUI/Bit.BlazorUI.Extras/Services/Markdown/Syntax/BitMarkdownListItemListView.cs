namespace Bit.BlazorUI;

/// <summary>
/// A live <see cref="IList{BitMarkdownViewerMarkdownNode}"/> view over a <see cref="BitMarkdownViewerListNode"/>'s strongly
/// typed <see cref="BitMarkdownViewerListNode.Items"/>. Exposing the real collection (instead of a copy)
/// lets generic AST processors add, remove, or replace list items and have those edits
/// reflected on the source. Non-<see cref="BitMarkdownViewerListItemNode"/> insertions are rejected.
/// </summary>
internal sealed class BitMarkdownViewerListItemListView(List<BitMarkdownViewerListItemNode> items) : IList<BitMarkdownViewerMarkdownNode>
{
    private static BitMarkdownViewerListItemNode AsItem(BitMarkdownViewerMarkdownNode node)
        => node as BitMarkdownViewerListItemNode ?? throw new ArgumentException($"A {nameof(BitMarkdownViewerListNode)} can only contain {nameof(BitMarkdownViewerListItemNode)} children.", nameof(node));

    public BitMarkdownViewerMarkdownNode this[int index]
    {
        get => items[index];
        set => items[index] = AsItem(value);
    }

    public int Count => items.Count;
    public bool IsReadOnly => false;
    public void Add(BitMarkdownViewerMarkdownNode item) => items.Add(AsItem(item));
    public void Clear() => items.Clear();
    public bool Contains(BitMarkdownViewerMarkdownNode item) => item is BitMarkdownViewerListItemNode li && items.Contains(li);
    public void CopyTo(BitMarkdownViewerMarkdownNode[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < items.Count)
            throw new ArgumentException("The destination array has insufficient space to copy the list items.", nameof(array));
        foreach (var i in items) array[arrayIndex++] = i;
    }
    public int IndexOf(BitMarkdownViewerMarkdownNode item) => item is BitMarkdownViewerListItemNode li ? items.IndexOf(li) : -1;
    public void Insert(int index, BitMarkdownViewerMarkdownNode item) => items.Insert(index, AsItem(item));
    public bool Remove(BitMarkdownViewerMarkdownNode item) => item is BitMarkdownViewerListItemNode li && items.Remove(li);
    public void RemoveAt(int index) => items.RemoveAt(index);
    public IEnumerator<BitMarkdownViewerMarkdownNode> GetEnumerator() { foreach (var i in items) yield return i; }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
