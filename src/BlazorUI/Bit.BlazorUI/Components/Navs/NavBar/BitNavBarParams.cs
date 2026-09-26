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
    /// Enables recalling the select events when the same item is selected.
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

        if (Alignment.HasValue && bitNavBar.HasNotBeenSet(nameof(Alignment)))
        {
            bitNavBar.Alignment = Alignment.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (AutoReorderOptions.HasValue && bitNavBar.HasNotBeenSet(nameof(AutoReorderOptions)))
        {
            bitNavBar.AutoReorderOptions = AutoReorderOptions.Value;
        }

        if (Classes is not null && bitNavBar.HasNotBeenSet(nameof(Classes)))
        {
            bitNavBar.Classes = Classes;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitNavBar.HasNotBeenSet(nameof(Color)))
        {
            bitNavBar.Color = Color.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Filled.HasValue && bitNavBar.HasNotBeenSet(nameof(Filled)))
        {
            bitNavBar.Filled = Filled.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (FitWidth.HasValue && bitNavBar.HasNotBeenSet(nameof(FitWidth)))
        {
            bitNavBar.FitWidth = FitWidth.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitNavBar.HasNotBeenSet(nameof(FullWidth)))
        {
            bitNavBar.FullWidth = FullWidth.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (HideUnselectedText.HasValue && bitNavBar.HasNotBeenSet(nameof(HideUnselectedText)))
        {
            bitNavBar.HideUnselectedText = HideUnselectedText.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (IconOnly.HasValue && bitNavBar.HasNotBeenSet(nameof(IconOnly)))
        {
            bitNavBar.IconOnly = IconOnly.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Indicator.HasValue && bitNavBar.HasNotBeenSet(nameof(Indicator)))
        {
            bitNavBar.Indicator = Indicator.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (InlineText.HasValue && bitNavBar.HasNotBeenSet(nameof(InlineText)))
        {
            bitNavBar.InlineText = InlineText.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (ItemTemplateRenderMode.HasValue && bitNavBar.HasNotBeenSet(nameof(ItemTemplateRenderMode)))
        {
            bitNavBar.ItemTemplateRenderMode = ItemTemplateRenderMode.Value;
        }

        if (Justified.HasValue && bitNavBar.HasNotBeenSet(nameof(Justified)))
        {
            bitNavBar.Justified = Justified.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Match.HasValue && bitNavBar.HasNotBeenSet(nameof(Match)))
        {
            urlMatchingChanged |= bitNavBar.Match != Match.Value;

            bitNavBar.Match = Match.Value;
        }

        if (Mode.HasValue && bitNavBar.HasNotBeenSet(nameof(Mode)))
        {
            urlMatchingChanged |= bitNavBar.Mode != Mode.Value;

            bitNavBar.Mode = Mode.Value;
        }

        if (Reselectable.HasValue && bitNavBar.HasNotBeenSet(nameof(Reselectable)))
        {
            bitNavBar.Reselectable = Reselectable.Value;
        }

        if (SafeArea.HasValue && bitNavBar.HasNotBeenSet(nameof(SafeArea)))
        {
            bitNavBar.SafeArea = SafeArea.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Scrollable.HasValue && bitNavBar.HasNotBeenSet(nameof(Scrollable)))
        {
            bitNavBar.Scrollable = Scrollable.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (SelectOnFocus.HasValue && bitNavBar.HasNotBeenSet(nameof(SelectOnFocus)))
        {
            bitNavBar.SelectOnFocus = SelectOnFocus.Value;
        }

        if (SingleTabStop.HasValue && bitNavBar.HasNotBeenSet(nameof(SingleTabStop)))
        {
            bitNavBar.SingleTabStop = SingleTabStop.Value;
        }

        if (Size.HasValue && bitNavBar.HasNotBeenSet(nameof(Size)))
        {
            bitNavBar.Size = Size.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (Styles is not null && bitNavBar.HasNotBeenSet(nameof(Styles)))
        {
            bitNavBar.Styles = Styles;

            bitNavBar.StyleBuilder.Reset();
        }

        if (Vertical.HasValue && bitNavBar.HasNotBeenSet(nameof(Vertical)))
        {
            bitNavBar.Vertical = Vertical.Value;

            bitNavBar.ClassBuilder.Reset();
        }

        if (WrapNavigation.HasValue && bitNavBar.HasNotBeenSet(nameof(WrapNavigation)))
        {
            bitNavBar.WrapNavigation = WrapNavigation.Value;
        }

        if (urlMatchingChanged)
        {
            bitNavBar.OnUrlMatchingChanged();
        }
    }
}
