namespace Bit.BlazorUI;

public partial class BitNavBar<TItem>
{
    /// <summary>
    /// The accent color that fills the hovered and the selected item of the navbar.
    /// While it is not set, the selection is conveyed by the <see cref="Color"/> of the item alone.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Items to render as children.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the navbar.
    /// </summary>
    [Parameter] public BitNavBarClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the navbar, used for the icon, the text and the indicator of the selected item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The initially selected item in manual mode.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Renders the nav bar in a width to only fit its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// Renders the nav bar in full width of its container element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// Only renders the text of the selected item and leaves the rest of the items with their icon alone,
    /// which is how a navigation bar keeps its labels readable while holding more destinations.
    /// Ignored while <see cref="IconOnly"/> is enabled.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool HideUnselectedText { get; set; }

    /// <summary>
    /// Only renders the icon of each navbar item.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// Renders the icon and the text of each item side by side instead of stacking the text under the icon.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool InlineText { get; set; }

    /// <summary>
    /// A collection of items to display in the navbar.
    /// </summary>
    [Parameter] public IList<TItem> Items { get; set; } = [];

    /// <summary>
    /// Gives every item an equal share of the navbar so that the items evenly fill it, which is how a
    /// navigation bar keeps its destinations on a predictable grid instead of letting a long label claim
    /// more room than a short one. By default each item only takes the width of its own content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Justified { get; set; }

    /// <summary>
    /// Used to customize how content inside the item is rendered.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// Modifies how the URL of an item is matched against the current URL in the automatic mode.
    /// The Match of an item takes precedence over this value, and the default is an exact match.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnUrlMatchingChanged))]
    public BitNavMatch? Match { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnUrlMatchingChanged))]
    public BitNavMode Mode { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitNavBarNameSelectors<TItem>? NameSelectors { get; set; }

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
    /// Reserves the safe area of the device (the home indicator of a phone, for instance) under the navbar,
    /// so a bar pinned to the bottom of the screen is not overlapped by it.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool SafeArea { get; set; }

    /// <summary>
    /// Selected item to show in the navbar.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedItem))]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Takes the navbar out of the tab sequence as a single stop: only the selected item (or the first one,
    /// while nothing is selected) is tabbable and the arrow keys move between the items, exactly like a
    /// toolbar. By default every item is a tab stop of its own, the way the links of a navigation are.
    /// </summary>
    [Parameter] public bool SingleTabStop { get; set; }

    /// <summary>
    /// The size of the navbar.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the navbar.
    /// </summary>
    [Parameter] public BitNavBarClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the items of the navbar in a column, which turns it into a vertical navigation rail.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }

    /// <summary>
    /// Lets the arrow keys wrap around at both ends of the navbar, from the last item to the first one and
    /// back, the way the toolbar pattern does. By default the focus stops at the ends instead, so the bar
    /// keeps a stable notion of a first and a last item.
    /// </summary>
    [Parameter] public bool WrapNavigation { get; set; }
}
