using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A navigation pane (Nav) provides links to the main areas of an app or site.
/// </summary>
public partial class BitNav<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    internal TItem? _currentItem;
    internal List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;
    internal Dictionary<TItem, bool> _itemExpandStates = [];



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The accent color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of the BitNav component.
    /// </summary>
    [Parameter] public string? ChevronDownIcon { get; set; }

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Renders the nav in a width to only fit its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Renders the nav in full width of its container element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom HeaderTemplate.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode HeaderTemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Only renders the icon of each nav item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    [Parameter] public int IndentValue { get; set; } = 16;

    /// <summary>
    /// The indentation padding in px for items without children (compensation space for chevron icon).
    /// </summary>
    [Parameter] public int IndentPadding { get; set; } = 27;

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    [Parameter] public int IndentReversedPadding { get; set; } = 4;

    /// <summary>
    /// A collection of items to display in the BitNav component.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom ItemTemplate.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode ItemTemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMode))]
    public BitNavMode Mode { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitNavNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemToggle { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

    /// <summary>
    /// Enables recalling the select events when the same item is selected.
    /// </summary>
    [Parameter] public bool Reselectable { get; set; }

    /// <summary>
    /// Reverses the location of the expander chevron.
    /// </summary>
    [Parameter] public bool ReversedChevron { get; set; }

    /// <summary>
    /// Selected item to show in the BitNav.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedItem))]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Enables the single-expand mode in the BitNav.
    /// </summary>
    [Parameter] public bool SingleExpand { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavClassStyles? Styles { get; set; }


    /// <summary>
    /// Collapses all items and children.
    /// </summary>
    public void CollapseAll()
    {
        foreach (var item in _items)
        {
            CollapseItemAndChildren(item);
        }
    }

    /// <summary>
    /// Toggles an item.
    /// </summary>
    public async Task ToggleItem(TItem Item)
    {
        var isExpanded = GetItemExpanded(Item) is false;

        if (SingleExpand)
        {
            if (isExpanded)
            {
                if (_currentItem is not null)
                {
                    ToggleItemAndParents(_items, _currentItem, false);
                }
            }

            if (isExpanded)
            {
                ToggleItemAndParents(_items, Item, isExpanded);
            }
            else
            {
                SetItemExpanded(Item, isExpanded);
            }

            StateHasChanged();

            _currentItem = Item;
        }
        else
        {
            SetItemExpanded(Item, isExpanded);
        }

        await OnItemToggle.InvokeAsync(Item);
    }



    internal BitNavAriaCurrent GetAriaCurrent(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.AriaCurrent;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.AriaCurrent;
        }

        if (NameSelectors is null) return BitNavAriaCurrent.Page;

        if (NameSelectors.AriaCurrent.Selector is not null)
        {
            return NameSelectors.AriaCurrent.Selector!(item) ?? BitNavAriaCurrent.Page;
        }

        return item.GetValueFromProperty(NameSelectors.AriaCurrent.Name, BitNavAriaCurrent.Page);
    }

    internal string? GetAriaLabel(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.AriaLabel;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal string? GetClass(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Class;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal List<TItem> GetChildItems(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ChildItems as List<TItem> ?? [];
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ChildItems as List<TItem> ?? [];
        }

        if (NameSelectors is null) return [];

        if (NameSelectors.ChildItems.Selector is not null)
        {
            return NameSelectors.ChildItems.Selector!(item) ?? [];
        }

        return item.GetValueFromProperty<List<TItem>>(NameSelectors.ChildItems.Name, [])!;
    }

    internal string? GetCollapseAriaLabel(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.CollapseAriaLabel;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.CollapseAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.CollapseAriaLabel.Selector is not null)
        {
            return NameSelectors.CollapseAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.CollapseAriaLabel.Name);
    }

    internal object? GetData(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Data;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Data;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Data.Selector is not null)
        {
            return NameSelectors.Data.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Data.Name);
    }

    internal string? GetDescription(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Description;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    internal string? GetExpandAriaLabel(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ExpandAriaLabel;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ExpandAriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.ExpandAriaLabel.Selector is not null)
        {
            return NameSelectors.ExpandAriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.ExpandAriaLabel.Name);
    }

    internal bool GetForceAnchor(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ForceAnchor;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.ForceAnchor;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.ForceAnchor.Selector is not null)
        {
            return NameSelectors.ForceAnchor.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.ForceAnchor.Name, false);
    }

    internal string? GetIconName(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IconName;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    internal bool GetIsEnabled(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IsEnabled;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item) ?? true;
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private bool? GetIsExpanded(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IsExpanded;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IsExpanded;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsExpanded.Selector is not null)
        {
            return NameSelectors.IsExpanded.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.IsExpanded.Name, false);
    }

    internal bool GetIsSeparator(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.IsSeparator;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.IsSeparator;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSeparator.Selector is not null)
        {
            return NameSelectors.IsSeparator.Selector!(item) ?? false;
        }

        return item.GetValueFromProperty(NameSelectors.IsSeparator.Name, false);
    }

    internal string? GetKey(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Key;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    internal string? GetStyle(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Style;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetTarget(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Target;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Template as RenderFragment<TItem>;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    internal BitNavItemTemplateRenderMode GetTemplateRenderMode(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.TemplateRenderMode;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.TemplateRenderMode;
        }

        if (NameSelectors is null) return BitNavItemTemplateRenderMode.Normal;

        if (NameSelectors.TemplateRenderMode.Selector is not null)
        {
            return NameSelectors.TemplateRenderMode.Selector!(item) ?? BitNavItemTemplateRenderMode.Normal;
        }

        return item.GetValueFromProperty(NameSelectors.TemplateRenderMode.Name, BitNavItemTemplateRenderMode.Normal);
    }

    internal string? GetText(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Text;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    internal string? GetTitle(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Title;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    internal string? GetUrl(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Url;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Url;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Url.Selector is not null)
        {
            return NameSelectors.Url.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Url.Name);
    }

    internal IEnumerable<string>? GetAdditionalUrls(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.AdditionalUrls;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.AdditionalUrls;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AdditionalUrls.Selector is not null)
        {
            return NameSelectors.AdditionalUrls.Selector!(item);
        }

        return item.GetValueFromProperty<IEnumerable<string>?>(NameSelectors.AdditionalUrls.Name);
    }

    internal string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }



    internal void SetItemExpanded(TItem item, bool value)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            SetIsExpanded(item, value);
            return;
        }

        if (_itemExpandStates.ContainsKey(item))
        {
            _itemExpandStates[item] = value;
        }
        else
        {
            _itemExpandStates.Add(item, value);
        }

    }

    internal bool GetItemExpanded(TItem item)
    {
        var isExpanded = GetIsExpanded(item);

        if (isExpanded is not null)
        {
            return isExpanded.Value;
        }

        return _itemExpandStates[item];
    }

    internal async Task SetSelectedItem(TItem item)
    {
        if (item == SelectedItem && Reselectable is false) return;

        if (await AssignSelectedItem(item) is false) return;

        await OnSelectItem.InvokeAsync(item);

        StateHasChanged();
    }

    internal void RegisterOption(BitNavOption option)
    {
        _items.Add((option as TItem)!);
        SetSelectedItemByCurrentUrl();
        StateHasChanged();
    }

    internal void UnregisterOption(BitNavOption option)
    {
        _items.Remove((option as TItem)!);
        SetSelectedItemByCurrentUrl();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-nav";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-nav-ftw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-nav-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-nav-ion" : string.Empty);

        ClassBuilder.Register(() => Accent switch
        {
            BitColor.Primary => "bit-nav-apri",
            BitColor.Secondary => "bit-nav-asec",
            BitColor.Tertiary => "bit-nav-ater",
            BitColor.Info => "bit-nav-ainf",
            BitColor.Success => "bit-nav-asuc",
            BitColor.Warning => "bit-nav-awrn",
            BitColor.SevereWarning => "bit-nav-aswr",
            BitColor.Error => "bit-nav-aerr",
            BitColor.PrimaryBackground => "bit-nav-apbg",
            BitColor.SecondaryBackground => "bit-nav-asbg",
            BitColor.TertiaryBackground => "bit-nav-atbg",
            BitColor.PrimaryForeground => "bit-nav-apfg",
            BitColor.SecondaryForeground => "bit-nav-asfg",
            BitColor.TertiaryForeground => "bit-nav-atfg",
            BitColor.PrimaryBorder => "bit-nav-apbr",
            BitColor.SecondaryBorder => "bit-nav-asbr",
            BitColor.TertiaryBorder => "bit-nav-atbr",
            _ => "bit-nav-apbg",
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-nav-pri",
            BitColor.Secondary => "bit-nav-sec",
            BitColor.Tertiary => "bit-nav-ter",
            BitColor.Info => "bit-nav-inf",
            BitColor.Success => "bit-nav-suc",
            BitColor.Warning => "bit-nav-wrn",
            BitColor.SevereWarning => "bit-nav-swr",
            BitColor.Error => "bit-nav-err",
            BitColor.PrimaryBackground => "bit-nav-pbg",
            BitColor.SecondaryBackground => "bit-nav-sbg",
            BitColor.TertiaryBackground => "bit-nav-tbg",
            BitColor.PrimaryForeground => "bit-nav-pfg",
            BitColor.SecondaryForeground => "bit-nav-sfg",
            BitColor.TertiaryForeground => "bit-nav-tfg",
            BitColor.PrimaryBorder => "bit-nav-pbr",
            BitColor.SecondaryBorder => "bit-nav-sbr",
            BitColor.TertiaryBorder => "bit-nav-tbr",
            _ => "bit-nav-pri",
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        if (ChildContent is null && Items.Any())
        {
            _items = Items.ToList();
            _oldItems = Items;
        }

        foreach (var item in Flatten(_items))
        {
            SetItemExpanded(item, GetIsExpanded(item) ?? false);
        }

        if (Mode == BitNavMode.Automatic)
        {
            SetSelectedItemByCurrentUrl();
            _navigationManager.LocationChanged += OnLocationChanged;
        }
        else
        {
            if (DefaultSelectedItem is not null && SelectedItemHasBeenSet is false)
            {
                await AssignSelectedItem(DefaultSelectedItem);
            }
        }

        await base.OnInitializedAsync();
    }



    private List<TItem> Flatten(IList<TItem> e) => e.SelectMany(c => Flatten(GetChildItems(c))).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void SetSelectedItemByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = Flatten(_items).FirstOrDefault(item => GetUrl(item) == currentUrl
                                                        || (GetAdditionalUrls(item)?.Contains(currentUrl) ?? false));

        _ = AssignSelectedItem(currentItem);
    }

    private void SetIsExpanded(TItem item, bool value)
    {
        if (item is BitNavItem navItem)
        {
            navItem.IsExpanded = value;
        }

        if (item is BitNavOption navOption)
        {
            navOption.IsExpanded = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsExpanded.Name, value);
    }

    private void CollapseItemAndChildren(TItem item)
    {
        SetIsExpanded(item, false);

        foreach (var child in GetChildItems(item))
        {
            CollapseItemAndChildren(child);
        }
    }

    private void OnSetSelectedItem()
    {
        if (SelectedItem is null) return;

        ToggleItemAndParents(_items, SelectedItem, true);
    }

    private void OnSetMode()
    {
        if (Mode is not BitNavMode.Automatic) return;

        SetSelectedItemByCurrentUrl();
    }

    private void OnSetParameters()
    {
        if (ChildContent is not null || Options is not null || Items == _oldItems) return;

        _items = Items?.ToList() ?? [];
        _oldItems = Items;
    }

    private bool ToggleItemAndParents(IList<TItem> items, TItem item, bool isExpanded)
    {
        foreach (var parent in items)
        {
            var childItems = GetChildItems(parent);
            if (parent == item || (childItems.Any() && ToggleItemAndParents(childItems, item, isExpanded)))
            {
                SetItemExpanded(parent, isExpanded);
                return true;
            }
        }

        return false;
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        if (Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }

        _disposed = true;
    }
}
