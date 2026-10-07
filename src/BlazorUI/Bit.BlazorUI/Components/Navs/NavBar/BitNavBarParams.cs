namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitNavBar{TItem}"/> component.
/// </summary>
/// <remarks>
/// The navbar is generic over its item type, but the parameters worth sharing between the navbars of an app
/// are not: the ones typed over TItem - the items themselves, the selection, the item template, the name
/// selectors and the event callbacks - stay on the instance, which is what keeps this object usable from a
/// single non-generic <see cref="BitParams"/> list no matter which of the three item APIs each navbar under
/// it uses. The header and footer content are left out too, since they belong to a single navbar.
/// </remarks>
public class BitNavBarParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitNavBar{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitNavBar value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitNavBar<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// How the items are distributed along the navbar.
    /// </summary>
    public BitAlignment? Alignment { get; set; }

    /// <summary>
    /// Keeps the order of the registered options in sync with their markup order (options API only).
    /// </summary>
    public bool? AutoReorderOptions { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the navbar.
    /// </summary>
    public BitNavBarClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the navbar, used for the icon, the text and the indicator of the selected item.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Fills the hovered and the selected item with the color of the navbar.
    /// </summary>
    public bool? Filled { get; set; }

    /// <summary>
    /// Renders the navbar in a width to only fit its content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Draws the Line indicator along the opposite edge of the selected item.
    /// </summary>
    public bool? FlipIndicator { get; set; }

    /// <summary>
    /// Renders the navbar in full width of its container element.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// Only renders the text of the selected item.
    /// </summary>
    public bool? HideUnselectedText { get; set; }

    /// <summary>
    /// Only renders the icon of each item.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// The shape of the indicator that marks the selected item.
    /// </summary>
    public BitNavBarIndicator? Indicator { get; set; }

    /// <summary>
    /// Renders the icon and the text of each item side by side.
    /// </summary>
    public bool? InlineText { get; set; }

    /// <summary>
    /// Whether the item template of the navbar renders inside each item or replaces it.
    /// </summary>
    public BitNavItemTemplateRenderMode? ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// Gives every item an equal share of the navbar.
    /// </summary>
    public bool? Justified { get; set; }

    /// <summary>
    /// How the URL of an item is matched against the current URL in the automatic mode.
    /// </summary>
    public BitNavMatch? Match { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    public BitNavMode? Mode { get; set; }

    /// <summary>
    /// The text the items opening a new tab are announced with. The default is "(opens in a new tab)"; an empty
    /// value takes the announcement off.
    /// </summary>
    public string? NewTabHint { get; set; }

    /// <summary>
    /// Stops the items opening a new tab from announcing that they do.
    /// </summary>
    public bool? NoNewTabHint { get; set; }

    /// <summary>
    /// Lets the click and the select events of the already selected item through: on a click in the manual
    /// mode, and on a navigation back to its URL in the automatic mode. By default they are swallowed.
    /// </summary>
    public bool? Reselectable { get; set; }

    /// <summary>
    /// Reserves the bottom safe area of the device under the navbar.
    /// </summary>
    public bool? SafeArea { get; set; }

    /// <summary>
    /// Lets the items scroll along the navbar instead of being squeezed into it.
    /// </summary>
    public bool? Scrollable { get; set; }

    /// <summary>
    /// Selects an item as soon as the focus reaches it (manual mode only).
    /// </summary>
    public bool? SelectOnFocus { get; set; }

    /// <summary>
    /// Makes the whole navbar a single tab stop, moving between the items with the arrow keys.
    /// </summary>
    public bool? SingleTabStop { get; set; }

    /// <summary>
    /// The size of the navbar.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the navbar.
    /// </summary>
    public BitNavBarClassStyles? Styles { get; set; }

    /// <summary>
    /// Stacks the items in a column, which turns the navbar into a vertical navigation rail.
    /// </summary>
    public bool? Vertical { get; set; }

    /// <summary>
    /// Lets the arrow keys wrap around at both ends of the navbar.
    /// </summary>
    public bool? WrapNavigation { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitNavBar{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitNavBar{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitNavBar"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitNavBar"/>.
    /// </remarks>
    /// <param name="bitNavBar">
    /// The <see cref="BitNavBar{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitNavBar<TItem> bitNavBar) where TItem : class
    {
        if (bitNavBar is null) return;

        UpdateBaseParameters(bitNavBar);

        // Mode and Match decide which item the current URL points at, so a change to either re-runs the match.
        // Only an actual change does: the cascade is re-applied on every parameter set, and a re-match on each
        // one would re-fire OnSelectItem on a Reselectable navbar.
        var urlMatchingChanged = false;

        if (Alignment.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Alignment), Alignment.Value, static n => n.Alignment, static (n, v) => n.Alignment = v);
        }

        if (AutoReorderOptions.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(AutoReorderOptions), AutoReorderOptions.Value, static n => n.AutoReorderOptions, static (n, v) => n.AutoReorderOptions = v);
        }

        if (Classes is not null)
        {
            bitNavBar.TakeFromCascade(nameof(Classes), Classes, static n => n.Classes, static (n, v) => n.Classes = v);
        }

        if (Color.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Color), Color.Value, static n => n.Color, static (n, v) => n.Color = v);
        }

        if (Filled.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Filled), Filled.Value, static n => n.Filled, static (n, v) => n.Filled = v);
        }

        if (FitWidth.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static n => n.FitWidth, static (n, v) => n.FitWidth = v);
        }

        if (FlipIndicator.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(FlipIndicator), FlipIndicator.Value, static n => n.FlipIndicator, static (n, v) => n.FlipIndicator = v);
        }

        if (FullWidth.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static n => n.FullWidth, static (n, v) => n.FullWidth = v);
        }

        if (HideUnselectedText.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(HideUnselectedText), HideUnselectedText.Value, static n => n.HideUnselectedText, static (n, v) => n.HideUnselectedText = v);
        }

        if (IconOnly.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static n => n.IconOnly, static (n, v) => n.IconOnly = v);
        }

        if (Indicator.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Indicator), Indicator.Value, static n => n.Indicator, static (n, v) => n.Indicator = v);
        }

        if (InlineText.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(InlineText), InlineText.Value, static n => n.InlineText, static (n, v) => n.InlineText = v);
        }

        if (ItemTemplateRenderMode.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(ItemTemplateRenderMode), ItemTemplateRenderMode.Value, static n => n.ItemTemplateRenderMode, static (n, v) => n.ItemTemplateRenderMode = v);
        }

        if (Justified.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Justified), Justified.Value, static n => n.Justified, static (n, v) => n.Justified = v);
        }

        if (Match.HasValue && bitNavBar.TakeFromCascade(nameof(Match), Match.Value, static n => n.Match, static (n, v) => n.Match = v))
        {
            urlMatchingChanged = true;
        }

        if (Mode.HasValue && bitNavBar.TakeFromCascade(nameof(Mode), Mode.Value, static n => n.Mode, static (n, v) => n.Mode = v))
        {
            urlMatchingChanged = true;
        }

        // an empty hint is a value of its own - the one that takes the announcement off - so only null is
        // what leaves the component to its default.
        if (NewTabHint is not null)
        {
            bitNavBar.TakeFromCascade(nameof(NewTabHint), NewTabHint, static n => n.NewTabHint, static (n, v) => n.NewTabHint = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static n => n.NoNewTabHint, static (n, v) => n.NoNewTabHint = v);
        }

        if (Reselectable.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Reselectable), Reselectable.Value, static n => n.Reselectable, static (n, v) => n.Reselectable = v);
        }

        if (SafeArea.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(SafeArea), SafeArea.Value, static n => n.SafeArea, static (n, v) => n.SafeArea = v);
        }

        if (Scrollable.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Scrollable), Scrollable.Value, static n => n.Scrollable, static (n, v) => n.Scrollable = v);
        }

        if (SelectOnFocus.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(SelectOnFocus), SelectOnFocus.Value, static n => n.SelectOnFocus, static (n, v) => n.SelectOnFocus = v);
        }

        if (SingleTabStop.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(SingleTabStop), SingleTabStop.Value, static n => n.SingleTabStop, static (n, v) => n.SingleTabStop = v);
        }

        if (Size.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Size), Size.Value, static n => n.Size, static (n, v) => n.Size = v);
        }

        if (Styles is not null)
        {
            bitNavBar.TakeFromCascade(nameof(Styles), Styles, static n => n.Styles, static (n, v) => n.Styles = v);
        }

        if (Vertical.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(Vertical), Vertical.Value, static n => n.Vertical, static (n, v) => n.Vertical = v);
        }

        if (WrapNavigation.HasValue)
        {
            bitNavBar.TakeFromCascade(nameof(WrapNavigation), WrapNavigation.Value, static n => n.WrapNavigation, static (n, v) => n.WrapNavigation = v);
        }

        if (urlMatchingChanged)
        {
            bitNavBar.OnUrlMatchingChanged();
        }
    }
}
