namespace Bit.BlazorUI;

public partial class BitNav<TItem>
{
    /// <summary>
    /// The accent color of the nav.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Expands all items on first render.
    /// </summary>
    [Parameter] public bool AllExpanded { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of each nav item.
    /// </summary>
    [Parameter] public string? ChevronDownIcon { get; set; }

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetParameters))]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav.
    /// </summary>
    [Parameter] public BitNavClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the nav that is only used for colored parts like icons.
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
    [Parameter] public BitNavItemTemplateRenderMode HeaderTemplateRenderMode { get; set; }

    /// <summary>
    /// Only renders the icon of each nav item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// The indentation padding in px for items without children (compensation space for chevron icon).
    /// </summary>
    [Parameter] public int IndentPadding { get; set; } = 27;

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    [Parameter] public int IndentReversedPadding { get; set; } = 4;

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    [Parameter] public int IndentValue { get; set; } = 16;

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
    [Parameter] public BitNavItemTemplateRenderMode ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// Gets or sets a value representing the global URL matching behavior of the nav.
    /// </summary>
    [Parameter] public BitNavMatch? Match { get; set; }

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
    /// Hides all collapse/expand buttons and remove their spaces at the start of each node.
    /// </summary>
    [Parameter] public bool NoCollapse { get; set; }

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
    [Parameter] public BitNavRenderType RenderType { get; set; }

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
}
