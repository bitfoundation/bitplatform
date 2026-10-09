namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitBreadcrumb{TItem}"/> component.
/// </summary>
/// <remarks>
/// It carries the parameters that say nothing about the item type, so one object fits every breadcrumb under it
/// whatever each of them holds. What is left on the breadcrumb itself is what depends on the item type (Items,
/// ItemTemplate, OverflowTemplate, NameSelectors and OnItemClick) and the options it renders (ChildContent/Options).
/// </remarks>
public class BitBreadcrumbParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitBreadcrumb{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitBreadcrumb value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// <br />
    /// The name is the bare name of the component, so a single cascade reaches every breadcrumb whatever it is generic over.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitBreadcrumb<BitBreadcrumbItem>)}";



    public string Name => ParamName;



    /// <summary>
    /// Collapses the items that do not fit the width of the breadcrumb into the overflow menu, and brings
    /// them back as the room for them returns, so the trail always stays on a single line.
    /// </summary>
    public bool? AutoCollapse { get; set; }

    /// <summary>
    /// Keeps the rendered order of the items in sync with the markup order of the options even when existing
    /// options are only reordered. It only affects the options API.
    /// </summary>
    public bool? AutoReorderOptions { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the breadcrumb.
    /// </summary>
    public BitBreadcrumbClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the breadcrumb items.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// Render a custom divider icon in place of the default chevron.
    /// </summary>
    public BitIconInfo? DividerIcon { get; set; }

    /// <summary>
    /// Name of the icon to render as the divider in place of the default chevron.
    /// </summary>
    public string? DividerIconName { get; set; }

    /// <summary>
    /// The custom template content to render divider icon.
    /// </summary>
    public RenderFragment? DividerIconTemplate { get; set; }

    /// <summary>
    /// A plain text divider (for example "/" or "›") to render in place of the default chevron icon.
    /// </summary>
    public string? DividerText { get; set; }

    /// <summary>
    /// Makes the overflow button put the collapsed items back into the trail instead of opening them in a menu.
    /// </summary>
    public bool? ExpandOverflow { get; set; }

    /// <summary>
    /// Where the icon of each item is rendered relative to its text.
    /// </summary>
    public BitPlacement? IconPlacement { get; set; }

    /// <summary>
    /// The maximum number of items to display before coalescing.
    /// </summary>
    public uint? MaxDisplayedItems { get; set; }

    /// <summary>
    /// The maximum width of the text of each item as a CSS length (for example "8rem").
    /// </summary>
    public string? MaxItemWidth { get; set; }

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
    /// Aria label for the overflow button.
    /// </summary>
    public string? OverflowAriaLabel { get; set; }

    /// <summary>
    /// Render a custom overflow icon in place of the default icon.
    /// </summary>
    public BitIconInfo? OverflowIcon { get; set; }

    /// <summary>
    /// Name of the icon to render in the overflow button in place of the default icon.
    /// </summary>
    public string? OverflowIconName { get; set; }

    /// <summary>
    /// The custom template content to render the overflow icon.
    /// </summary>
    public RenderFragment? OverflowIconTemplate { get; set; }

    /// <summary>
    /// Optional index where overflow items will be collapsed.
    /// </summary>
    public uint? OverflowIndex { get; set; }

    /// <summary>
    /// Lets a long breadcrumb trail scroll sideways inside its container instead of overflowing it.
    /// </summary>
    public bool? Scrollable { get; set; }

    /// <summary>
    /// Renders the selected item as plain text instead of as a link or a button.
    /// </summary>
    public bool? SelectedItemAsText { get; set; }

    /// <summary>
    /// The size of the items of the breadcrumb.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Renders the trail as a schema.org BreadcrumbList in a JSON-LD script next to it.
    /// </summary>
    public bool? StructuredData { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the breadcrumb.
    /// </summary>
    public BitBreadcrumbClassStyles? Styles { get; set; }

    /// <summary>
    /// Lets a long breadcrumb trail wrap into multiple lines instead of overflowing its container in a single line.
    /// </summary>
    public bool? Wrap { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitBreadcrumb{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitBreadcrumb{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitBreadcrumb"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitBreadcrumb"/>.
    /// </remarks>
    /// <param name="bitBreadcrumb">
    /// The <see cref="BitBreadcrumb{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitBreadcrumb<TItem> bitBreadcrumb) where TItem : class
    {
        if (bitBreadcrumb is null) return;

        UpdateBaseParameters(bitBreadcrumb);

        if (AutoCollapse.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(AutoCollapse), AutoCollapse.Value, static b => b.AutoCollapse, static (b, v) => b.AutoCollapse = v);
        }

        if (AutoReorderOptions.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(AutoReorderOptions), AutoReorderOptions.Value, static b => b.AutoReorderOptions, static (b, v) => b.AutoReorderOptions = v);
        }

        if (Classes is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Classes), Classes, static b => b.Classes, static (b, v) => b.Classes = v);
        }

        if (Color.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Color), Color.Value, static b => b.Color, static (b, v) => b.Color = v);
        }

        var ownDividerIcon = bitBreadcrumb.HasSetAnyOf(nameof(DividerIcon), nameof(DividerIconName));

        if (DividerIcon is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(DividerIcon), DividerIcon, static b => b.DividerIcon, static (b, v) => b.DividerIcon = v, outranked: ownDividerIcon);
        }

        if (DividerIconName.HasValue())
        {
            bitBreadcrumb.TakeFromCascade(nameof(DividerIconName), DividerIconName, static b => b.DividerIconName, static (b, v) => b.DividerIconName = v, outranked: ownDividerIcon);
        }

        if (DividerIconTemplate is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(DividerIconTemplate), DividerIconTemplate, static b => b.DividerIconTemplate, static (b, v) => b.DividerIconTemplate = v);
        }

        if (DividerText.HasValue())
        {
            bitBreadcrumb.TakeFromCascade(nameof(DividerText), DividerText, static b => b.DividerText, static (b, v) => b.DividerText = v);
        }

        if (ExpandOverflow.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(ExpandOverflow), ExpandOverflow.Value, static b => b.ExpandOverflow, static (b, v) => b.ExpandOverflow = v);
        }

        if (IconPlacement.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(IconPlacement), IconPlacement.Value, static b => b.IconPlacement, static (b, v) => b.IconPlacement = v);
        }

        if (MaxDisplayedItems.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(MaxDisplayedItems), MaxDisplayedItems.Value, static b => b.MaxDisplayedItems, static (b, v) => b.MaxDisplayedItems = v);
        }

        if (MaxItemWidth.HasValue())
        {
            bitBreadcrumb.TakeFromCascade(nameof(MaxItemWidth), MaxItemWidth, static b => b.MaxItemWidth, static (b, v) => b.MaxItemWidth = v);
        }

        // an empty hint is a value of its own - the one that takes the announcement off - so only null is
        // what leaves the component to its default.
        if (NewTabHint is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(NewTabHint), NewTabHint, static b => b.NewTabHint, static (b, v) => b.NewTabHint = v);
        }

        if (NoNewTabHint.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(NoNewTabHint), NoNewTabHint.Value, static b => b.NoNewTabHint, static (b, v) => b.NoNewTabHint = v);
        }

        if (OverflowAriaLabel.HasValue())
        {
            bitBreadcrumb.TakeFromCascade(nameof(OverflowAriaLabel), OverflowAriaLabel, static b => b.OverflowAriaLabel, static (b, v) => b.OverflowAriaLabel = v);
        }

        var ownOverflowIcon = bitBreadcrumb.HasSetAnyOf(nameof(OverflowIcon), nameof(OverflowIconName));

        if (OverflowIcon is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(OverflowIcon), OverflowIcon, static b => b.OverflowIcon, static (b, v) => b.OverflowIcon = v, outranked: ownOverflowIcon);
        }

        if (OverflowIconName.HasValue())
        {
            bitBreadcrumb.TakeFromCascade(nameof(OverflowIconName), OverflowIconName, static b => b.OverflowIconName, static (b, v) => b.OverflowIconName = v, outranked: ownOverflowIcon);
        }

        if (OverflowIconTemplate is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(OverflowIconTemplate), OverflowIconTemplate, static b => b.OverflowIconTemplate, static (b, v) => b.OverflowIconTemplate = v);
        }

        if (OverflowIndex.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(OverflowIndex), OverflowIndex.Value, static b => b.OverflowIndex, static (b, v) => b.OverflowIndex = v);
        }

        if (Scrollable.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Scrollable), Scrollable.Value, static b => b.Scrollable, static (b, v) => b.Scrollable = v);
        }

        if (SelectedItemAsText.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(SelectedItemAsText), SelectedItemAsText.Value, static b => b.SelectedItemAsText, static (b, v) => b.SelectedItemAsText = v);
        }

        if (Size.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Size), Size.Value, static b => b.Size, static (b, v) => b.Size = v);
        }

        if (StructuredData.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(StructuredData), StructuredData.Value, static b => b.StructuredData, static (b, v) => b.StructuredData = v);
        }

        if (Styles is not null)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Styles), Styles, static b => b.Styles, static (b, v) => b.Styles = v);
        }

        if (Wrap.HasValue)
        {
            bitBreadcrumb.TakeFromCascade(nameof(Wrap), Wrap.Value, static b => b.Wrap, static (b, v) => b.Wrap = v);
        }
    }
}
