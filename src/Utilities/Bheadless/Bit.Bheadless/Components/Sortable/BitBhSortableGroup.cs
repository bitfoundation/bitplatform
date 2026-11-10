using System.Runtime.CompilerServices;

namespace Bit.Bheadless;

/// <summary>
/// The lists that exchange items: every <see cref="BitBhSortable{TItem}"/> with the same <c>Group</c> (and item type)
/// in one scope - a circuit, a WebAssembly app - shares one, and a list without a group has one of its own. It holds
/// the keyboard grab, which can move from list to list.
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
internal sealed class BitBhSortableGroup<TItem>
{
    // Keyed by the scope's interop: one registry per circuit / WebAssembly app (per test context in bUnit), never
    // shared between the users of a server. Static per TItem, so lists of different item types never meet.
    private static readonly ConditionalWeakTable<BitBhInterop, Dictionary<string, BitBhSortableGroup<TItem>>> Registry = new();

    private readonly Dictionary<string, BitBhSortableGroup<TItem>>? _registry;

    private BitBhSortableGroup(string? name, Dictionary<string, BitBhSortableGroup<TItem>>? registry)
    {
        Name = name;
        _registry = registry;
    }

    /// <summary>The group key; null for a list on its own.</summary>
    public string? Name { get; }

    /// <summary>Whether lists exchange items through this group.</summary>
    public bool IsShared => Name is not null;

    /// <summary>The lists, in the order they joined (the fallback order of the keys that move between them).</summary>
    public List<BitBhSortable<TItem>> Members { get; } = [];

    /// <summary>The keyboard grab in progress, if any.</summary>
    public BitBhSortableGrab<TItem>? Grab { get; set; }

    /// <summary>A group of its own for a list without a <c>Group</c>.</summary>
    public static BitBhSortableGroup<TItem> Own(BitBhSortable<TItem> list)
    {
        var group = new BitBhSortableGroup<TItem>(null, null);
        group.Members.Add(list);
        return group;
    }

    /// <summary>Adds <paramref name="list"/> to the group named <paramref name="name"/> of the scope.</summary>
    public static BitBhSortableGroup<TItem> Join(BitBhInterop scope, string name, BitBhSortable<TItem> list)
    {
        var registry = Registry.GetValue(scope, static _ => new Dictionary<string, BitBhSortableGroup<TItem>>(StringComparer.Ordinal));
        if (registry.TryGetValue(name, out var group) is false)
            registry[name] = group = new BitBhSortableGroup<TItem>(name, registry);
        if (group.Members.Contains(list) is false) group.Members.Add(list);
        return group;
    }

    /// <summary>Removes <paramref name="list"/>; the group is forgotten with its last list.</summary>
    public void Leave(BitBhSortable<TItem> list)
    {
        Members.Remove(list);
        if (Members.Count == 0 && Name is not null && _registry is not null && _registry.TryGetValue(Name, out var current) && current == this)
            _registry.Remove(Name);
    }

    /// <summary>
    /// Ends the keyboard grab: every list shows its own items again. The lists the item is not in and did not come
    /// from re-render now (their handles' keys change); the caller renders those two once it has committed or
    /// restored the move.
    /// </summary>
    public void EndGrab()
    {
        var grab = Grab;
        Grab = null;
        foreach (var list in Members.ToArray())
        {
            list.ClearPreview();
            if (grab is null || (list != grab.Origin && list != grab.Current)) list.Rerender();
        }
    }

    /// <summary>The list whose element id is <paramref name="id"/>.</summary>
    public BitBhSortable<TItem>? Find(string? id) => id is null ? null : Members.Find(m => m.ContainerId == id);
}

/// <summary>An item picked up with the keyboard: where it came from and where it is now.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
internal sealed class BitBhSortableGrab<TItem>(BitBhSortable<TItem> origin, int originIndex, TItem item)
{
    /// <summary>The list the item was picked up from.</summary>
    public BitBhSortable<TItem> Origin { get; } = origin;

    /// <summary>The item's position in <see cref="Origin"/> when it was picked up.</summary>
    public int OriginIndex { get; } = originIndex;

    /// <summary>The item.</summary>
    public TItem Item { get; } = item;

    /// <summary>The list that shows the item now.</summary>
    public BitBhSortable<TItem> Current { get; set; } = origin;

    /// <summary>The item's position in <see cref="Current"/>'s preview.</summary>
    public int Index { get; set; } = originIndex;
}
