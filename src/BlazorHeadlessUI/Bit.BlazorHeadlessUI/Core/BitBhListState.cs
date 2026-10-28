using System.Globalization;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The shared option-list engine behind <see cref="BitBhListbox{TValue}"/> and
/// <see cref="BitBhCombobox{TValue}"/>: option registry (or a data-driven item list
/// for virtualization), the highlighted ("active") option, keyboard navigation,
/// typeahead and selection queries.
///
/// <para>
/// The active option is tracked by element id, never by index, so options can be
/// added, removed, filtered or virtualized without the highlight or
/// <c>aria-activedescendant</c> drifting.
/// </para>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class BitBhListState<TValue>
{
    private readonly BitBhInterop _interop;
    private readonly Func<TValue, bool> _isSelected;
    private readonly Action _refreshOwner;
    private readonly Func<IEqualityComparer<TValue>> _comparer;
    private readonly Dictionary<string, BitBhOptionBase<TValue>> _rendered = new(StringComparer.Ordinal);
    private bool _panelRevealed;
    private bool _scrollPending;
    private bool _hasActiveValue;
    private TValue _activeValue = default!;
    private bool _optionsUnmounted;
    private bool _optionsEverMounted;
    private (TValue Value, string? Text)[] _unmountedTexts = [];

    internal BitBhListState(string key, BitBhInterop interop, Func<TValue, bool> isSelected, Action refreshOwner, Func<IEqualityComparer<TValue>>? comparer = null)
    {
        Key = key;
        _interop = interop;
        _isSelected = isSelected;
        _refreshOwner = refreshOwner;
        _comparer = comparer ?? (static () => EqualityComparer<TValue>.Default);
        Options = new BitBhCollection<BitBhOptionBase<TValue>>(key);
    }

    /// <summary>The collection key (rendered as <c>data-bit-bh-item</c> on options).</summary>
    public string Key { get; }

    /// <summary>Registered option components (component-driven mode).</summary>
    public BitBhCollection<BitBhOptionBase<TValue>> Options { get; }

    /// <summary>The id of the highlighted option, or null.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The id of the options panel (scroll container).</summary>
    public string? PanelId { get; internal set; }

    internal BitBhTypeahead Typeahead { get; } = new();

    // ── Data-driven (virtualized) mode ───────────────────────────────────────

    internal IReadOnlyList<TValue>? Items { get; private set; }
    internal Func<TValue, bool>? ItemDisabled { get; private set; }
    internal Func<TValue, string?>? ItemText { get; private set; }
    internal float ItemSize { get; private set; }

    /// <summary>Whether options come from a data list (virtualization) instead of registered components.</summary>
    public bool IsDataMode => Items is not null;

    internal void SetItems(IReadOnlyList<TValue>? items, Func<TValue, bool>? disabled, Func<TValue, string?>? text, float itemSize)
    {
        Items = items;
        ItemDisabled = disabled;
        ItemText = text;
        ItemSize = itemSize;

        // Data option ids are positions: when the list changes (a new filter) the highlight follows its
        // item to the new position, or is dropped when the item is gone - never left on whatever moved in.
        if (items is not null && ActiveId is not null && _hasActiveValue)
        {
            var index = IndexOfValue(items, _activeValue, IndexOfId(ActiveId));
            SetActiveId(index >= 0 ? DataItemId(index) : null, scroll: false);
        }
    }

    private int IndexOfValue(IReadOnlyList<TValue> items, TValue value, int hint)
    {
        var comparer = _comparer();
        if (hint >= 0 && hint < items.Count && comparer.Equals(items[hint], value)) return hint;
        for (var i = 0; i < items.Count; i++)
            if (comparer.Equals(items[i], value)) return i;
        return -1;
    }

    /// <summary>The id of the data item at <paramref name="index"/> (data mode).</summary>
    public string DataItemId(int index) => $"{Key}-o{index}";

    // ── Model ────────────────────────────────────────────────────────────────

    /// <summary>The number of options.</summary>
    public int Count => Items?.Count ?? Options.Count;

    internal string IdAt(int index) => Items is not null ? DataItemId(index) : Options.Items[index].ItemId;

    internal TValue ValueAt(int index) => Items is not null ? Items[index] : Options.Items[index].Value;

    internal bool DisabledAt(int index)
        => Items is not null ? ItemDisabled?.Invoke(Items[index]) ?? false : Options.Items[index].IsItemDisabled;

    internal string? TextAt(int index)
        => Items is not null
            ? ItemText?.Invoke(Items[index]) ?? Items[index]?.ToString()
            : Options.Items[index].ItemText;

    internal int IndexOfId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        if (Items is not null)
        {
            var prefix = Key + "-o";
            return id.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(id.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var i) && i < Items.Count ? i : -1;
        }
        for (var i = 0; i < Options.Count; i++)
            if (Options.Items[i].ItemId == id) return i;
        return -1;
    }

    /// <summary>The index of the highlighted option, or -1.</summary>
    public int ActiveIndex => IndexOfId(ActiveId);

    /// <summary>Whether the highlighted option exists and is enabled.</summary>
    public bool HasActive => ActiveIndex >= 0;

    /// <summary>The value of the highlighted option.</summary>
    public bool TryGetActiveValue(out TValue value)
    {
        var index = ActiveIndex;
        if (index < 0) { value = default!; return false; }
        value = ValueAt(index);
        return true;
    }

    internal bool IsSelected(TValue value) => _isSelected(value);

    // ── Rendered option tracking (targeted refresh) ──────────────────────────

    internal void Attach(BitBhOptionBase<TValue> option)
    {
        _rendered[option.ItemId] = option;
        if (!IsDataMode) Options.Register(option);
    }

    internal void Detach(BitBhOptionBase<TValue> option, string id)
    {
        if (_rendered.TryGetValue(id, out var existing) && ReferenceEquals(existing, option))
            _rendered.Remove(id);
        Options.Unregister(option);
    }

    // ── Highlight ────────────────────────────────────────────────────────────

    /// <summary>Highlights the option with the given id (null clears the highlight).</summary>
    internal void SetActiveId(string? id, bool scroll = true)
    {
        if (ActiveId == id) return;
        var previous = ActiveId;
        ActiveId = id;
        var index = Items is not null ? IndexOfId(id) : -1;
        _hasActiveValue = index >= 0;
        _activeValue = index >= 0 ? Items![index] : default!;
        if (previous is not null && _rendered.TryGetValue(previous, out var p)) p.Refresh();
        if (id is not null && _rendered.TryGetValue(id, out var n)) n.Refresh();
        _refreshOwner();
        if (id is null)
            _scrollPending = false;
        else if (scroll && PanelId is not null)
        {
            // A hidden panel can't scroll (an anchored or portalled one stays hidden until the runtime has
            // positioned it): the scroll waits for the panel to be revealed.
            if (_panelRevealed) ScrollTo(id);
            else _scrollPending = true;
        }
    }

    private void ScrollTo(string id)
    {
        if (PanelId is null) return;
        if (IsDataMode)
            _ = _interop.ScrollToIndexAsync(PanelId, IndexOfId(id), ItemSize, id);
        else
            _ = _interop.ScrollIntoViewAsync(id);
    }

    /// <summary>Called by the options panel after each render: whether it is visible on the page.</summary>
    internal void SetPanelRevealed(bool revealed)
    {
        if (_panelRevealed == revealed) return;
        _panelRevealed = revealed;
        if (revealed && _scrollPending && ActiveId is { } id)
        {
            _scrollPending = false;
            ScrollTo(id);
        }
    }

    /// <summary>
    /// Called by an options panel that removes its options while closed (<c>Unmount</c>): their texts are
    /// kept, so the button still names the selection while the options are gone.
    /// </summary>
    internal void SetOptionsMounted(bool mounted)
    {
        _optionsEverMounted |= mounted;
        if (_optionsUnmounted != mounted) return;
        _optionsUnmounted = !mounted;
        _unmountedTexts = _optionsUnmounted ? Options.Items.Select(o => (o.Value, o.ItemText)).ToArray() : [];
    }

    internal void SetActiveIndex(int index, bool scroll = true)
        => SetActiveId(index >= 0 && index < Count ? IdAt(index) : null, scroll);

    internal int FirstEnabled()
    {
        for (var i = 0; i < Count; i++) if (!DisabledAt(i)) return i;
        return -1;
    }

    internal int LastEnabled()
    {
        for (var i = Count - 1; i >= 0; i--) if (!DisabledAt(i)) return i;
        return -1;
    }

    internal int Step(int from, int step, bool loop)
    {
        var count = Count;
        if (count == 0) return -1;
        if (from < 0) return step > 0 ? FirstEnabled() : LastEnabled();
        var i = from;
        for (var attempts = 0; attempts < count; attempts++)
        {
            i += step;
            if (i >= count) { if (!loop) return from; i = 0; }
            else if (i < 0) { if (!loop) return from; i = count - 1; }
            if (!DisabledAt(i)) return i;
        }
        return from;
    }

    internal int Offset(int from, int delta)
    {
        var current = from < 0 ? (delta > 0 ? FirstEnabled() : LastEnabled()) : from;
        var steps = Math.Abs(delta);
        for (var s = 0; s < steps; s++)
        {
            var next = Step(current, Math.Sign(delta), loop: false);
            if (next == current) break;
            current = next;
        }
        return current;
    }

    /// <summary>The index of the first selected, enabled option, or -1.</summary>
    internal int FirstSelected()
    {
        for (var i = 0; i < Count; i++)
            if (!DisabledAt(i) && _isSelected(ValueAt(i))) return i;
        return -1;
    }

    internal void Search(string key)
    {
        var index = Typeahead.SearchIndex(Count, key, ActiveIndex, TextAt, DisabledAt);
        if (index >= 0) SetActiveIndex(index);
    }

    /// <summary>
    /// After options changed: keeps the highlight valid. When the highlighted option
    /// disappeared it moves to <paramref name="fallbackToFirst"/> ? the first option : nothing.
    /// </summary>
    internal void Reconcile(bool fallbackToFirst)
    {
        if (ActiveId is not null && IndexOfId(ActiveId) >= 0) return;
        SetActiveIndex(fallbackToFirst ? FirstEnabled() : -1, scroll: false);
    }

    /// <summary>
    /// Whether the options' texts can be known: an options panel with <c>Unmount</c> that has never been opened
    /// has rendered no option to read a text from (and until a panel renders, none has).
    /// </summary>
    internal bool OptionTextsKnown => IsDataMode || _optionsEverMounted;

    /// <summary>Text of the options whose value is selected (for button labels).</summary>
    internal IEnumerable<string> SelectedTexts()
    {
        if (!IsDataMode && _optionsUnmounted)
        {
            foreach (var (value, text) in _unmountedTexts)
                if (text is { Length: > 0 } && _isSelected(value)) yield return text;
            yield break;
        }
        for (var i = 0; i < Count; i++)
            if (_isSelected(ValueAt(i)) && TextAt(i) is { Length: > 0 } text) yield return text;
    }

    internal Task EnsureOrderAsync() => IsDataMode ? Task.CompletedTask : Options.EnsureOrderAsync(_interop);

    /// <summary>The value posted by a hidden form input: <paramref name="format"/>, else culture-invariant text.</summary>
    internal static string? FormatForPost(TValue value, Func<TValue, string?>? format)
        => format is not null ? format(value) : value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
}
