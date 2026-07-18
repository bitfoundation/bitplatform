namespace Bit.BlazorUI;

/// <summary>
/// The AccordionList component is an advanced version of the BitAccordion that renders a list of expandable/collapsible
/// items (panels) from a single collection. It manages the expand/collapse state of all the items and supports
/// single-expand (default) and multiple-expand modes.
/// </summary>
public partial class BitAccordionList<TItem> : BitComponentBase where TItem : class
{
    private int _optionKeySeed;
    private List<TItem> _items = [];
    private IEnumerable<TItem> _oldItems = default!;
    private string? _internalExpandedKey;
    private List<string> _internalExpandedKeys = [];
    private readonly HashSet<string> _expandedKeys = [];
    internal BitAccordionClassStyles? _itemClasses;
    internal BitAccordionClassStyles? _itemStyles;



    /// <summary>
    /// The color kind of the background of all the accordion items.
    /// </summary>
    [Parameter] public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of all the accordion items.
    /// </summary>
    [Parameter] public BitColorKind? Border { get; set; }

    /// <summary>
    /// The content of the AccordionList, composed of BitAccordionListOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the AccordionList.
    /// </summary>
    [Parameter] public BitAccordionListClassStyles? Classes { get; set; }

    /// <summary>
    /// The custom template to render the body (content) of each item. Used when an item does not provide its own body.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? BodyTemplate { get; set; }

    /// <summary>
    /// The default expanded key in single-expand mode (used when <see cref="ExpandedKey"/> is not set).
    /// </summary>
    [Parameter] public string? DefaultExpandedKey { get; set; }

    /// <summary>
    /// The default expanded keys in multiple-expand mode (used when <see cref="ExpandedKeys"/> is not set).
    /// </summary>
    [Parameter] public IEnumerable<string>? DefaultExpandedKeys { get; set; }

    /// <summary>
    /// The expanded key in single-expand mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound] public string? ExpandedKey { get; set; }

    /// <summary>
    /// The expanded keys in multiple-expand mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound] public IEnumerable<string>? ExpandedKeys { get; set; }

    /// <summary>
    /// Gets or sets the icon to display as the expander of all items using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpanderIconName"/> when both are set.
    /// Can be overridden per item.
    /// </summary>
    [Parameter] public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display as the expander of all items from the built-in Fluent UI icons.
    /// Can be overridden per item.
    /// </summary>
    [Parameter] public string? ExpanderIconName { get; set; }

    /// <summary>
    /// The space (gap) in pixels between the accordion items.
    /// </summary>
    [Parameter, ResetStyleBuilder] public int? Gap { get; set; }

    /// <summary>
    /// The custom template to render the header of each item. Replaces the default Title/Description header.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// The collection of items to render in the AccordionList.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// Enables the multiple-expand mode in which more than one item can be expanded at the same time.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool Multiple { get; set; }

    /// <summary>
    /// Removes the default border of all the accordion items and gives a background color to their body.
    /// </summary>
    [Parameter] public bool NoBorder { get; set; }

    /// <summary>
    /// The callback that is called when an item is collapsed.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnCollapse { get; set; }

    /// <summary>
    /// The callback that is called when an item is expanded.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnExpand { get; set; }

    /// <summary>
    /// The callback that is called when the header of an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The callback that is called when an item is toggled (expanded or collapsed).
    /// </summary>
    [Parameter] public EventCallback<TItem> OnToggle { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitAccordionListNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the AccordionList.
    /// </summary>
    [Parameter] public BitAccordionListClassStyles? Styles { get; set; }



    /// <summary>
    /// Expands all the items (only effective in multiple-expand mode).
    /// </summary>
    public async Task ExpandAll()
    {
        if (Multiple is false) return;

        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasNoValue() || _expandedKeys.Contains(key!)) continue;

            _expandedKeys.Add(key!);
            SetIsExpanded(item, true);
            await OnExpand.InvokeAsync(item);
            await OnToggle.InvokeAsync(item);
        }

        await UpdateBoundKeys();
        RefreshOptions();
        StateHasChanged();
    }

    /// <summary>
    /// Collapses all the expanded items.
    /// </summary>
    public async Task CollapseAll()
    {
        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasNoValue() || _expandedKeys.Contains(key!) is false) continue;

            _expandedKeys.Remove(key!);
            SetIsExpanded(item, false);
            await OnCollapse.InvokeAsync(item);
            await OnToggle.InvokeAsync(item);
        }

        await UpdateBoundKeys();
        RefreshOptions();
        StateHasChanged();
    }



    internal void RegisterOption(BitAccordionListOption option)
    {
        if (option.Key.HasNoValue())
        {
            // Use a monotonic seed so keys remain unique even after removals, and guard
            // against colliding with any existing explicit keys.
            var key = (_optionKeySeed++).ToString();
            while (_items.Any(i => GetItemKey(i) == key))
            {
                key = (_optionKeySeed++).ToString();
            }
            option.Key = key;
        }

        var item = (option as TItem)!;

        _items.Add(item);

        if (ShouldExpandOnRegister(option.Key!, option.IsExpanded))
        {
            _expandedKeys.Add(option.Key!);
            _internalExpandedKeys = GetOrderedExpandedKeys();
            _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();
        }

        StateHasChanged();
    }

    private bool ShouldExpandOnRegister(string key, bool optionIsExpanded)
    {
        // The controlled values take precedence over the default values which take
        // precedence over the option's own IsExpanded parameter.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet)
            {
                return ExpandedKeys?.Contains(key) ?? false;
            }

            if (DefaultExpandedKeys is not null)
            {
                return DefaultExpandedKeys.Contains(key);
            }

            return optionIsExpanded;
        }

        if (ExpandedKeyHasBeenSet)
        {
            return ExpandedKey == key;
        }

        if (DefaultExpandedKey.HasValue())
        {
            return DefaultExpandedKey == key;
        }

        // In single-expand mode only the first expanded option wins.
        return optionIsExpanded && _expandedKeys.Count == 0;
    }

    internal async Task UnregisterOption(BitAccordionListOption option)
    {
        _items.Remove((option as TItem)!);

        var wasExpanded = false;
        if (option.Key.HasValue())
        {
            wasExpanded = _expandedKeys.Remove(option.Key!);
        }

        // When a removed option was expanded, refresh the internal representations and the
        // two-way bound values so they don't keep referencing the removed key.
        if (wasExpanded)
        {
            await UpdateBoundKeys();
        }

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-acl";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Multiple ? "bit-acl-mlt" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue ? $"gap:{Gap}px" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _items = (ChildContent is null && Options is null && Items is not null) ? [.. Items] : [];

        if (ChildContent is null && Options is null)
        {
            AssignItemKeys();
            InitializeExpandedKeys();
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        BuildItemClassStyles();

        if (ChildContent is null && Options is null && Items is not null)
        {
            if (_oldItems is null || (ReferenceEquals(Items, _oldItems) is false && Items.SequenceEqual(_oldItems) is false))
            {
                _oldItems = Items;
                _items = [.. Items];
                AssignItemKeys();
                InitializeExpandedKeys();
            }
        }

        // React to external (controlled) changes of the bound keys.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet && (ExpandedKeys ?? []).SequenceEqual(_internalExpandedKeys) is false)
            {
                SyncFromExpandedKeys(ExpandedKeys);
            }
        }
        else
        {
            if (ExpandedKeyHasBeenSet && _internalExpandedKey != ExpandedKey)
            {
                SyncFromExpandedKey(ExpandedKey);
            }
        }

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // accordion list's own parameters (Styles, ExpandedKey(s), ...) change, so push a re-render to each one.
        RefreshOptions();

        await base.OnParametersSetAsync();
    }



    private void AssignItemKeys()
    {
        // Collect the explicit keys first so the auto-generated keys never collide with them.
        var usedKeys = new HashSet<string>();
        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasValue()) usedKeys.Add(key!);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (GetItemKey(item).HasValue()) continue;

            // Start from the loop index and increment until a non-colliding key is found so the
            // result stays deterministic across renders while remaining unique.
            var suffix = i;
            var candidate = suffix.ToString();
            while (usedKeys.Contains(candidate))
            {
                candidate = (++suffix).ToString();
            }

            SetItemKey(item, candidate);
            usedKeys.Add(candidate);
        }
    }

    private void InitializeExpandedKeys()
    {
        _expandedKeys.Clear();

        // Controlled values take precedence over default values.
        if (Multiple)
        {
            if (ExpandedKeysHasBeenSet && ExpandedKeys is not null)
            {
                AddExpandedKeys(ExpandedKeys);
            }
            else if (DefaultExpandedKeys is not null)
            {
                AddExpandedKeys(DefaultExpandedKeys);
            }
            else
            {
                foreach (var item in _items.Where(GetIsExpanded))
                {
                    var key = GetItemKey(item);
                    if (key.HasValue()) _expandedKeys.Add(key!);
                }
            }
        }
        else
        {
            string? key = null;

            if (ExpandedKeyHasBeenSet)
            {
                key = ExpandedKey;
            }
            else if (DefaultExpandedKey.HasValue())
            {
                key = DefaultExpandedKey;
            }
            else
            {
                key = _items.Where(GetIsExpanded).Select(GetItemKey).FirstOrDefault(k => k.HasValue());
            }

            if (key.HasValue()) _expandedKeys.Add(key!);
        }

        SyncItemsExpandedState();

        _internalExpandedKeys = GetOrderedExpandedKeys();
        _internalExpandedKey = _internalExpandedKeys.FirstOrDefault();
    }

    private void AddExpandedKeys(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            if (key.HasNoValue()) continue;
            _expandedKeys.Add(key);
            if (Multiple is false) break;
        }
    }

    private void SyncItemsExpandedState()
    {
        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            SetIsExpanded(item, key.HasValue() && _expandedKeys.Contains(key!));
        }
    }

    // Emits the expanded keys in a stable order (the order of _items) so the two-way bound
    // ExpandedKeys and the internal SequenceEqual comparisons stay deterministic across renders.
    private List<string> GetOrderedExpandedKeys()
    {
        var ordered = new List<string>(_expandedKeys.Count);
        var seen = new HashSet<string>();

        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasValue() && _expandedKeys.Contains(key!) && seen.Add(key!))
            {
                ordered.Add(key!);
            }
        }

        // Preserve any expanded keys that don't currently map to an item, in a deterministic
        // order so the bound ExpandedKeys and SequenceEqual comparisons stay stable across renders.
        foreach (var key in _expandedKeys.Where(k => seen.Contains(k) is false).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (seen.Add(key)) ordered.Add(key);
        }

        return ordered;
    }

    private void SyncFromExpandedKey(string? key)
    {
        _expandedKeys.Clear();
        if (key.HasValue()) _expandedKeys.Add(key!);
        SyncItemsExpandedState();
        _internalExpandedKey = key;
    }

    private void SyncFromExpandedKeys(IEnumerable<string>? keys)
    {
        _expandedKeys.Clear();
        if (keys is not null) AddExpandedKeys(keys);
        SyncItemsExpandedState();
        _internalExpandedKeys = GetOrderedExpandedKeys();
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (IsEnabled is false || GetIsEnabled(item) is false) return;

        _ = OnItemClick.InvokeAsync(item);

        InvokeItemClick(item);

        var key = GetItemKey(item);
        if (key.HasNoValue()) return;

        var isExpanded = _expandedKeys.Contains(key!);

        await ToggleItem(item, key!, isExpanded is false);
    }

    private async Task ToggleItem(TItem item, string key, bool expand)
    {
        if (expand)
        {
            if (Multiple is false)
            {
                // Collapse the currently expanded item(s) in single-expand mode.
                foreach (var otherKey in _expandedKeys.ToArray())
                {
                    if (otherKey == key) continue;

                    _expandedKeys.Remove(otherKey);
                    var otherItem = _items.FirstOrDefault(i => GetItemKey(i) == otherKey);
                    if (otherItem is not null)
                    {
                        SetIsExpanded(otherItem, false);
                        await OnCollapse.InvokeAsync(otherItem);
                        await OnToggle.InvokeAsync(otherItem);
                    }
                }
            }

            _expandedKeys.Add(key);
            SetIsExpanded(item, true);
            await OnExpand.InvokeAsync(item);
        }
        else
        {
            _expandedKeys.Remove(key);
            SetIsExpanded(item, false);
            await OnCollapse.InvokeAsync(item);
        }

        await OnToggle.InvokeAsync(item);

        await UpdateBoundKeys();

        // A toggle can affect other items too (single-expand mode collapses the previously expanded
        // item), and the click handler runs on the clicked item's renderer, so both the registered
        // options and the accordion list itself need an explicit re-render.
        RefreshOptions();
        StateHasChanged();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitAccordionListOption)?.InternalStateHasChanged();
        }
    }

    private async Task UpdateBoundKeys()
    {
        if (Multiple)
        {
            _internalExpandedKeys = GetOrderedExpandedKeys();
            await AssignExpandedKeys([.. _internalExpandedKeys]);
        }
        else
        {
            _internalExpandedKey = _expandedKeys.FirstOrDefault();
            await AssignExpandedKey(_internalExpandedKey);
        }
    }

    private void BuildItemClassStyles()
    {
        _itemClasses = new BitAccordionClassStyles
        {
            Root = Classes?.Item,
            Expanded = Classes?.ItemExpanded,
            Header = Classes?.ItemHeader,
            HeaderContent = Classes?.ItemHeaderContent,
            Title = Classes?.ItemTitle,
            Description = Classes?.ItemDescription,
            ExpanderIconWrapper = Classes?.ItemExpanderIconWrapper,
            ExpanderIcon = Classes?.ItemExpanderIcon,
            ExpandedIcon = Classes?.ItemExpandedIcon,
            ContentContainer = Classes?.ItemContentContainer,
            Content = Classes?.ItemContent,
        };

        _itemStyles = new BitAccordionClassStyles
        {
            Root = Styles?.Item,
            Expanded = Styles?.ItemExpanded,
            Header = Styles?.ItemHeader,
            HeaderContent = Styles?.ItemHeaderContent,
            Title = Styles?.ItemTitle,
            Description = Styles?.ItemDescription,
            ExpanderIconWrapper = Styles?.ItemExpanderIconWrapper,
            ExpanderIcon = Styles?.ItemExpanderIcon,
            ExpandedIcon = Styles?.ItemExpandedIcon,
            ContentContainer = Styles?.ItemContentContainer,
            Content = Styles?.ItemContent,
        };
    }

    internal bool IsItemExpanded(TItem item)
    {
        var key = GetItemKey(item);
        return key.HasValue() && _expandedKeys.Contains(key!);
    }

    internal RenderFragment<bool>? GetItemHeaderTemplate(TItem item)
    {
        var itemTemplate = GetHeaderTemplate(item);
        if (itemTemplate is not null) return _ => itemTemplate(item);

        if (HeaderTemplate is not null)
        {
            return _ => HeaderTemplate(item);
        }

        return null;
    }

    internal RenderFragment? GetItemBody(TItem item)
    {
        // The option's plain inline content (ChildContent) is rendered as-is.
        if (item is BitAccordionListOption listOption && listOption.ChildContent is not null)
        {
            return listOption.ChildContent;
        }

        var body = GetBody(item);
        if (body is not null) return body(item);

        if (BodyTemplate is not null)
        {
            return BodyTemplate(item);
        }

        return null;
    }

    internal BitIconInfo? GetItemExpanderIcon(TItem item)
    {
        return GetExpanderIcon(item) ?? ExpanderIcon;
    }

    internal string? GetItemExpanderIconName(TItem item)
    {
        return GetExpanderIconName(item) ?? ExpanderIconName;
    }



    internal string? GetItemKey(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Key;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private void SetItemKey(TItem item, string value)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.Key = value;
            return;
        }

        if (item is BitAccordionListOption listOption)
        {
            listOption.Key = value;
            return;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.Key.Name, value);
    }

    internal string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Class;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Style;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Title;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    internal string? GetDescription(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Description;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    internal bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.IsEnabled;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private bool GetIsExpanded(TItem? item)
    {
        if (item is null) return false;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.IsExpanded;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.IsExpanded;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsExpanded.Selector is not null)
        {
            return NameSelectors.IsExpanded.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsExpanded.Name, false);
    }

    private void SetIsExpanded(TItem item, bool value)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.IsExpanded = value;
            return;
        }

        // Option components expose IsExpanded only as an initial parameter; their runtime
        // state is tracked internally via the expanded keys set.
        if (item is BitAccordionListOption)
        {
            return;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsExpanded.Name, value);
    }

    private BitIconInfo? GetExpanderIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpanderIcon;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpanderIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpanderIcon.Selector is not null)
        {
            return NameSelectors.ExpanderIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.ExpanderIcon.Name);
    }

    private string? GetExpanderIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.ExpanderIconName;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.ExpanderIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpanderIconName.Selector is not null)
        {
            return NameSelectors.ExpanderIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ExpanderIconName.Name);
    }

    private RenderFragment<TItem>? GetBody(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.Body as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.Body as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Body.Selector is not null)
        {
            return NameSelectors.Body.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Body.Name);
    }

    private RenderFragment<TItem>? GetHeaderTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitAccordionListItem listItem)
        {
            return listItem.HeaderTemplate as RenderFragment<TItem>;
        }

        if (item is BitAccordionListOption listOption)
        {
            return listOption.HeaderTemplate as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.HeaderTemplate.Selector is not null)
        {
            return NameSelectors.HeaderTemplate.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.HeaderTemplate.Name);
    }

    private void InvokeItemClick(TItem item)
    {
        if (item is BitAccordionListItem listItem)
        {
            listItem.OnClick?.Invoke(listItem);
            return;
        }

        if (item is BitAccordionListOption listOption)
        {
            _ = listOption.OnClick.InvokeAsync(listOption);
            return;
        }

        if (NameSelectors is null) return;

        if (NameSelectors.OnClick.Selector is not null)
        {
            NameSelectors.OnClick.Selector!(item)?.Invoke(item);
        }
        else
        {
            item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
        }
    }
}
