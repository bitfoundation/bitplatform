namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitNav{TItem}"/> component.
/// </summary>
/// <remarks>
/// The nav is generic over its item type, but the parameters that are worth sharing between the navs of a
/// page are not: the ones typed over TItem - the items themselves, the selection, the templates rendering an
/// item, the name selectors and the event callbacks - stay on the instance, which is what keeps this object
/// usable from a single non-generic <see cref="BitParams"/> list no matter which of the three item APIs each
/// nav under it uses.
/// </remarks>
public class BitNavParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitNav{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitNav value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitNav<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// The accent color of the nav: the background of the hovered and the selected item.
    /// </summary>
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Expands all items when they are first rendered.
    /// </summary>
    public bool? AllExpanded { get; set; }

    /// <summary>
    /// The icon for the chevron-down element of each nav item, from an external icon library.
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of each nav item.
    /// </summary>
    public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav.
    /// </summary>
    public BitNavClassStyles? Classes { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of an expanded item.
    /// </summary>
    public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The general color of the nav that is only used for colored parts like icons.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of a collapsed item.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Renders the nav in a width to only fit its content.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// Renders the nav in full width of its container element.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The render mode of the custom HeaderTemplate.
    /// </summary>
    public BitNavItemTemplateRenderMode? HeaderTemplateRenderMode { get; set; }

    /// <summary>
    /// Only renders the icon of each nav item.
    /// </summary>
    public bool? IconOnly { get; set; }

    /// <summary>
    /// The indentation padding in px for items without children (compensation space for chevron icon).
    /// </summary>
    public int? IndentPadding { get; set; }

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    public int? IndentReversedPadding { get; set; }

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    public int? IndentValue { get; set; }

    /// <summary>
    /// The render mode of the custom ItemTemplate.
    /// </summary>
    public BitNavItemTemplateRenderMode? ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// The global URL matching behavior of the nav.
    /// </summary>
    public BitNavMatch? Match { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    public BitNavMode? Mode { get; set; }

    /// <summary>
    /// Hides all collapse/expand buttons and remove their spaces at the start of each node.
    /// </summary>
    public bool? NoCollapse { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    public BitNavRenderType? RenderType { get; set; }

    /// <summary>
    /// Enables recalling the select events when the same item is selected.
    /// </summary>
    public bool? Reselectable { get; set; }

    /// <summary>
    /// Reverses the location of the expander chevron.
    /// </summary>
    public bool? ReversedChevron { get; set; }

    /// <summary>
    /// Enables the single-expand mode in the nav.
    /// </summary>
    public bool? SingleExpand { get; set; }

    /// <summary>
    /// The size of the nav items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav.
    /// </summary>
    public BitNavClassStyles? Styles { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitNav{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitNav{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitNav"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitNav"/>.
    /// </remarks>
    /// <param name="bitNav">
    /// The <see cref="BitNav{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitNav<TItem> bitNav) where TItem : class
    {
        if (bitNav is null) return;

        UpdateBaseParameters(bitNav);

        if (Accent.HasValue && bitNav.HasNotBeenSet(nameof(Accent)))
        {
            bitNav.Accent = Accent.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (AllExpanded.HasValue && bitNav.HasNotBeenSet(nameof(AllExpanded)))
        {
            bitNav.AllExpanded = AllExpanded.Value;
        }

        if (ChevronDownIcon is not null && bitNav.HasNotBeenSet(nameof(ChevronDownIcon)))
        {
            bitNav.ChevronDownIcon = ChevronDownIcon;
        }

        if (ChevronDownIconName.HasValue() && bitNav.HasNotBeenSet(nameof(ChevronDownIconName)))
        {
            bitNav.ChevronDownIconName = ChevronDownIconName;
        }

        if (Classes is not null && bitNav.HasNotBeenSet(nameof(Classes)))
        {
            bitNav.Classes = Classes;

            bitNav.ClassBuilder.Reset();
        }

        if (CollapseAriaLabel.HasValue() && bitNav.HasNotBeenSet(nameof(CollapseAriaLabel)))
        {
            bitNav.CollapseAriaLabel = CollapseAriaLabel;
        }

        if (Color.HasValue && bitNav.HasNotBeenSet(nameof(Color)))
        {
            bitNav.Color = Color.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (ExpandAriaLabel.HasValue() && bitNav.HasNotBeenSet(nameof(ExpandAriaLabel)))
        {
            bitNav.ExpandAriaLabel = ExpandAriaLabel;
        }

        if (FitWidth.HasValue && bitNav.HasNotBeenSet(nameof(FitWidth)))
        {
            bitNav.FitWidth = FitWidth.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (FullWidth.HasValue && bitNav.HasNotBeenSet(nameof(FullWidth)))
        {
            bitNav.FullWidth = FullWidth.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (HeaderTemplateRenderMode.HasValue && bitNav.HasNotBeenSet(nameof(HeaderTemplateRenderMode)))
        {
            bitNav.HeaderTemplateRenderMode = HeaderTemplateRenderMode.Value;
        }

        if (IconOnly.HasValue && bitNav.HasNotBeenSet(nameof(IconOnly)))
        {
            bitNav.IconOnly = IconOnly.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (IndentPadding.HasValue && bitNav.HasNotBeenSet(nameof(IndentPadding)))
        {
            bitNav.IndentPadding = IndentPadding.Value;
        }

        if (IndentReversedPadding.HasValue && bitNav.HasNotBeenSet(nameof(IndentReversedPadding)))
        {
            bitNav.IndentReversedPadding = IndentReversedPadding.Value;
        }

        if (IndentValue.HasValue && bitNav.HasNotBeenSet(nameof(IndentValue)))
        {
            bitNav.IndentValue = IndentValue.Value;
        }

        if (ItemTemplateRenderMode.HasValue && bitNav.HasNotBeenSet(nameof(ItemTemplateRenderMode)))
        {
            bitNav.ItemTemplateRenderMode = ItemTemplateRenderMode.Value;
        }

        // The URL match of the automatic mode depends on both of these, and it is only re-run when one of them
        // actually changes: the cascade is applied on every parameter set, and re-running the match each time
        // would re-select the current item over and over - and re-raise OnSelectItem for a Reselectable nav.
        var urlMatchingChanged = false;

        if (Match.HasValue && bitNav.HasNotBeenSet(nameof(Match)) && bitNav.Match != Match.Value)
        {
            bitNav.Match = Match.Value;

            urlMatchingChanged = true;
        }

        if (Mode.HasValue && bitNav.HasNotBeenSet(nameof(Mode)) && bitNav.Mode != Mode.Value)
        {
            bitNav.Mode = Mode.Value;

            urlMatchingChanged = true;
        }

        if (urlMatchingChanged)
        {
            bitNav.OnUrlMatchingChanged();
        }

        if (NoCollapse.HasValue && bitNav.HasNotBeenSet(nameof(NoCollapse)))
        {
            bitNav.NoCollapse = NoCollapse.Value;
        }

        if (RenderType.HasValue && bitNav.HasNotBeenSet(nameof(RenderType)))
        {
            bitNav.RenderType = RenderType.Value;
        }

        if (Reselectable.HasValue && bitNav.HasNotBeenSet(nameof(Reselectable)))
        {
            bitNav.Reselectable = Reselectable.Value;
        }

        if (ReversedChevron.HasValue && bitNav.HasNotBeenSet(nameof(ReversedChevron)))
        {
            bitNav.ReversedChevron = ReversedChevron.Value;
        }

        if (SingleExpand.HasValue && bitNav.HasNotBeenSet(nameof(SingleExpand)))
        {
            bitNav.SingleExpand = SingleExpand.Value;
        }

        if (Size.HasValue && bitNav.HasNotBeenSet(nameof(Size)))
        {
            bitNav.Size = Size.Value;

            bitNav.ClassBuilder.Reset();
        }

        if (Styles is not null && bitNav.HasNotBeenSet(nameof(Styles)))
        {
            bitNav.Styles = Styles;

            bitNav.StyleBuilder.Reset();
        }
    }
}
