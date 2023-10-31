using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

public partial class BitNav<TItem> : IDisposable where TItem : class
{
    private const string ARIA_CURRENT_FIELD = nameof(BitNavItem.AriaCurrent);
    private const string ARIA_LABEL_FIELD = nameof(BitNavItem.AriaLabel);
    private const string CHILD_ITEMS_FIELD = nameof(BitNavItem.ChildItems);
    private const string COLLAPSE_ARIA_LABEL_FIELD = nameof(BitNavItem.CollapseAriaLabel);
    private const string DATA_FIELD = nameof(BitNavItem.Data);
    private const string DESCRIPTION_FIELD = nameof(BitNavItem.Description);
    private const string EXPAND_ARIA_LABEL_FIELD = nameof(BitNavItem.ExpandAriaLabel);
    private const string FORCE_ANCHOR_FIELD = nameof(BitNavItem.ForceAnchor);
    private const string ICON_NAME_FIELD = nameof(BitNavItem.IconName);
    private const string IS_ENABLED_FIELD = nameof(BitNavItem.IsEnabled);
    private const string IS_EXPANDED_FIELD = nameof(BitNavItem.IsExpanded);
    private const string KEY_FIELD = nameof(BitNavItem.Key);
    private const string STYLE_FIELD = nameof(BitNavItem.Style);
    private const string TARGET_FIELD = nameof(BitNavItem.Target);
    private const string TEMPLATE_FIELD = nameof(BitNavItem.Template);
    private const string TEMPLATE_RENDER_MODE_FIELD = nameof(BitNavItem.TemplateRenderMode);
    private const string TEXT_FIELD = nameof(BitNavItem.Text);
    private const string TITLE_FIELD = nameof(BitNavItem.Title);
    private const string URL_FIELD = nameof(BitNavItem.Url);
    private const string ADDITIONAL_URLS_FIELD = nameof(BitNavItem.AdditionalUrls);


    private bool SelectedItemHasBeenSet;
    private TItem? selectedItem;

    private string _internalAriaCurrentField = ARIA_CURRENT_FIELD;
    private string _internalAriaLabelField = ARIA_LABEL_FIELD;
    private string _internalChildItemsField = CHILD_ITEMS_FIELD;
    private string _internalCollapseAriaLabelField = COLLAPSE_ARIA_LABEL_FIELD;
    private string _internalDataField = DATA_FIELD;
    private string _internalDescriptionField = DESCRIPTION_FIELD;
    private string _internalExpandAriaLabelField = EXPAND_ARIA_LABEL_FIELD;
    private string _internalForceAnchorField = FORCE_ANCHOR_FIELD;
    private string _internalIconNameField = ICON_NAME_FIELD;
    private string _internalIsEnabledField = IS_ENABLED_FIELD;
    private string _internalIsExpandedField = IS_EXPANDED_FIELD;
    private string _internalKeyField = KEY_FIELD;
    private string _internalStyleField = STYLE_FIELD;
    private string _internalTargetField = TARGET_FIELD;
    private string _internalTemplateField = TEMPLATE_FIELD;
    private string _internalTemplateRenderModeField = TEMPLATE_RENDER_MODE_FIELD;
    private string _internalTextField = TEXT_FIELD;
    private string _internalTitleField = TITLE_FIELD;
    private string _internalUrlField = URL_FIELD;
    private string _internalAdditionalUrlsField = ADDITIONAL_URLS_FIELD;


    internal List<TItem> _items = new();
    private IEnumerable<TItem>? _oldItems = default!;
    internal Dictionary<TItem, bool> _itemExpandStates = new();
    private bool _disposed;



    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// Aria-current token for active nav item.
    /// Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public string AriaCurrentField { get; set; } = ARIA_CURRENT_FIELD;

    /// <summary>
    /// Aria-current token for active nav item.
    /// Must be a valid token value, and defaults to 'page'.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitNavAriaCurrent>>? AriaCurrentFieldSelector { get; set; }

    /// <summary>
    /// Aria label for the item.
    /// Ignored if collapseAriaLabel or expandAriaLabel is provided.
    /// </summary>
    [Parameter] public string AriaLabelField { get; set; } = ARIA_LABEL_FIELD;

    /// <summary>
    /// Aria label for the item.
    /// Ignored if collapseAriaLabel or expandAriaLabel is provided.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? AriaLabelFieldSelector { get; set; }

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// A list of items to render as children of the current item.
    /// </summary>
    [Parameter] public string ChildItemsField { get; set; } = CHILD_ITEMS_FIELD;

    /// <summary>
    /// A list of items to render as children of the current item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, IList<TItem>>>? ChildItemsFieldSelector { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavClassStyles? Classes { get; set; }

    /// <summary>
    /// Aria label when group is collapsed.
    /// </summary>
    [Parameter] public string CollapseAriaLabelField { get; set; } = COLLAPSE_ARIA_LABEL_FIELD;

    /// <summary>
    /// Aria label when group is collapsed.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? CollapseAriaLabelFieldSelector { get; set; }

    /// <summary>
    /// The field name of the Data property.
    /// </summary>
    [Parameter] public string DataField { get; set; } = DATA_FIELD;

    /// <summary>
    /// The field selector of the Data property.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object>>? DataFieldSelector { get; set; }

    /// <summary>
    /// The field name of the Description property.
    /// </summary>
    [Parameter] public string DescriptionField { get; set; } = DESCRIPTION_FIELD;

    /// <summary>
    /// The field selector of the Description property.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? DescriptionFieldSelector { get; set; }

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Aria label when group is expanded.
    /// </summary>
    [Parameter] public string ExpandAriaLabelField { get; set; } = EXPAND_ARIA_LABEL_FIELD;

    /// <summary>
    /// Aria label when group is expanded.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? ExpandAriaLabelFieldSelector { get; set; }

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. 
    /// (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public string ForceAnchorField { get; set; } = FORCE_ANCHOR_FIELD;

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. Set this property to true to override that behavior. 
    /// (Links without onClick defined will render as anchors by default.)
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? ForceAnchorFieldSelector { get; set; }

    /// <summary>
    /// Used to customize how content inside the group header is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom HeaderTemplate.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode HeaderTemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Name of an icon to render next to the item button.
    /// </summary>
    [Parameter] public string IconNameField { get; set; } = ICON_NAME_FIELD;

    /// <summary>
    /// Name of an icon to render next to the item button.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? IconNameFieldSelector { get; set; }

    /// <summary>
    /// Whether or not the item is disabled.
    /// </summary>
    [Parameter] public string IsEnabledField { get; set; } = IS_ENABLED_FIELD;

    /// <summary>
    /// Whether or not the item is disabled.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsEnabledFieldSelector { get; set; }

    /// <summary>
    /// Whether or not the group is in an expanded state.
    /// </summary>
    [Parameter] public string IsExpandedField { get; set; } = IS_EXPANDED_FIELD;

    /// <summary>
    /// Whether or not the group is in an expanded state.
    /// </summary>
    [Parameter] public Expression<Func<TItem, bool>>? IsExpandedFieldSelector { get; set; }

    /// <summary>
    /// A collection of item to display in the navigation bar.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = new List<TItem>();

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The render mode of the custom ItemTemplate.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode ItemTemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// A unique value to use as a key or id of the item.
    /// </summary>
    [Parameter] public string KeyField { get; set; } = KEY_FIELD;

    /// <summary>
    /// A unique value to use as a key or id of the item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? KeyFieldSelector { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter] public BitNavMode Mode { get; set; } = BitNavMode.Automatic;

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
            _ = SelectedItemChanged.InvokeAsync(value);

            if (value is not null)
            {
                ExpandParents(_items);
            }
        }
    }
    [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

    /// <summary>
    /// Custom style for the each item element.
    /// </summary>
    [Parameter] public string StyleField { get; set; } = STYLE_FIELD;

    /// <summary>
    /// Custom style for the each item element.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? StyleFieldSelector { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitNav component.
    /// </summary>
    [Parameter] public BitNavClassStyles? Styles { get; set; }

    /// <summary>
    /// Link target, specifies how to open the item link.
    /// </summary>
    [Parameter] public string TargetField { get; set; } = TARGET_FIELD;

    /// <summary>
    /// Link target, specifies how to open the item link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TargetFieldSelector { get; set; }

    /// <summary>
    /// The field name of the Template property in the nav item class.
    /// </summary>
    [Parameter] public string TemplateField { get; set; } = TEMPLATE_FIELD;

    /// <summary>
    /// The field selector of the Template property in the nav item class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, RenderFragment>>? TemplateFieldSelector { get; set; }

    /// <summary>
    /// The field name of the TemplateRenderMode property in the nav item class.
    /// </summary>
    [Parameter] public string TemplateRenderModeField { get; set; } = TEMPLATE_RENDER_MODE_FIELD;

    /// <summary>
    /// The field selector of the TemplateRenderMode property in the nav item class.
    /// </summary>
    [Parameter] public Expression<Func<TItem, BitNavItemTemplateRenderMode>>? TemplateRenderModeFieldSelector { get; set; }

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public string TextField { get; set; } = TEXT_FIELD;

    /// <summary>
    /// Text to render for the item.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TextFieldSelector { get; set; }

    /// <summary>
    /// Text for the item tooltip.
    /// </summary>
    [Parameter] public string TitleField { get; set; } = TITLE_FIELD;

    /// <summary>
    /// Text for the item tooltip.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? TitleFieldSelector { get; set; }

    /// <summary>
    /// URL to navigate for the item link.
    /// </summary>
    [Parameter] public string UrlField { get; set; } = URL_FIELD;

    /// <summary>
    /// URL to navigate for the item link.
    /// </summary>
    [Parameter] public Expression<Func<TItem, string>>? UrlFieldSelector { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected item by the current URL.
    /// </summary>
    [Parameter] public string AdditionalUrlsField { get; set; } = ADDITIONAL_URLS_FIELD;

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected item by the current URL.
    /// </summary>
    [Parameter] public Expression<Func<TItem, IEnumerable<string>>>? AdditionalUrlsFieldSelector { get; set; }


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

        return item.GetValueFromProperty(_internalAriaCurrentField, BitNavAriaCurrent.Page);
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

        return item.GetValueFromProperty<string?>(_internalAriaLabelField);
    }

    internal List<TItem> GetChildItems(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.ChildItems as List<TItem> ?? new List<TItem>();
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Items as List<TItem> ?? new List<TItem>();
        }

        return item.GetValueFromProperty(_internalChildItemsField, new List<TItem>())!;
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

        return item.GetValueFromProperty<string?>(_internalCollapseAriaLabelField);
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

        return item.GetValueFromProperty<string?>(_internalDescriptionField);
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

        return item.GetValueFromProperty<string?>(_internalExpandAriaLabelField);
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

        return item.GetValueFromProperty(_internalForceAnchorField, false);
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

        return item.GetValueFromProperty<string?>(_internalIconNameField);
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

        return item.GetValueFromProperty(_internalIsEnabledField, true);
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

        return item.GetValueFromProperty<bool?>(_internalIsExpandedField, null);
    }

    internal string? GetKey(TItem item)
    {
        if (item is BitNavItem navItem)
        {
            return navItem.Key ?? navItem.Text;
        }

        if (item is BitNavOption navOption)
        {
            return navOption.Key ?? navOption.Text;
        }

        return item.GetValueFromProperty(_internalKeyField, GetText(item) ?? string.Empty);
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

        return item.GetValueFromProperty<string?>(_internalStyleField);
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

        return item.GetValueFromProperty<string?>(_internalTargetField);
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

        return item.GetValueFromProperty<RenderFragment<TItem>?>(_internalTemplateField);
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

        return item.GetValueFromProperty<BitNavItemTemplateRenderMode>(_internalTemplateRenderModeField);
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

        return item.GetValueFromProperty<string>(_internalTextField);
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

        return item.GetValueFromProperty<string?>(_internalTitleField);
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

        return item.GetValueFromProperty<string?>(_internalUrlField);
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

        return item.GetValueFromProperty<IEnumerable<string>?>(_internalAdditionalUrlsField);
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
        SelectedItem = item;
        await OnSelectItem.InvokeAsync(item);
        StateHasChanged();
    }

    internal void RegisterOption(BitNavOption option)
    {
        _items.Add((option as TItem)!);
        StateHasChanged();
    }

    internal void UnregisterOption(BitNavOption option)
    {
        _items.Remove((option as TItem)!);
        StateHasChanged();
    }



    protected override string RootElementClass => "bit-nav";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnInitializedAsync()
    {
        _internalAriaCurrentField = AriaCurrentFieldSelector?.GetName() ?? AriaCurrentField;
        _internalAriaLabelField = AriaLabelFieldSelector?.GetName() ?? AriaLabelField;
        _internalChildItemsField = ChildItemsFieldSelector?.GetName() ?? ChildItemsField;
        _internalCollapseAriaLabelField = CollapseAriaLabelFieldSelector?.GetName() ?? CollapseAriaLabelField;
        _internalDataField = DataFieldSelector?.GetName() ?? DataField;
        _internalDescriptionField = DescriptionFieldSelector?.GetName() ?? DescriptionField;
        _internalExpandAriaLabelField = ExpandAriaLabelFieldSelector?.GetName() ?? ExpandAriaLabelField;
        _internalForceAnchorField = ForceAnchorFieldSelector?.GetName() ?? ForceAnchorField;
        _internalIconNameField = IconNameFieldSelector?.GetName() ?? IconNameField;
        _internalIsEnabledField = IsEnabledFieldSelector?.GetName() ?? IsEnabledField;
        _internalIsExpandedField = IsExpandedFieldSelector?.GetName() ?? IsExpandedField;
        _internalKeyField = KeyFieldSelector?.GetName() ?? KeyField;
        _internalStyleField = StyleFieldSelector?.GetName() ?? StyleField;
        _internalTargetField = TargetFieldSelector?.GetName() ?? TargetField;
        _internalTemplateField = TemplateFieldSelector?.GetName() ?? TemplateField;
        _internalTemplateRenderModeField = TargetFieldSelector?.GetName() ?? TemplateRenderModeField;
        _internalTextField = TextFieldSelector?.GetName() ?? TextField;
        _internalTitleField = TitleFieldSelector?.GetName() ?? TitleField;
        _internalUrlField = UrlFieldSelector?.GetName() ?? UrlField;
        _internalAdditionalUrlsField = AdditionalUrlsFieldSelector?.GetName() ?? AdditionalUrlsField;


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
                SelectedItem = DefaultSelectedItem;
            }
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent is null && Items != _oldItems)
        {
            _items = Items?.ToList() ?? new();
            _oldItems = Items;
        }

        await base.OnParametersSetAsync();
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

        if (currentItem is not null)
        {
            SelectedItem = currentItem;
        }
    }

    private bool ExpandParents(IList<TItem> items)
    {
        foreach (var parent in items)
        {
            if (parent == SelectedItem || (GetChildItems(parent).Any() && ExpandParents(GetChildItems(parent))))
            {
                SetItemExpanded(parent, true);
                return true;
            }
        }

        return false;
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

        item.SetValueToProperty(_internalIsExpandedField, value);
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
