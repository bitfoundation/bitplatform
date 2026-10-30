namespace Bit.BlazorHeadlessUI;

/// <summary>An item that participates in a <see cref="BitBhCollection{TItem}"/>.</summary>
public interface IBitBhCollectionItem
{
    /// <summary>The item's DOM id (stable for the item's lifetime). Used for <c>aria-activedescendant</c> and focus.</summary>
    string ItemId { get; }

    /// <summary>Whether the item is disabled and should be skipped by keyboard navigation.</summary>
    bool IsItemDisabled { get; }

    /// <summary>Plain text used for typeahead and announcements.</summary>
    string? ItemText { get; }
}

/// <summary>
/// An ordered registry of child items (options, menu items, tabs, tree items …)
/// shared through a cascading context. Items register when initialized and
/// unregister when disposed.
///
/// <para>
/// <b>Ordering.</b> Blazor initializes nested components in render-queue order,
/// which is not always document order (items inside groups initialize after
/// their siblings). Items therefore render <c>data-bit-bh-item="{Key}"</c> and call
/// <see cref="EnsureOrderAsync"/> after their first render; the collection then
/// asks the browser for the document order once per membership change and
/// re-sorts itself. Without a browser (prerendering, unit tests) registration
/// order is used.
/// </para>
///
/// <para>
/// Items are tracked by reference, never by index, so adding or removing items
/// (filtering a combobox, lazy tree children) never desynchronizes ids, the
/// active item, or <c>aria-activedescendant</c>.
/// </para>
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed class BitBhCollection<TItem> where TItem : class, IBitBhCollectionItem
{
    private readonly List<TItem> _items = new();
    private int _version;
    private int _syncedVersion;
    private Task? _syncTask;
    private Dictionary<string, string>? _domText;

    /// <summary>Creates a collection identified by <paramref name="key"/>.</summary>
    public BitBhCollection(string key)
    {
        Key = key;
    }

    /// <summary>The value of the <c>data-bit-bh-item</c> attribute rendered by members.</summary>
    public string Key { get; }

    /// <summary>The registered items in (best known) document order.</summary>
    public IReadOnlyList<TItem> Items => _items;

    /// <summary>The number of registered items.</summary>
    public int Count => _items.Count;

    /// <summary>Incremented whenever membership or order changes.</summary>
    public int Version => _version;

    /// <summary>Raised after items were added, removed or re-ordered.</summary>
    public event Action? Changed;

    /// <summary>Adds <paramref name="item"/> (no-op when already registered).</summary>
    public void Register(TItem item)
    {
        if (_items.Contains(item)) return;
        _items.Add(item);
        _version++;
        Changed?.Invoke();
    }

    /// <summary>Removes <paramref name="item"/>.</summary>
    public void Unregister(TItem item)
    {
        if (!_items.Remove(item)) return;
        _domText?.Remove(item.ItemId);
        _version++;
        // Removal never changes the relative order of the remaining items.
        if (_syncedVersion == _version - 1) _syncedVersion = _version;
        Changed?.Invoke();
    }

    /// <summary>Returns the index of <paramref name="item"/>, or -1.</summary>
    public int IndexOf(TItem? item) => item is null ? -1 : _items.IndexOf(item);

    /// <summary>Finds an item by its DOM id.</summary>
    public TItem? FindById(string? id)
        => string.IsNullOrEmpty(id) ? null : _items.Find(i => string.Equals(i.ItemId, id, StringComparison.Ordinal));

    /// <summary>
    /// Re-sorts the items into document order when membership changed since the
    /// last sync. Concurrent callers share one browser round-trip.
    /// </summary>
    public Task EnsureOrderAsync(BitBhInterop interop)
    {
        if (_syncedVersion == _version || _items.Count == 0)
            return Task.CompletedTask;
        if (_syncTask is not null)
            return _syncTask;
        // SyncAsync can complete synchronously (no browser, a disposed interop): its own cleanup then
        // runs before the assignment below, so a finished task is never kept as the one in flight.
        var task = SyncAsync(interop);
        if (task.IsCompleted is false) _syncTask = task;
        return task;
    }

    /// <summary>
    /// The rendered text of an item as last read from the DOM (used as the
    /// typeahead fallback when an item has no explicit label).
    /// </summary>
    public string? GetRenderedText(string id)
        => _domText is not null && _domText.TryGetValue(id, out var text) ? text : null;

    private async Task SyncAsync(BitBhInterop interop)
    {
        try
        {
            // Loop while members keep arriving during the round-trip.
            for (var attempt = 0; attempt < 3 && _syncedVersion != _version && _items.Count > 0; attempt++)
                await SyncOnceAsync(interop);
        }
        finally
        {
            _syncTask = null;
        }
    }

    private async Task SyncOnceAsync(BitBhInterop interop)
    {
        var target = _version;
        var entries = await interop.CollectionOrderAsync(Key);
        var changed = false;
        if (entries is { Length: > 0 })
        {
            var rank = new Dictionary<string, int>(entries.Length, StringComparer.Ordinal);
            // Rebuilt from what the page shows now, so the texts of members that left (a combobox re-filtered
            // on every keystroke) are not kept for the component's lifetime.
            var texts = new Dictionary<string, string>(entries.Length, StringComparer.Ordinal);
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] is not { Length: > 0 } entry || string.IsNullOrEmpty(entry[0])) continue;
                rank.TryAdd(entry[0], i);
                if (entry.Length > 1) texts[entry[0]] = entry[1];
            }
            changed = SameTexts(_domText, texts) is false;
            _domText = texts;

            var ordered = _items
                .Select((item, index) => (item, index))
                .OrderBy(x => rank.TryGetValue(x.item.ItemId, out var r) ? r : int.MaxValue)
                .ThenBy(x => x.index)
                .Select(x => x.item)
                .ToList();

            if (!ordered.SequenceEqual(_items))
            {
                var unchangedMembership = target == _version;
                _items.Clear();
                _items.AddRange(ordered);
                _version++;
                if (unchangedMembership) target = _version;
                changed = true;
            }
        }
        _syncedVersion = target;
        // Members re-render for a new order or new rendered texts (typeahead / labels), not for a sync that
        // found both as they were.
        if (changed) Changed?.Invoke();
    }

    private static bool SameTexts(Dictionary<string, string>? current, Dictionary<string, string> next)
    {
        if ((current?.Count ?? 0) != next.Count) return false;
        foreach (var (id, text) in next)
            if (current!.TryGetValue(id, out var known) is false || string.Equals(known, text, StringComparison.Ordinal) is false)
                return false;
        return true;
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    /// <summary>The first enabled item.</summary>
    public TItem? First() => _items.Find(i => !i.IsItemDisabled);

    /// <summary>The last enabled item.</summary>
    public TItem? Last() => _items.FindLast(i => !i.IsItemDisabled);

    /// <summary>The next enabled item after <paramref name="from"/> (or the first when null).</summary>
    public TItem? Next(TItem? from, bool loop = true) => Step(from, +1, loop);

    /// <summary>The previous enabled item before <paramref name="from"/> (or the last when null).</summary>
    public TItem? Previous(TItem? from, bool loop = true) => Step(from, -1, loop);

    /// <summary>Moves <paramref name="count"/> enabled items forward (positive) or back, clamping at the ends.</summary>
    public TItem? Offset(TItem? from, int count)
    {
        var current = from;
        var step = Math.Sign(count);
        for (var i = 0; i < Math.Abs(count); i++)
        {
            var next = Step(current, step, loop: false);
            if (next is null) break;
            current = next;
        }
        return current ?? (step > 0 ? First() : Last());
    }

    private TItem? Step(TItem? from, int step, bool loop)
    {
        if (_items.Count == 0) return null;
        var start = IndexOf(from);
        if (start < 0) return step > 0 ? First() : Last();

        var index = start;
        for (var attempts = 0; attempts < _items.Count; attempts++)
        {
            index += step;
            if (index >= _items.Count)
            {
                if (!loop) return null;
                index = 0;
            }
            else if (index < 0)
            {
                if (!loop) return null;
                index = _items.Count - 1;
            }
            if (!_items[index].IsItemDisabled) return _items[index];
        }
        return null;
    }
}
