namespace Bit.BlazorUI;

public partial class BitNavBar<TItem>
{
    /// <summary>
    /// How the items are distributed along the navbar: packed at its start, its center or its end, or
    /// spread over it. While it is not set the items of a horizontal navbar are spread evenly over its
    /// width and the items of a <see cref="Vertical"/> rail are packed at its top.
    /// <br />
    /// <see cref="BitAlignment.Baseline"/> and <see cref="BitAlignment.Stretch"/> carry no distribution of
    /// their own and are left at the default; use <see cref="Justified"/> to have the items fill the navbar.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Keeps the order of the registered options in sync with the markup order of the options, even when an
    /// option is added, removed or reordered conditionally after the first render (an option that shows up
    /// later registers itself at the end of the list, no matter where in the markup it sits, which leaves
    /// the keyboard moving between the items in another order than the one they are rendered in).
    /// This is achieved by reading the DOM order of the options after each render, so it adds a JS interop
    /// call per change and is opt-in. It only applies to the options; the Items collection already keeps
    /// its own order.
    /// </summary>
    [Parameter] public bool AutoReorderOptions { get; set; }

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
    /// Fills the hovered and the selected item of the navbar with the <see cref="Color"/> of the navbar.
    /// While it is not enabled, the selection is conveyed by the color of the item alone.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Filled { get; set; }

    /// <summary>
    /// Renders the nav bar in a width to only fit its content.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FitWidth { get; set; }

    /// <summary>
    /// The content rendered after the items of the navbar, outside of the list they form: the trailing
    /// actions of a bar, or the account button at the bottom of a navigation rail.
    /// </summary>
    [Parameter] public RenderFragment? FooterTemplate { get; set; }

    /// <summary>
    /// Renders the nav bar in full width of its container element.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The content rendered before the items of the navbar, outside of the list they form: the logo or the
    /// menu button of a bar, or the button a navigation rail is conventionally headed with.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

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
    /// The shape of the indicator that marks the selected item: a line along the edge of the item, or the
    /// pill a Material navigation bar draws behind the icon of its current destination. While it is not set,
    /// the selection is conveyed by the color of the item and by the fill <see cref="Filled"/> gives it.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitNavBarIndicator? Indicator { get; set; }

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
    /// Whether the <see cref="ItemTemplate"/> is rendered inside the anchor (or the button) each item is, or
    /// replaces it altogether, which is what items that are controls of their own need, since an interactive
    /// element cannot be nested in another one. Replaced items are left out of the keyboard navigation of the
    /// navbar, and whatever they render owns its own clicks, its own focus and its own accessible name.
    /// The mode of an item applies to the template of that item, and this one to the navbar's own template.
    /// </summary>
    [Parameter] public BitNavItemTemplateRenderMode ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// Modifies how the URL of an item is matched against the current URL in the automatic mode.
    /// The Match of an item takes precedence over this value, and the default is an exact match.
    /// <br />
    /// A Wildcard (or a Regex) URL is run as the pattern it was written as, so unlike an Exact or a Prefix
    /// URL it is never normalized: it has to be app-relative and to carry its leading slash itself, as in
    /// "/products/*".
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
    /// Lets the items scroll along the navbar instead of being squeezed into it, which is what a bar (or a
    /// rail) holding more destinations than it has room for needs. The scrollbar itself is hidden, the items
    /// keep the size of their own content, and the selected one is scrolled into view as the selection moves.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Scrollable { get; set; }

    /// <summary>
    /// Selected item to show in the navbar.
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedItem))]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// Selects an item as soon as the focus reaches it, so walking the navbar with the arrow keys switches
    /// the selection along with it, the way the tabs of a tab list do. It only applies to the
    /// <see cref="BitNavMode.Manual"/> mode, where the selection is the navbar's own; in the automatic mode
    /// the current URL is what selects an item.
    /// </summary>
    [Parameter] public bool SelectOnFocus { get; set; }

    /// <summary>
    /// Takes the navbar out of the tab sequence as a single stop: only one item is tabbable and the arrow
    /// keys move between the items, exactly like a toolbar. The stop is the item the focus was last on, so
    /// Tab returns to where the reader left it; before the navbar has ever been focused it is the selected
    /// item, and the first focusable one while nothing is selected either. By default every item is a tab
    /// stop of its own, the way the links of a navigation are.
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
