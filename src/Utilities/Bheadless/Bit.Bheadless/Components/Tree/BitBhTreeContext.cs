namespace Bit.Bheadless;

/// <summary>How items of a <see cref="BitBhTree{TValue}"/> can be selected.</summary>
public enum BitBhTreeSelectionMode
{
    /// <summary>No selection; items can only be focused and activated.</summary>
    None,

    /// <summary>One selected item (<c>aria-selected</c>).</summary>
    Single,

    /// <summary>Many selected items (<c>aria-multiselectable</c>); Ctrl / Shift extend the selection.</summary>
    Multiple,
}

/// <summary>
/// The state of a <see cref="BitBhTree{TValue}"/>, cascaded (fixed) to its items:
/// registered items and their hierarchy, expansion, selection and focus.
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public sealed class BitBhTreeContext<TValue>
{
    private readonly BitBhTree<TValue> _tree;
    private int _structureVersion = -1;
    private List<BitBhTreeItem<TValue>> _roots = new();
    private Dictionary<BitBhTreeItem<TValue>, List<BitBhTreeItem<TValue>>> _children = new();
    private Dictionary<BitBhTreeItem<TValue>, int> _positions = new();

    internal BitBhTreeContext(BitBhTree<TValue> tree, string key)
    {
        _tree = tree;
        Items = new BitBhCollection<BitBhTreeItem<TValue>>(key);
    }

    internal BitBhTree<TValue> Tree => _tree;

    /// <summary>Every registered item, in document order.</summary>
    public BitBhCollection<BitBhTreeItem<TValue>> Items { get; }

    /// <summary>The selection mode.</summary>
    public BitBhTreeSelectionMode SelectionMode => _tree.SelectionMode;

    /// <summary>Whether items show tri-state checkboxes (hierarchical selection).</summary>
    public bool Checkboxes => _tree.Checkboxes;

    /// <summary>The item that currently owns the tab stop.</summary>
    public BitBhTreeItem<TValue>? FocusedItem { get; internal set; }

    /// <summary>Whether <paramref name="value"/> is expanded.</summary>
    public bool IsExpanded(TValue value) => _tree.IsExpanded(value);

    /// <summary>Whether <paramref name="value"/> is selected.</summary>
    public bool IsSelected(TValue value) => _tree.IsSelected(value);

    /// <summary>The children of <paramref name="parent"/> (null for top-level items), in document order.</summary>
    public IEnumerable<BitBhTreeItem<TValue>> ChildrenOf(BitBhTreeItem<TValue>? parent)
    {
        EnsureStructure();
        return parent is null ? _roots : _children.TryGetValue(parent, out var children) ? children : [];
    }

    /// <summary>The 1-based position of <paramref name="item"/> among its siblings and their count.</summary>
    internal (int Position, int Count) PositionOf(BitBhTreeItem<TValue> item)
    {
        EnsureStructure();
        return _positions.TryGetValue(item, out var index) ? (index + 1, ChildrenOf(item.ParentItem).Count()) : (0, 0);
    }

    // Every item asks for its siblings and children while rendering: scanning all items each time made a
    // render O(n^2). The hierarchy only changes with the collection (an item's parent is fixed), so it is
    // built once per collection version. New lists each time: a caller may still be enumerating the old ones.
    private void EnsureStructure()
    {
        if (_structureVersion == Items.Version) return;
        var roots = new List<BitBhTreeItem<TValue>>();
        var children = new Dictionary<BitBhTreeItem<TValue>, List<BitBhTreeItem<TValue>>>();
        var positions = new Dictionary<BitBhTreeItem<TValue>, int>();
        foreach (var item in Items.Items)
        {
            List<BitBhTreeItem<TValue>>? siblings;
            if (item.ParentItem is null) siblings = roots;
            else if (children.TryGetValue(item.ParentItem, out siblings) is false) children[item.ParentItem] = siblings = new();
            positions[item] = siblings.Count;
            siblings.Add(item);
        }
        (_roots, _children, _positions, _structureVersion) = (roots, children, positions, Items.Version);
    }

    /// <summary>Every descendant of <paramref name="item"/>.</summary>
    public IEnumerable<BitBhTreeItem<TValue>> DescendantsOf(BitBhTreeItem<TValue> item)
    {
        foreach (var child in ChildrenOf(item))
        {
            yield return child;
            foreach (var d in DescendantsOf(child)) yield return d;
        }
    }

    /// <summary>Items whose ancestors are all expanded, in document order.</summary>
    public IEnumerable<BitBhTreeItem<TValue>> VisibleItems()
        => Items.Items.Where(i => i.AncestorsExpanded);

    /// <summary>The checkbox state of <paramref name="item"/>: true, false or null (mixed).</summary>
    public bool? CheckedState(BitBhTreeItem<TValue> item)
    {
        var leaves = DescendantsOf(item).Where(d => !ChildrenOf(d).Any()).ToList();
        if (leaves.Count == 0) return IsSelected(item.Value);
        var checkedCount = leaves.Count(l => IsSelected(l.Value));
        return checkedCount == 0 ? false : checkedCount == leaves.Count ? true : null;
    }
}

/// <summary>Render-fragment context exposed by <see cref="BitBhTreeItem{TValue}"/>.</summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public sealed record BitBhTreeItemRenderContext<TValue>
{
    /// <summary>The item's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Depth, starting at 1.</summary>
    public required int Level { get; init; }

    /// <summary>Whether the item has (or may lazily load) children.</summary>
    public required bool IsExpandable { get; init; }

    /// <summary>Whether the item is expanded.</summary>
    public required bool IsExpanded { get; init; }

    /// <summary>Whether the item is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Checkbox state: true, false or null (mixed). Only meaningful with checkboxes.</summary>
    public required bool? Checked { get; init; }

    /// <summary>Whether the item has keyboard focus (tab stop).</summary>
    public required bool IsFocused { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether children are being loaded.</summary>
    public required bool IsLoading { get; init; }

    /// <summary>Toggles expansion.</summary>
    public required Func<Task> ToggleExpanded { get; init; }
}
