using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// A headless, accessible Tree View implementing the WAI-ARIA Tree pattern —
/// file explorers, navigation trees, nested settings, hierarchical pickers.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Keyboard</b> — Up / Down, Right (expand / first child), Left (collapse / parent), Home / End,
///     <c>*</c> (expand siblings), Enter (activate), Space (select / check), typeahead, Shift+arrows and Ctrl+A in multi-select.</item>
///   <item><b>Selection</b> — none, single (<c>@bind-SelectedValue</c>) or multiple (<c>@bind-SelectedValues</c>, Ctrl / Shift click).</item>
///   <item><b>Hierarchical checkboxes</b> — <see cref="Checkboxes"/>: checking a parent checks its descendants;
///     parents show the mixed state (<c>aria-checked="mixed"</c>).</item>
///   <item><b>Expansion</b> — <c>@bind-ExpandedValues</c> or uncontrolled; <see cref="LoadChildren"/> for lazy loading (<c>aria-busy</c>).</item>
///   <item><b>Roving focus</b> — one tab stop; <c>aria-level</c>, <c>aria-setsize</c>, <c>aria-posinset</c> computed for you.</item>
/// </list>
///
/// <code>
/// &lt;BitBhTree TValue="string" aria-label="Files" @bind-SelectedValue="selected"&gt;
///     &lt;BitBhTreeItem TValue="string" Value="@("src")" Label="src"&gt;
///         &lt;span class="row"&gt;&lt;BitBhTreeItemToggle TValue="string"&gt;▸&lt;/BitBhTreeItemToggle&gt; src&lt;/span&gt;
///         &lt;BitBhTreeGroup TValue="string"&gt;
///             &lt;BitBhTreeItem TValue="string" Value="@("app.cs")"&gt;app.cs&lt;/BitBhTreeItem&gt;
///         &lt;/BitBhTreeGroup&gt;
///     &lt;/BitBhTreeItem&gt;
/// &lt;/BitBhTree&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BitBhTree<TValue> : BitBhComponentBase
{
    private BitBhTreeContext<TValue>? _context;
    private HashSet<TValue>? _expanded;
    private HashSet<TValue>? _selected;
    private BitBhTreeItem<TValue>? _anchor;
    private ItemRefresher? _refresher;
    private readonly BitBhTypeahead _typeahead = new();
    private HashSet<TValue> _loading = new();
    private HashSet<TValue> _loaded = new();

    [CascadingParameter]
    private BitBhDirection CascadedDir { get; set; }

    /// <summary>How items can be selected. Default: <see cref="BitBhTreeSelectionMode.Single"/>.</summary>
    [Parameter]
    public BitBhTreeSelectionMode SelectionMode { get; set; } = BitBhTreeSelectionMode.Single;

    /// <summary>Show hierarchical tri-state checkboxes (implies multiple selection).</summary>
    [Parameter]
    public bool Checkboxes { get; set; }

    /// <summary>The selected item (single mode). Supplying it makes selection controlled.</summary>
    [Parameter]
    public TValue? SelectedValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedValue"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> SelectedValueChanged { get; set; }

    /// <summary>The selected items (multiple / checkbox mode). Supplying it makes selection controlled.</summary>
    [Parameter]
    public IEnumerable<TValue>? SelectedValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedValues"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> SelectedValuesChanged { get; set; }

    /// <summary>Initially selected items when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultSelectedValues { get; set; }

    /// <summary>The expanded items. Supplying it makes expansion controlled (use <c>@bind-ExpandedValues</c>).</summary>
    [Parameter]
    public IEnumerable<TValue>? ExpandedValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="ExpandedValues"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> ExpandedValuesChanged { get; set; }

    /// <summary>Initially expanded items when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultExpandedValues { get; set; }

    /// <summary>Invoked on Enter or double click — open the file, navigate, …</summary>
    [Parameter]
    public EventCallback<TValue> OnItemActivate { get; set; }

    /// <summary>Loads children the first time an item marked <c>HasChildren</c> expands (once per item).</summary>
    [Parameter]
    public Func<TValue, Task>? LoadChildren { get; set; }

    /// <summary>Compares item values. Defaults to <see cref="EqualityComparer{T}.Default"/>.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>Disables the whole tree.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (mirrors Left / Right). Inherits from <see cref="BitBhDirectionProvider"/>.</summary>
    [Parameter]
    public BitBhDirection? Dir { get; set; }

    /// <summary>The top-level <see cref="BitBhTreeItem{TValue}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;
    private BitBhTreeContext<TValue> Context => _context ??= new BitBhTreeContext<TValue>(this, BitBhId.New("bit-bh-tree"));
    private bool IsMulti => Checkboxes || SelectionMode == BitBhTreeSelectionMode.Multiple;
    private BitBhDirection EffectiveDir => Dir ?? CascadedDir;

    internal bool IsTreeDisabled => Disabled;

    protected override void OnInitialized()
    {
        _expanded = new HashSet<TValue>(DefaultExpandedValues ?? [], EffectiveComparer);
        _selected = new HashSet<TValue>(DefaultSelectedValues ?? [], EffectiveComparer);
        _loading = new HashSet<TValue>(EffectiveComparer);
        _loaded = new HashSet<TValue>(EffectiveComparer);
        Context.Items.Changed += () => _refresher?.Request();
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => Context.Items.EnsureOrderAsync(Interop);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhTreeContext<TValue>>>(40);
        builder.AddComponentParameter(41, "Value", Context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.OpenComponent<ItemRefresher>(50);
        builder.AddComponentParameter(51, nameof(ItemRefresher.Tree), this);
        builder.CloseComponent();
        builder.CloseElement();
    }

    /// <summary>
    /// Re-renders the items whose position (<c>aria-posinset</c> / <c>aria-setsize</c>), tab stop or
    /// expandability changed when items come and go - and again once the browser re-sorted them, since an
    /// item inserted mid-list registers last. Rendering coalesces every request of a render batch into one
    /// pass, however many items registered in it.
    /// </summary>
    private sealed class ItemRefresher : ComponentBase
    {
        [Parameter]
        public BitBhTree<TValue> Tree { get; set; } = default!;

        protected override void OnInitialized() => Tree._refresher = this;

        internal void Request() => StateHasChanged();

        // The pass runs while rendering so the items it refreshes re-render in the same batch.
        protected override void BuildRenderTree(RenderTreeBuilder builder) => Tree.RefreshStaleItems();
    }

    private void RefreshStaleItems()
    {
        var tabStop = TabStopItem();
        foreach (var item in Context.Items.Items)
            if (item.IsStale(tabStop)) item.Refresh();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "tree";
        if (IsMulti) attrs["aria-multiselectable"] = "true";
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Expansion ────────────────────────────────────────────────────────────

    internal bool IsExpanded(TValue value)
        => IsProvided(nameof(ExpandedValues))
            ? ExpandedValues?.Contains(value, EffectiveComparer) ?? false
            : _expanded!.Contains(value);

    internal bool IsLoading(TValue value) => _loading.Contains(value);

    internal Task SetExpandedAsync(BitBhTreeItem<TValue> item, bool expanded) => SetExpandedAsync([item], expanded);

    private async Task SetExpandedAsync(IReadOnlyList<BitBhTreeItem<TValue>> items, bool expanded)
    {
        // An item whose children are loading expands when they arrive: another request meanwhile would load
        // them a second time.
        var targets = items.Where(i => i.IsExpandable && IsExpanded(i.Value) != expanded && !_loading.Contains(i.Value)).ToList();
        if (targets.Count == 0) return;

        if (expanded && LoadChildren is not null)
        {
            foreach (var item in targets.Where(NeedsLoad).ToArray())
            {
                _loading.Add(item.Value);
                item.Refresh();
                try
                {
                    await LoadChildren(item.Value);
                    _loaded.Add(item.Value);
                }
                finally
                {
                    _loading.Remove(item.Value);
                }
            }
        }

        // One set for every item: a controlled ExpandedValues only catches up once the parent re-rendered, so
        // a set built per item from it would keep just the last one.
        var set = new HashSet<TValue>(IsProvided(nameof(ExpandedValues)) ? ExpandedValues ?? [] : _expanded!, EffectiveComparer);
        foreach (var item in targets)
        {
            if (expanded) set.Add(item.Value); else set.Remove(item.Value);
        }
        if (!IsProvided(nameof(ExpandedValues))) _expanded = set;
        await ExpandedValuesChanged.InvokeAsync(set.ToArray());

        // Collapsing a branch that contains the tab stop moves it to the branch.
        if (!expanded && Context.FocusedItem is { } focused && targets.FirstOrDefault(t => Context.DescendantsOf(t).Contains(focused)) is { } branch)
            Context.FocusedItem = branch;

        foreach (var item in targets) item.Refresh();
    }

    // Children load once. A collapsed item whose group unmounts them (Unmount) has no registered children on
    // every expand, which is no reason to load them again.
    private bool NeedsLoad(BitBhTreeItem<TValue> item)
        => item.HasChildren && !_loaded.Contains(item.Value) && !Context.ChildrenOf(item).Any();

    internal Task ToggleExpandedAsync(BitBhTreeItem<TValue> item) => SetExpandedAsync(item, !IsExpanded(item.Value));

    // ── Selection ────────────────────────────────────────────────────────────

    internal bool IsSelected(TValue value)
    {
        if (SelectionMode == BitBhTreeSelectionMode.None && !Checkboxes) return false;
        if (!IsMulti && IsProvided(nameof(SelectedValue)))
            return SelectedValue is not null && EffectiveComparer.Equals(SelectedValue, value);
        if (IsMulti && IsProvided(nameof(SelectedValues)))
            return SelectedValues?.Contains(value, EffectiveComparer) ?? false;
        return _selected!.Contains(value);
    }

    private HashSet<TValue> CurrentSelection()
    {
        if (!IsMulti && IsProvided(nameof(SelectedValue)))
            return SelectedValue is null ? new HashSet<TValue>(EffectiveComparer) : new HashSet<TValue>(EffectiveComparer) { SelectedValue };
        if (IsMulti && IsProvided(nameof(SelectedValues)))
            return new HashSet<TValue>(SelectedValues ?? [], EffectiveComparer);
        return new HashSet<TValue>(_selected!, EffectiveComparer);
    }

    private async Task CommitSelectionAsync(HashSet<TValue> selection)
    {
        if (!IsProvided(nameof(SelectedValue)) && !IsProvided(nameof(SelectedValues))) _selected = selection;
        if (IsMulti)
            await SelectedValuesChanged.InvokeAsync(selection.ToArray());
        else
            await SelectedValueChanged.InvokeAsync(selection.Count > 0 ? selection.First() : default);
        // Selection changes can affect ancestors' mixed state: refresh everything.
        foreach (var item in Context.Items.Items.Where(i => i.ParentItem is null).ToArray()) item.Refresh();
    }

    internal async Task SelectAsync(BitBhTreeItem<TValue> item, bool toggle = false, bool range = false)
    {
        if (item.IsItemDisabled) return;
        if (SelectionMode == BitBhTreeSelectionMode.None && !Checkboxes) return;
        var selection = CurrentSelection();

        if (Checkboxes)
        {
            var check = Context.CheckedState(item) != true;
            foreach (var v in Context.DescendantsOf(item).Where(d => !d.IsItemDisabled).Select(d => d.Value).Append(item.Value))
            {
                if (check) selection.Add(v); else selection.Remove(v);
            }
            // Keep ancestors consistent with their descendants.
            for (var parent = item.ParentItem; parent is not null; parent = parent.ParentItem)
            {
                var all = Context.DescendantsOf(parent).Where(d => !Context.ChildrenOf(d).Any()).All(l => selection.Contains(l.Value));
                if (all) selection.Add(parent.Value); else selection.Remove(parent.Value);
            }
        }
        else if (!IsMulti)
        {
            if (selection.Contains(item.Value) && toggle) selection.Clear();
            else selection = new HashSet<TValue>(EffectiveComparer) { item.Value };
        }
        else if (range && _anchor is not null)
        {
            var visible = Context.VisibleItems().ToList();
            var a = visible.IndexOf(_anchor);
            var b = visible.IndexOf(item);
            if (a >= 0 && b >= 0)
            {
                selection = new HashSet<TValue>(EffectiveComparer);
                for (var i = Math.Min(a, b); i <= Math.Max(a, b); i++)
                    if (!visible[i].IsItemDisabled) selection.Add(visible[i].Value);
            }
        }
        else if (toggle)
        {
            if (!selection.Remove(item.Value)) selection.Add(item.Value);
            _anchor = item;
        }
        else
        {
            selection = new HashSet<TValue>(EffectiveComparer) { item.Value };
            _anchor = item;
        }

        await CommitSelectionAsync(selection);
    }

    private async Task SelectAllAsync()
    {
        var selection = new HashSet<TValue>(Context.Items.Items.Where(i => !i.IsItemDisabled).Select(i => i.Value), EffectiveComparer);
        await CommitSelectionAsync(selection);
    }

    // ── Focus / keyboard ─────────────────────────────────────────────────────

    /// <summary>The item holding the tree's single tab stop.</summary>
    internal BitBhTreeItem<TValue>? TabStopItem()
    {
        var focused = Context.FocusedItem;
        if (focused is { IsRegistered: true, IsItemDisabled: false } && focused.AncestorsExpanded) return focused;
        // Default tab stop: the first selected visible item, else the first visible item.
        BitBhTreeItem<TValue>? first = null;
        foreach (var item in Context.VisibleItems())
        {
            if (item.IsItemDisabled) continue;
            if (IsSelected(item.Value)) return item;
            first ??= item;
        }
        return first;
    }

    internal void OnItemFocused(BitBhTreeItem<TValue> item)
    {
        // A click focuses a disabled item too (tabindex="-1"); it never becomes the tab stop.
        if (item.IsItemDisabled || ReferenceEquals(Context.FocusedItem, item)) return;
        var previous = TabStopItem();
        Context.FocusedItem = item;
        previous?.Refresh();
        item.Refresh();
    }

    private async Task FocusItemAsync(BitBhTreeItem<TValue>? item)
    {
        if (item is null) return;
        OnItemFocused(item);
        await Interop.FocusAsync(item.ItemId);
    }

    internal async Task HandleKeyDownAsync(BitBhTreeItem<TValue> item, KeyboardEventArgs e)
    {
        if (Disabled) return;
        await Context.Items.EnsureOrderAsync(Interop);
        // Disabled items included: one can hold DOM focus (a click focuses it), and the arrows then move on
        // from where it is instead of from the top.
        var visible = Context.VisibleItems().ToList();
        var index = visible.IndexOf(item);
        var rtl = EffectiveDir == BitBhDirection.Rtl;
        var openKey = rtl ? "ArrowLeft" : "ArrowRight";
        var closeKey = rtl ? "ArrowRight" : "ArrowLeft";

        if (e.Key == " " && _typeahead.IsActive)
        {
            await TypeaheadAsync(item, " ", visible);
            return;
        }

        switch (e.Key)
        {
            case "ArrowDown":
                if (NextEnabled(visible, index, +1) is { } next)
                {
                    await FocusItemAsync(next);
                    if (e.ShiftKey && IsMulti && !Checkboxes) await SelectAsync(next, range: true);
                }
                return;
            case "ArrowUp":
                if (NextEnabled(visible, index, -1) is { } prev)
                {
                    await FocusItemAsync(prev);
                    if (e.ShiftKey && IsMulti && !Checkboxes) await SelectAsync(prev, range: true);
                }
                return;
            case "Home":
                await FocusItemAsync(NextEnabled(visible, -1, +1));
                return;
            case "End":
                await FocusItemAsync(NextEnabled(visible, visible.Count, -1));
                return;
            case "Enter":
                if (item.IsItemDisabled) return;
                if (SelectionMode == BitBhTreeSelectionMode.Single && !Checkboxes) await SelectAsync(item);
                await OnItemActivate.InvokeAsync(item.Value);
                return;
            case " ":
                // A held key repeats: the selection would flip back and forth.
                if (e.Repeat) return;
                await SelectAsync(item, toggle: IsMulti || e.CtrlKey || e.MetaKey);
                return;
            case "*":
                await SetExpandedAsync(Context.ChildrenOf(item.ParentItem).ToArray(), true);
                return;
            case "a" or "A" when (e.CtrlKey || e.MetaKey) && IsMulti:
                await SelectAllAsync();
                return;
        }

        if (e.Key == openKey)
        {
            if (item.IsExpandable && !IsExpanded(item.Value)) await SetExpandedAsync(item, true);
            else if (item.IsExpandable) await FocusItemAsync(Context.ChildrenOf(item).FirstOrDefault(c => !c.IsItemDisabled));
        }
        else if (e.Key == closeKey)
        {
            if (item.IsExpandable && IsExpanded(item.Value)) await SetExpandedAsync(item, false);
            else await FocusItemAsync(item.ParentItem);
        }
        else if (BitBhTypeahead.IsTypeaheadKey(e.Key, e.CtrlKey || e.MetaKey, e.AltKey))
        {
            await TypeaheadAsync(item, e.Key, visible);
        }
    }

    private static BitBhTreeItem<TValue>? NextEnabled(List<BitBhTreeItem<TValue>> items, int from, int step)
    {
        for (var i = from + step; i >= 0 && i < items.Count; i += step)
            if (!items[i].IsItemDisabled) return items[i];
        return null;
    }

    private Task TypeaheadAsync(BitBhTreeItem<TValue> item, string key, List<BitBhTreeItem<TValue>> visible)
        => _typeahead.Search(visible, key, item, i => i.ItemText, i => i.IsItemDisabled) is { } match ? FocusItemAsync(match) : Task.CompletedTask;

    internal async Task HandleClickAsync(BitBhTreeItem<TValue> item, MouseEventArgs e)
    {
        if (Disabled || item.IsItemDisabled) return;
        OnItemFocused(item);
        if (Checkboxes) await SelectAsync(item);
        else if (IsMulti) await SelectAsync(item, toggle: e.CtrlKey || e.MetaKey, range: e.ShiftKey);
        else if (SelectionMode == BitBhTreeSelectionMode.Single) await SelectAsync(item);
        if (e.Detail == 2) await OnItemActivate.InvokeAsync(item.Value);
    }
}
