using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A tab panel that provides navigation links to the main areas of an app.
/// </summary>
public partial class BitNavBar<TItem> : BitComponentBase, IDisposable where TItem : class
{
    private bool _disposed;
    internal TItem? _currentItem;
    internal List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the navbar.
    /// </summary>
    [Parameter] public BitNavBarClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the navbar.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Renders the nav bat in a width to only fit its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Renders the nav bar in full width of its container element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Only renders the icon of each navbar item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// A collection of items to display in the navbar.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

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
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public RenderFragment? Options { get; set; }

    /// <summary>
    /// Enables recalling the select events when the same item is selected.
    /// </summary>
    [Parameter] public bool Reselectable { get; set; }

    /// <summary>
    /// Selected item to show in the navbar.
    /// </summary>
    [Parameter, TwoWayBound]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the navbar.
    /// </summary>
    [Parameter] public BitNavBarClassStyles? Styles { get; set; }



    internal void RegisterOption(BitNavBarOption option)
    {
        _items.Add((option as TItem)!);
        SetSelectedItemByCurrentUrl();
        StateHasChanged();
    }

    internal void UnregisterOption(BitNavBarOption option)
    {
        _items.Remove((option as TItem)!);
        SetSelectedItemByCurrentUrl();
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-nbr";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FitWidth ? "bit-nbr-ftw" : string.Empty);
        ClassBuilder.Register(() => FullWidth ? "bit-nbr-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-nbr-ion" : string.Empty);

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-nbr-pri",
            BitColor.Secondary => "bit-nbr-sec",
            BitColor.Tertiary => "bit-nbr-ter",
            BitColor.Info => "bit-nbr-inf",
            BitColor.Success => "bit-nbr-suc",
            BitColor.Warning => "bit-nbr-wrn",
            BitColor.SevereWarning => "bit-nbr-swr",
            BitColor.Error => "bit-nbr-err",
            BitColor.PrimaryBackground => "bit-nbr-pbg",
            BitColor.SecondaryBackground => "bit-nbr-sbg",
            BitColor.TertiaryBackground => "bit-nbr-tbg",
            BitColor.PrimaryForeground => "bit-nbr-pfg",
            BitColor.SecondaryForeground => "bit-nbr-sfg",
            BitColor.TertiaryForeground => "bit-nbr-tfg",
            BitColor.PrimaryBorder => "bit-nbr-pbr",
            BitColor.SecondaryBorder => "bit-nbr-sbr",
            BitColor.TertiaryBorder => "bit-nbr-tbr",
            _ => "bit-nbr-pri",
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        if (ChildContent is null && Options is null && Items.Any())
        {
            _items = [.. Items];
            _oldItems = Items;
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



    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void SetSelectedItemByCurrentUrl()
    {
        if (Mode is not BitNavMode.Automatic) return;

        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = _items.FirstOrDefault(item => string.Equals(GetUrl(item), currentUrl, StringComparison.OrdinalIgnoreCase) ||
                                                        (GetAdditionalUrls(item)?.Any(u => string.Equals(u, currentUrl, StringComparison.OrdinalIgnoreCase)) ?? false));

        _ = AssignSelectedItem(currentItem);
    }

    private void OnSetParameters()
    {
        if (ChildContent is not null || Options is not null || Items == _oldItems) return;

        _items = Items?.ToList() ?? [];
        _oldItems = Items;
    }

    private void OnSetMode()
    {
        if (Mode is not BitNavMode.Automatic) return;

        SetSelectedItemByCurrentUrl();
    }

    private async Task SetSelectedItem(TItem item)
    {
        if (item == SelectedItem && Reselectable is false) return;

        if (await AssignSelectedItem(item) is false) return;

        await OnSelectItem.InvokeAsync(item);

        StateHasChanged();
    }

    private string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    private async Task HandleOnClick(TItem item)
    {
        if (GetIsEnabled(item) is false) return;

        if (SelectedItem != item || Reselectable)
        {
            await OnItemClick.InvokeAsync(item);
        }

        if (Mode == BitNavMode.Manual)
        {
            await SetSelectedItem(item);
        }
    }

    private string GetItemCssStyle(TItem item)
    {
        var itm = Styles?.Item;
        var style = GetStyle(item);
        var selected = SelectedItem == item ? Styles?.SelectedItem : string.Empty;
        return $"{itm} {style} {selected}".Trim();
    }

    private string GetItemCssClass(TItem item, bool isEnabled)
    {
        var itm = Classes?.Item;
        var @class = GetClass(item);
        var selected = SelectedItem == item ? $"bit-nbr-sel {Classes?.SelectedItem}" : string.Empty;
        var disabled = isEnabled ? string.Empty : "bit-nbr-dis";

        return $"bit-nbr-itm {itm} {@class} {selected} {disabled}".Trim();
    }



    private string? GetClass(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Class;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetIconName(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.IconName;
        }

        if (item is BitNavBarOption navOption)
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

    private bool GetIsEnabled(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.IsEnabled;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetKey(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Key;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetStyle(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Style;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetTarget(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Target;
        }

        if (item is BitNavBarOption navOption)
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

    private RenderFragment<TItem>? GetTemplate(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Template as RenderFragment<TItem>;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetText(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Text;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetTitle(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Title;
        }

        if (item is BitNavBarOption navOption)
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

    private string? GetUrl(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.Url;
        }

        if (item is BitNavBarOption navOption)
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

    private IEnumerable<string>? GetAdditionalUrls(TItem item)
    {
        if (item is BitNavBarItem navItem)
        {
            return navItem.AdditionalUrls;
        }

        if (item is BitNavBarOption navOption)
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



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing is false || _disposed) return;

        _navigationManager.LocationChanged -= OnLocationChanged;

        _disposed = true;
    }
}
