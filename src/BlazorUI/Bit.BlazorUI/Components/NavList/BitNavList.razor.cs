using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavList<TItem> : IDisposable
{
    private const string FORCE_ANCHOR = "ForceAnchor";
    private const string KEY = "Key";
    private const string NAME = "Name";
    private const string TITLE = "Title";
    private const string URL = "Url";
    private const string ARIA_CURRENT = "AriaCurrent";
    private const string EXPAND_ARIA_LABEL = "ExpandAriaLabel";
    private const string COLLAPSE_ARIA_LABEL = "CollapseAriaLabel";
    private const string ARIA_LABEL = "AriaLabel";
    private const string ICON_NAME = "IconName";
    private const string IS_EXPANDED = "IsExpanded";
    private const string IS_ENABLED = "IsEnabled";
    private const string STYLE = "Style";
    private const string TARGET = "Target";
    private const string ITEMS = "Items";

    private bool SelectedKeyHasBeenSet;
    private string? selectedKey;

    private string _internalForceAnchorField = FORCE_ANCHOR;
    private string _internalKeyField = KEY;
    private string _internalNameField = NAME;
    private string _internalTitleField = TITLE;
    private string _internalUrlField = URL;
    private string _internalAriaCurrentField = ARIA_CURRENT;
    private string _internalExpandAriaLabelField = EXPAND_ARIA_LABEL;
    private string _internalCollapseAriaLabelField = COLLAPSE_ARIA_LABEL;
    private string _internalAriaLabelField = ARIA_LABEL;
    private string _internalIconNameField = ICON_NAME;
    private string _internalIsExpandedField = IS_EXPANDED;
    private string _internalIsEnabledField = IS_ENABLED;
    private string _internalStyleField = STYLE;
    private string _internalTargetField = TARGET;
    private string _internalItemsField = ITEMS;

    private IDictionary<string, int> _itemsDepth = new Dictionary<string, int>();
    private IDictionary<string, bool> _itemsExpanded = new Dictionary<string, bool>();
    private IDictionary<BitNavListItemAriaCurrent, string> _ariaCurrentMap = new Dictionary<BitNavListItemAriaCurrent, string>()
    {
        [BitNavListItemAriaCurrent.Page] = "page",
        [BitNavListItemAriaCurrent.Step] = "step",
        [BitNavListItemAriaCurrent.Location] = "location",
        [BitNavListItemAriaCurrent.Time] = "time",
        [BitNavListItemAriaCurrent.Date] = "date",
        [BitNavListItemAriaCurrent.True] = "true"
    };

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;

    /// <summary>
    /// Aria-current token for active nav item.
    /// Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public string AriaCurrentField { get; set; } = ARIA_CURRENT;

    /// <summary>
    /// Aria-current token for active nav item.
    /// Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitNavListItemAriaCurrent>>? AriaCurrentFieldSelector { get; set; }

    /// <summary>
    /// Aria label for the item.
    /// Ignored if collapseAriaLabel or expandAriaLabel is provided.
    /// </summary>
    [Parameter] public string AriaLabelField { get; set; } = ARIA_LABEL;

    /// <summary>
    /// Aria label for the item.
    /// Ignored if collapseAriaLabel or expandAriaLabel is provided.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? AriaLabelFieldSelector { get; set; }

    /// <summary>
    /// Aria label when group is collapsed.
    /// </summary>
    [Parameter] public string CollapseAriaLabelField { get; set; } = COLLAPSE_ARIA_LABEL;

    /// <summary>
    /// Aria label when group is collapsed.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? CollapseAriaLabelFieldSelector { get; set; }

    /// <summary>
    /// Aria label when group is expanded.
    /// </summary>
    [Parameter] public string ExpandAriaLabelField { get; set; } = EXPAND_ARIA_LABEL;

    /// <summary>
    /// Aria label when group is expanded.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? ExpandAriaLabelFieldSelector { get; set; }

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. 
    /// (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public string ForceAnchorField { get; set; } = FORCE_ANCHOR;

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. 
    /// (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? ForceAnchorFieldelector { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// (Optional) The key of the nav item initially selected in manual mode.
    /// </summary>
    [Parameter] public string? InitialSelectedKey { get; set; }

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// A collection of item to display in the navigation bar.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// A list of items to render as children of the current item.
    /// </summary>
    [Parameter] public string ItemsField { get; set; } = TARGET;

    /// <summary>
    /// A list of items to render as children of the current item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, IList<TItem>>>? ItemsFieldSelector { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item button.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME;

    /// <summary>
    /// Name of an icon to render next to the item button.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitIconName>>? IconNameFieldSelector { get; set; }

    /// <summary>
    /// Whether or not the group is in an expanded state.
    /// </summary>
    [Parameter] public string IsExpandedField { get; set; } = IS_EXPANDED;

    /// <summary>
    /// Whether or not the group is in an expanded state.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsExpandedFieldSelector { get; set; }

    /// <summary>
    /// Whether or not the item is disabled.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED;

    /// <summary>
    /// Whether or not the item is disabled.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the item, used when rendering the list of item and for tracking the currently selected item.
    /// </summary>
    [Parameter] public string KeyField { get; set; } = KEY;

    /// <summary>
    /// A unique value to use as a key or id of the item, used when rendering the list of item and for tracking the currently selected item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? KeyFieldSelector { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter] public BitNavListMode Mode { get; set; } = BitNavListMode.Automatic;

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public string NameField { get; set; } = NAME;

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? NameFieldSelector { get; set; }

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemExpand { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    [Parameter] public BitNavListRenderType RenderType { get; set; } = BitNavListRenderType.Normal;

    /// <summary>
    /// The key of the nav item selected by caller.
    /// </summary>
    [Parameter]
    public string? SelectedKey
    {
        get => selectedKey;
        set
        {
            if (value == selectedKey) return;
            selectedKey = value;
            _ = SelectedKeyChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<string> SelectedKeyChanged { get; set; }

    /// <summary>
    /// Custom style for the each item element.
    /// </summary>
    [Parameter] public string StyleField { get; set; } = STYLE;

    /// <summary>
    /// Custom style for the each item element.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? StyleFieldSelector { get; set; }

    /// <summary>
    /// Text for the item tooltip.
    /// </summary>
    [Parameter] public string TitleField { get; set; } = TITLE;

    /// <summary>
    /// Text for the item tooltip.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? TitleFieldSelector { get; set; }

    /// <summary>
    /// Link target, specifies how to open the item link.
    /// </summary>
    [Parameter] public string TargetField { get; set; } = TARGET;

    /// <summary>
    /// Link target, specifies how to open the item link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? TargetFieldSelector { get; set; }

    /// <summary>
    /// URL to navigate for the item link.
    /// </summary>
    [Parameter] public string UrlField { get; set; } = URL;

    /// <summary>
    /// URL to navigate for the item link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? UrlFieldSelector { get; set; }

    protected override string RootElementClass => "bit-nvl";

    protected override async Task OnInitializedAsync()
    {
        if (Mode == BitNavListMode.Automatic)
        {
            _navigationManager.LocationChanged += OnLocationChanged;
        }

        if (InitialSelectedKey.HasValue())
        {
            SelectedKey = InitialSelectedKey;
        }

        foreach (var item in Items)
        {
            SetItemsExpanded(item);
            SetItemsDepth(item);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _internalForceAnchorField = ForceAnchorFieldelector?.GetName() ?? ForceAnchorField;
        _internalKeyField = KeyFieldSelector?.GetName() ?? KeyField;
        _internalNameField = NameFieldSelector?.GetName() ?? NameField;
        _internalTitleField = TitleFieldSelector?.GetName() ?? TitleField;
        _internalUrlField = UrlFieldSelector?.GetName() ?? UrlField;
        _internalAriaCurrentField = AriaCurrentFieldSelector?.GetName() ?? AriaCurrentField;
        _internalExpandAriaLabelField = ExpandAriaLabelFieldSelector?.GetName() ?? ExpandAriaLabelField;
        _internalCollapseAriaLabelField = CollapseAriaLabelFieldSelector?.GetName() ?? CollapseAriaLabelField;
        _internalAriaLabelField = AriaLabelFieldSelector?.GetName() ?? AriaLabelField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalIsExpandedField = IsExpandedFieldSelector?.GetName() ?? IsExpandedField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalStyleField = StyleFieldSelector?.GetName() ?? StyleField;
        _internalTargetField = TargetFieldSelector?.GetName() ?? TargetField;
        _internalItemsField = ItemsFieldSelector?.GetName() ?? ItemsField;

        await base.OnParametersSetAsync();
    }

    private bool GetForceAnchor(TItem item) => item.GetValueFromProperty(_internalForceAnchorField, false);
    private string GetKey(TItem item) => item.GetValueAsObjectFromProperty(_internalKeyField)?.ToString() ?? $"{GetName(item)}_{GetUrl(item)}";
    private string? GetName(TItem item) => item.GetValueAsObjectFromProperty(_internalNameField)?.ToString();
    private string? GetTitle(TItem item) => item.GetValueAsObjectFromProperty(_internalTitleField)?.ToString();
    private string? GetUrl(TItem item) => item.GetValueAsObjectFromProperty(_internalUrlField)?.ToString();
    private BitNavListItemAriaCurrent GetAriaCurrent(TItem item) => item.GetValueFromProperty(_internalAriaCurrentField, BitNavListItemAriaCurrent.Page);
    private string? GetExpandAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalExpandAriaLabelField)?.ToString();
    private string? GetCollapseAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalCollapseAriaLabelField)?.ToString();
    private string? GetAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalAriaLabelField)?.ToString();
    private BitIconName? GetIconName(TItem item) => item.GetValueFromProperty<BitIconName>(_internalIconNameField);
    private bool GetIsExpanded(TItem item) => item.GetValueFromProperty(_internalIsExpandedField, false);
    private bool GetIsEnabled(TItem item) => item.GetValueFromProperty(_internalIsEnabledField, true);
    private string? GetStyle(TItem item) => item.GetValueAsObjectFromProperty(_internalStyleField)?.ToString();
    private string? GetTarget(TItem item) => item.GetValueAsObjectFromProperty(_internalTargetField)?.ToString();
    private List<TItem>? GetItems(TItem item) => item.GetValueFromProperty<List<TItem>>(_internalItemsField);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var currentPageUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        TItem? currentItem = default;
        SetCurrentItemByUrl(Items);

        if (currentItem is not null)
        {
            SelectedKey = GetKey(currentItem);
            StateHasChanged();
        }

        void SetCurrentItemByUrl(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (GetUrl(item)?.ToLower(Thread.CurrentThread.CurrentCulture) == currentPageUrl.ToLower(Thread.CurrentThread.CurrentCulture))
                {
                    currentItem = item;
                    return;
                }
                else if (GetItems(item) is not null)
                {
                    SetCurrentItemByUrl(GetItems(item)!);
                }
            }
        }
    }

    private void SetItemsDepth(TItem item, int depth = 0)
    {
        _itemsDepth.Add(GetKey(item), depth);

        if (GetItems(item) is not null)
        {
            foreach (var childItem in GetItems(item)!)
            {
                SetItemsDepth(childItem, depth + 1);
            }

            depth = 0;
        }
    }

    private void SetItemsExpanded(TItem item)
    {
        var isExpanded = (GetItems(item) is not null && GetItems(item)!.Any(ci => GetKey(ci) == SelectedKey)) || GetIsExpanded(item);

        _itemsExpanded.Add(GetKey(item), isExpanded);

        if (GetItems(item) is not null)
        {
            foreach (var childItem in GetItems(item)!)
            {
                SetItemsExpanded(childItem);
            }
        }
    }

    private async void HandleOnItemClick(TItem item)
    {
        if (GetIsEnabled(item) == false) return;

        if (Mode == BitNavListMode.Manual && GetItems(item) is null)
        {
            SelectedKey = GetKey(item);
        }
        else if (GetUrl(item).HasNoValue())
        {
            await HandleOnItemExpand(item);
        }

        await OnItemClick.InvokeAsync(item);
    }

    private async Task HandleOnItemExpand(TItem item)
    {
        if (GetIsEnabled(item) is false || GetItems(item) is null) return;

        var itemKey = GetKey(item);
        var oldIsExpanded = _itemsExpanded[itemKey];
        _itemsExpanded.Remove(itemKey);
        _itemsExpanded.Add(itemKey, !oldIsExpanded);

        await OnItemExpand.InvokeAsync(item);
    }

    private string GetItemClass(TItem item)
    {
        StringBuilder classBuilder = new StringBuilder();

        var enabledClass = GetIsEnabled(item) ? "enabled" : "disabled";
        var hasUrlClass = GetUrl(item).HasNoValue() ? "nourl" : "hasurl";

        classBuilder.Append($"link-{enabledClass}-{hasUrlClass}");

        if (SelectedKey.HasValue() && SelectedKey == GetKey(item))
        {
            classBuilder.Append(" selected");
        }

        return classBuilder.ToString();
    }

    private bool IsRelativeUrl(TItem item)
    {
        string url = GetUrl(item) ?? string.Empty;
        var regex = new Regex(@"!/^[a-z0-9+-.]+:\/\//i");
        return regex.IsMatch(url);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Mode == BitNavListMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
