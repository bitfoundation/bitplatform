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
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set, and is not applied to a nav
    /// that sets its own ChevronDownIcon or ChevronDownIconName.
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
    /// The width in px of the chevron, which the items without children keep as padding in its place so every text lines up.
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
    /// The text the items opening a new tab are announced with. The default is "(opens in a new tab)"; an empty
    /// value takes the announcement off.
    /// </summary>
    public string? NewTabHint { get; set; }

    /// <summary>
    /// Keeps every item expanded and hides the collapse/expand buttons together with the space they reserve at the start of each item.
    /// </summary>
    public bool? NoCollapse { get; set; }

    /// <summary>
    /// Stops the items opening a new tab from announcing that they do.
    /// </summary>
    public bool? NoNewTabHint { get; set; }

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

        if (Accent.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Accent), Accent.Value, static n => n.Accent, static (n, v) => n.Accent = v);
        }

        if (AllExpanded.HasValue)
        {
            bitNav.TakeFromCascade(nameof(AllExpanded), AllExpanded.Value, static n => n.AllExpanded, static (n, v) => n.AllExpanded = v);
        }

        var ownChevronDownIcon = bitNav.HasSetAnyOf(nameof(ChevronDownIcon), nameof(ChevronDownIconName));

        if (ChevronDownIcon is not null)
        {
            bitNav.TakeFromCascade(nameof(ChevronDownIcon), ChevronDownIcon, static n => n.ChevronDownIcon, static (n, v) => n.ChevronDownIcon = v, outranked: ownChevronDownIcon);
        }

        if (ChevronDownIconName.HasValue())
        {
            bitNav.TakeFromCascade(nameof(ChevronDownIconName), ChevronDownIconName, static n => n.ChevronDownIconName, static (n, v) => n.ChevronDownIconName = v, outranked: ownChevronDownIcon);
        }

        if (Classes is not null)
        {
            bitNav.TakeFromCascade(nameof(Classes), Classes, static n => n.Classes, static (n, v) => n.Classes = v);
        }

        if (CollapseAriaLabel.HasValue())
        {
            bitNav.TakeFromCascade(nameof(CollapseAriaLabel), CollapseAriaLabel, static n => n.CollapseAriaLabel, static (n, v) => n.CollapseAriaLabel = v);
        }

        if (Color.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Color), Color.Value, static n => n.Color, static (n, v) => n.Color = v);
        }

        if (ExpandAriaLabel.HasValue())
        {
            bitNav.TakeFromCascade(nameof(ExpandAriaLabel), ExpandAriaLabel, static n => n.ExpandAriaLabel, static (n, v) => n.ExpandAriaLabel = v);
        }

        if (FitWidth.HasValue)
        {
            bitNav.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static n => n.FitWidth, static (n, v) => n.FitWidth = v);
        }

        if (FullWidth.HasValue)
        {
            bitNav.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static n => n.FullWidth, static (n, v) => n.FullWidth = v);
        }

        if (HeaderTemplateRenderMode.HasValue)
        {
            bitNav.TakeFromCascade(nameof(HeaderTemplateRenderMode), HeaderTemplateRenderMode.Value, static n => n.HeaderTemplateRenderMode, static (n, v) => n.HeaderTemplateRenderMode = v);
        }

        if (IconOnly.HasValue)
        {
            bitNav.TakeFromCascade(nameof(IconOnly), IconOnly.Value, static n => n.IconOnly, static (n, v) => n.IconOnly = v);
        }

        if (IndentPadding.HasValue)
        {
            bitNav.TakeFromCascade(nameof(IndentPadding), IndentPadding.Value, static n => n.IndentPadding, static (n, v) => n.IndentPadding = v);
        }

        if (IndentReversedPadding.HasValue)
        {
            bitNav.TakeFromCascade(nameof(IndentReversedPadding), IndentReversedPadding.Value, static n => n.IndentReversedPadding, static (n, v) => n.IndentReversedPadding = v);
        }

        if (IndentValue.HasValue)
        {
            bitNav.TakeFromCascade(nameof(IndentValue), IndentValue.Value, static n => n.IndentValue, static (n, v) => n.IndentValue = v);
        }

        if (ItemTemplateRenderMode.HasValue)
        {
            bitNav.TakeFromCascade(nameof(ItemTemplateRenderMode), ItemTemplateRenderMode.Value, static n => n.ItemTemplateRenderMode, static (n, v) => n.ItemTemplateRenderMode = v);
        }

        if (Match.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Match), Match.Value, static n => n.Match, static (n, v) => n.Match = v);
        }

        if (Mode.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Mode), Mode.Value, static n => n.Mode, static (n, v) => n.Mode = v);
        }

        // an empty hint is a value of its own - the one that takes the announcement off - so only null is
        // what leaves the component to its default.
        if (NewTabHint is not null)
        {
            bitNav.TakeFromCascade(nameof(NewTabHint), NewTabHint, static n => n.NewTabHint, static (n, v) => n.NewTabHint = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitNav.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static n => n.NoNewTabHint, static (n, v) => n.NoNewTabHint = v);
        }

        if (NoCollapse.HasValue)
        {
            bitNav.TakeFromCascade(nameof(NoCollapse), NoCollapse.Value, static n => n.NoCollapse, static (n, v) => n.NoCollapse = v);
        }

        if (RenderType.HasValue)
        {
            bitNav.TakeFromCascade(nameof(RenderType), RenderType.Value, static n => n.RenderType, static (n, v) => n.RenderType = v);
        }

        if (Reselectable.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Reselectable), Reselectable.Value, static n => n.Reselectable, static (n, v) => n.Reselectable = v);
        }

        if (ReversedChevron.HasValue)
        {
            bitNav.TakeFromCascade(nameof(ReversedChevron), ReversedChevron.Value, static n => n.ReversedChevron, static (n, v) => n.ReversedChevron = v);
        }

        if (SingleExpand.HasValue)
        {
            bitNav.TakeFromCascade(nameof(SingleExpand), SingleExpand.Value, static n => n.SingleExpand, static (n, v) => n.SingleExpand = v);
        }

        if (Size.HasValue)
        {
            bitNav.TakeFromCascade(nameof(Size), Size.Value, static n => n.Size, static (n, v) => n.Size = v);
        }

        if (Styles is not null)
        {
            bitNav.TakeFromCascade(nameof(Styles), Styles, static n => n.Styles, static (n, v) => n.Styles = v);
        }
    }
}
