using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNavList<TItem> : IDisposable where TItem : class
{
    private const string FORCE_ANCHOR = "ForceAnchor";
    private const string TEXT = "Text";
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

    private bool SelectedItemHasBeenSet;
    private TItem? selectedItem;

    private string _internalForceAnchorField = FORCE_ANCHOR;
    private string _internalTextField = TEXT;
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

    internal Dictionary<TItem, bool> _itemExpandStates = new();

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
    [Parameter] public Expression<Func<TItem, BitNavItemAriaCurrent>>? AriaCurrentFieldSelector { get; set; }

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
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

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
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

    /// <summary>
    /// Callback invoked when an item is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Callback invoked when an item is selected.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Callback invoked when a group header is clicked and Expanded or Collapse.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemToggle { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    [Parameter] public BitNavRenderType RenderType { get; set; } = BitNavRenderType.Normal;

    /// <summary>
    /// Selected item to show in Nav.
    /// </summary>
    [Parameter]
    public TItem? SelectedItem
    {
        get => selectedItem;
        set
        {
            if (value == selectedItem) return;
            selectedItem = value;
            if (value is not null) ExpandSelectedItemParents(Items);
            SelectedItemChanged.InvokeAsync(selectedItem);
        }
    }
    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT;

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? TextFieldSelector { get; set; }

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
        _internalForceAnchorField = ForceAnchorFieldelector?.GetName() ?? ForceAnchorField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
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

        foreach (var item in Flatten(Items))
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
                SelectedItem = DefaultSelectedItem;
            }
        }

        await base.OnInitializedAsync();
    }

    internal bool GetForceAnchor(TItem item) => item.GetValueFromProperty(_internalForceAnchorField, false);
    internal string? GetText(TItem item) => item.GetValueAsObjectFromProperty(_internalTextField)?.ToString();
    internal string? GetTitle(TItem item) => item.GetValueAsObjectFromProperty(_internalTitleField)?.ToString();
    internal string? GetUrl(TItem item) => item.GetValueAsObjectFromProperty(_internalUrlField)?.ToString();
    internal BitNavItemAriaCurrent GetAriaCurrent(TItem item) => item.GetValueFromProperty(_internalAriaCurrentField, BitNavItemAriaCurrent.Page);
    internal string? GetExpandAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalExpandAriaLabelField)?.ToString();
    internal string? GetCollapseAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalCollapseAriaLabelField)?.ToString();
    internal string? GetAriaLabel(TItem item) => item.GetValueAsObjectFromProperty(_internalAriaLabelField)?.ToString();
    internal BitIconName? GetIconName(TItem item) => item.GetValueFromProperty<BitIconName>(_internalIconNameField);
    internal bool? GetIsExpanded(TItem item) => item.GetValueFromProperty<bool?>(_internalIsExpandedField, null);
    private void SetIsExpanded(TItem item, bool value) => item.SetValueToProperty(_internalIsExpandedField, value);
    internal bool GetIsEnabled(TItem item) => item.GetValueFromProperty(_internalIsEnabledField, true);
    internal string? GetStyle(TItem item) => item.GetValueAsObjectFromProperty(_internalStyleField)?.ToString();
    internal string? GetTarget(TItem item) => item.GetValueAsObjectFromProperty(_internalTargetField)?.ToString();
    internal IList<TItem> GetItems(TItem item) => item.GetValueFromProperty(_internalItemsField , new List<TItem>())!;

    private List<TItem> Flatten(IList<TItem> e) => e.SelectMany(c => Flatten(GetItems(c))).Concat(e).ToList();

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetSelectedItemByCurrentUrl();

        StateHasChanged();
    }

    private void SetSelectedItemByCurrentUrl()
    {
        var currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var currentItem = Flatten(Items).FirstOrDefault(item => GetUrl(item) == currentUrl);

        if (currentItem is not null)
        {
            SelectedItem = currentItem;
        }
    }

    private void SetItemExpanded(TItem item, bool value)
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

        if (isExpanded is not null) return isExpanded.Value;

        return _itemExpandStates[item];
    }

    private bool ExpandSelectedItemParents(IList<TItem> items)
    {
        foreach (var parent in items)
        {
            if (parent == SelectedItem || (GetItems(parent).Any() && ExpandSelectedItemParents(GetItems(parent))))
            {
                SetItemExpanded(parent, true);
                return true;
            }
        }

        return false;
    }

    internal async void HandleOnClick(TItem item)
    {
        if (GetIsEnabled(item) == false) return;

        if (GetItems(item).Any() && GetUrl(item).HasNoValue())
        {
            await ToggleItem(item);
        }
        else if (Mode == BitNavMode.Manual)
        {
            SelectedItem = item;
            await OnSelectItem.InvokeAsync(item);
            StateHasChanged();
        }

        await OnItemClick.InvokeAsync(item);
    }

    internal async Task ToggleItem(TItem item)
    {
        if (GetIsEnabled(item) is false || GetItems(item).Any() is false) return;

        SetItemExpanded(item, !GetItemExpanded(item));

        await OnItemToggle.InvokeAsync(item);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Mode == BitNavMode.Automatic)
        {
            _navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
