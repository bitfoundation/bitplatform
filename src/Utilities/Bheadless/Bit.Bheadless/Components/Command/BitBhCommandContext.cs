namespace Bit.Bheadless;

/// <summary>
/// The state of a <see cref="BitBhCommand"/>: the search, the scored / filtered items,
/// the highlighted item and the page stack. Cascaded (fixed) to every part;
/// parts are refreshed individually when their visibility or highlight changes.
/// </summary>
public sealed class BitBhCommandContext
{
    private readonly BitBhCommand _command;
    private readonly Dictionary<BitBhCommandItem, double> _scores = new();
    private readonly List<BitBhCommandGroup> _groups = new();
    private readonly List<Action> _listeners = new();

    // Every item asks for its rank while rendering and a search re-renders them all: sorted per call, a
    // keystroke cost O(n^3). The order only changes with the items, the scores (Refilter), the groups, an
    // item's ForceMount or the options, so it is built once per change.
    private BitBhCommandItem[]? _sorted;
    private Dictionary<BitBhCommandItem, int>? _ranks;
    private (int Version, string Search, bool Sort, bool Filter) _sortedFor;

    internal BitBhCommandContext(BitBhCommand command, BitBhInterop interop, string baseId)
    {
        _command = command;
        Interop = interop;
        BaseId = baseId;
        Items = new BitBhCollection<BitBhCommandItem>($"{baseId}-items");
        // Items register after parts like <BitBhCommandEmpty> rendered: tell them the count changed.
        Items.Changed += () =>
        {
            foreach (var listener in _listeners.ToArray()) listener();
        };
    }

    internal BitBhInterop Interop { get; }

    internal BitBhCommandInput? Input { get; set; }

    internal BitBhCommandList? List { get; set; }

    /// <summary>Whether the command renders its own label (its <c>Label</c> parameter is set).</summary>
    internal bool HasLabel => string.IsNullOrEmpty(_command.Label) is false;

    internal BitBhCommand Command => _command;

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The id of the input.</summary>
    public string InputId => $"{BaseId}-input";

    /// <summary>The id of the list (<c>role="listbox"</c>).</summary>
    public string ListId => $"{BaseId}-list";

    /// <summary>The id of the accessible label.</summary>
    public string LabelId => $"{BaseId}-label";

    /// <summary>All registered items in document order (visible or not).</summary>
    public BitBhCollection<BitBhCommandItem> Items { get; }

    /// <summary>The current search text.</summary>
    public string Search => _command.CurrentSearch;

    /// <summary>The current page (null for the root page).</summary>
    public string? Page => _command.CurrentPage;

    /// <summary>The page stack (root first).</summary>
    public IReadOnlyList<string> Pages => _command.PageStack;

    /// <summary>The highlighted item.</summary>
    public BitBhCommandItem? ActiveItem { get; private set; }

    /// <summary>The number of items matching the search.</summary>
    public int VisibleCount => Items.Items.Count(IsVisible);

    /// <summary>Whether filtering is enabled.</summary>
    public bool ShouldFilter => _command.ShouldFilter;

    /// <summary>Whether results are sorted by relevance.</summary>
    public bool Sort => _command.Sort;

    /// <summary>Pushes a page (nested command menu) and clears the search.</summary>
    public Task PushPageAsync(string page) => _command.PushPageAsync(page);

    /// <summary>Returns to the previous page.</summary>
    public Task PopPageAsync() => _command.PopPageAsync();

    /// <summary>Clears the search.</summary>
    public Task ClearSearchAsync() => _command.SetSearchAsync(string.Empty);

    /// <summary>Whether <paramref name="item"/> matches the current search.</summary>
    public bool IsVisible(BitBhCommandItem item)
        => item.ForceMount || !ShouldFilter || string.IsNullOrWhiteSpace(Search) || Score(item) > 0;

    /// <summary>The relevance score of <paramref name="item"/> (1 when not searching).</summary>
    public double Score(BitBhCommandItem item) => _scores.TryGetValue(item, out var s) ? s : 1;

    /// <summary>The 0-based visual order of <paramref name="item"/> when sorting.</summary>
    public int Rank(BitBhCommandItem item)
    {
        if (!Sort || string.IsNullOrWhiteSpace(Search)) return Items.IndexOf(item);
        // Ranked among every visible item, disabled ones included: ranked among the enabled ones
        // only, a disabled match would get -1 and be drawn above the best match.
        var sorted = SortedVisible();
        _ranks ??= sorted.Select((i, index) => (i, index)).ToDictionary(x => x.i, x => x.index);
        return _ranks.TryGetValue(item, out var rank) ? rank : -1;
    }

    /// <summary>Whether <paramref name="group"/> has visible items.</summary>
    public bool IsVisible(BitBhCommandGroup group)
        => group.ForceMount || Items.Items.Any(i => ReferenceEquals(i.Group, group) && IsVisible(i));

    /// <summary>Best score within a group (for sorting groups).</summary>
    public double GroupScore(BitBhCommandGroup group)
        => Items.Items.Where(i => ReferenceEquals(i.Group, group) && IsVisible(i)).Select(Score).DefaultIfEmpty(0).Max();

    internal void RegisterGroup(BitBhCommandGroup group)
    {
        if (_groups.Contains(group)) return;
        _groups.Add(group);
        InvalidateOrder();
    }

    internal void UnregisterGroup(BitBhCommandGroup group)
    {
        if (_groups.Remove(group)) InvalidateOrder();
    }

    /// <summary>Drops the cached display order (something it depends on changed).</summary>
    internal void InvalidateOrder()
    {
        _sorted = null;
        _ranks = null;
    }

    internal void Subscribe(Action listener) => _listeners.Add(listener);

    internal void Unsubscribe(Action listener) => _listeners.Remove(listener);

    /// <summary>Visible, enabled items in navigation order.</summary>
    internal IEnumerable<BitBhCommandItem> NavigationOrder() => SortedVisible().Where(i => !i.IsItemDisabled);

    /// <summary>Visible items (disabled ones included) in display order.</summary>
    private BitBhCommandItem[] SortedVisible()
    {
        var key = (Items.Version, Search, Sort, ShouldFilter);
        if (_sorted is not null && _sortedFor == key) return _sorted;

        var visible = Items.Items.Where(IsVisible).ToArray();
        if (Sort && !string.IsNullOrWhiteSpace(Search))
        {
            // Group scores once per sort, not once per sort key.
            var groupScores = new Dictionary<BitBhCommandGroup, double>();
            foreach (var item in visible)
            {
                if (item.Group is not { } group) continue;
                var score = Score(item);
                if (!groupScores.TryGetValue(group, out var best) || score > best) groupScores[group] = score;
            }
            visible = visible
                .OrderByDescending(i => i.Group is null ? Score(i) : groupScores[i.Group])
                .ThenBy(i => i.Group is null ? -1 : _groups.IndexOf(i.Group))
                .ThenByDescending(Score)
                .ToArray();
        }

        _sortedFor = key;
        _ranks = null;
        return _sorted = visible;
    }

    /// <summary>Re-scores every item for the current search and refreshes what changed.</summary>
    internal void Refilter()
    {
        _scores.Clear();
        InvalidateOrder();
        if (ShouldFilter && !string.IsNullOrWhiteSpace(Search))
        {
            foreach (var item in Items.Items)
                _scores[item] = _command.ScoreItem(item.ItemValue, Search, item.Keywords ?? []);
        }

        foreach (var item in Items.Items.ToArray()) item.Refresh();
        foreach (var group in _groups.ToArray()) group.Refresh();
        foreach (var listener in _listeners.ToArray()) listener();
        // The input shows the search: a search changed from code (ClearSearchAsync, a controlled
        // Search) re-renders it even when the highlight - its other re-render trigger - stays put.
        Input?.Refresh();

        // Keep the highlight on a visible item; default to the first one.
        if (ActiveItem is null || !IsVisible(ActiveItem) || ActiveItem.IsItemDisabled)
            SetActive(NavigationOrder().FirstOrDefault(), scroll: true);
    }

    internal void SetActive(BitBhCommandItem? item, bool scroll = true)
    {
        if (ReferenceEquals(item, ActiveItem)) return;
        var previous = ActiveItem;
        ActiveItem = item;
        previous?.Refresh();
        item?.Refresh();
        Input?.Refresh();
        _command.OnActiveChanged(item);
        if (item is not null && scroll)
            _ = Interop.ScrollIntoViewAsync(item.ItemId);
    }

    internal void Move(int delta, bool loop)
    {
        var order = NavigationOrder().ToList();
        if (order.Count == 0) return;
        var index = ActiveItem is null ? -1 : order.IndexOf(ActiveItem);
        var next = index + delta;
        if (next >= order.Count) next = loop ? 0 : order.Count - 1;
        if (next < 0) next = loop ? order.Count - 1 : 0;
        SetActive(order[next]);
    }

    internal void MoveToGroup(int direction)
    {
        var order = NavigationOrder().ToList();
        if (order.Count == 0) return;
        var currentGroup = ActiveItem?.Group;
        var index = ActiveItem is null ? -1 : order.IndexOf(ActiveItem);
        if (direction > 0)
        {
            var next = order.Skip(index + 1).FirstOrDefault(i => !ReferenceEquals(i.Group, currentGroup));
            if (next is not null) SetActive(next);
        }
        else
        {
            // Start of the current group, then start of the previous one.
            var groupStart = order.FirstOrDefault(i => ReferenceEquals(i.Group, currentGroup));
            if (groupStart is not null && !ReferenceEquals(groupStart, ActiveItem))
            {
                SetActive(groupStart);
                return;
            }
            var previous = order.Take(Math.Max(0, index)).LastOrDefault(i => !ReferenceEquals(i.Group, currentGroup));
            if (previous is not null)
                SetActive(order.First(i => ReferenceEquals(i.Group, previous.Group)));
        }
    }

    internal void First() => SetActive(NavigationOrder().FirstOrDefault());

    internal void Last() => SetActive(NavigationOrder().LastOrDefault());

    internal Task SelectActiveAsync() => ActiveItem is { IsItemDisabled: false } item ? item.SelectAsync() : Task.CompletedTask;

    internal bool IsActive(BitBhCommandItem item) => ReferenceEquals(item, ActiveItem);
}
