using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavMenu<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    internal TItem? _currentItem;
    internal List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The accent color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavMenuClassStyles? Classes { get; set; }

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
    /// Only renders the icon of each nav item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// A collection of item to display in the navigation bar.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetMode))]
    public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitNavNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

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
    /// Custom CSS styles for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavMenuClassStyles? Styles { get; set; }



    internal string? GetClass(TItem item)
    {
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Class;
        }

        if (item is BitNavMenuOption navOption)
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

    internal string? GetIconName(TItem item)
    {
        if (item is BitNavMenuItem navItem)
        {
            return navItem.IconName;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.IsEnabled;
        }

        if (item is BitNavMenuOption navOption)
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

    internal string? GetKey(TItem item)
    {
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Key;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Style;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Target;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Template as RenderFragment<TItem>;
        }

        if (item is BitNavMenuOption navOption)
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

    internal string? GetText(TItem item)
    {
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Text;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Title;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.Url;
        }

        if (item is BitNavMenuOption navOption)
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
        if (item is BitNavMenuItem navItem)
        {
            return navItem.AdditionalUrls;
        }

        if (item is BitNavMenuOption navOption)
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


    internal async Task SetSelectedItem(TItem item)
    {
        if (await AssignSelectedItem(item) is false) return;

        if (item != SelectedItem || Reselectable)
        {
            await OnSelectItem.InvokeAsync(item);
        }

        StateHasChanged();
    }


    internal void RegisterOption(BitNavMenuOption option)
    {
        _items.Add((option as TItem)!);
        StateHasChanged();
    }

    internal void UnregisterOption(BitNavMenuOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }


    internal string GetItemKey(TItem item)
    {
        return GetKey(item) ?? Guid.NewGuid().ToString();
    }



    protected override string RootElementClass => "bit-nmn";

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
            _items = [.. Items];
            _oldItems = Items;
        }

        if (Mode != BitNavMode.Automatic && SelectedItemHasBeenSet is false && DefaultSelectedItem is not null)
        {
            await AssignSelectedItem(DefaultSelectedItem);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent is null && Items != _oldItems)
        {
            _items = Items?.ToList() ?? [];
            _oldItems = Items;
        }

        await base.OnParametersSetAsync();
    }



    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void SetSelectedItemByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = _items.FirstOrDefault(item => GetUrl(item) == currentUrl ||
                                                        (GetAdditionalUrls(item)?.Contains(currentUrl) ?? false));

        if (currentItem is not null)
        {
            _ = AssignSelectedItem(currentItem);
        }
    }

    private void OnSetMode()
    {
        if (Mode == BitNavMode.Automatic)
        {
            SetSelectedItemByCurrentUrl();
            _navigationManager.LocationChanged += OnLocationChanged;
        }
        else
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }

    private void OnSetSelectedItem()
    {
        if (SelectedItem is null) return;
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
