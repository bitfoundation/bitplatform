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
    /// The maximum number of items to display before coalescing.
    /// </summary>
    public uint? MaxDisplayedItems { get; set; }

    /// <summary>
    /// The maximum width of the text of each item as a CSS length (for example "8rem").
    /// </summary>
    public string? MaxItemWidth { get; set; }

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
    /// Reverses the positions of the icon and the item text of the item content.
    /// </summary>
    public bool? ReversedIcon { get; set; }

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

        if (AutoCollapse.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(AutoCollapse)))
        {
            bitBreadcrumb.AutoCollapse = AutoCollapse.Value;
        }

        if (AutoReorderOptions.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(AutoReorderOptions)))
        {
            bitBreadcrumb.AutoReorderOptions = AutoReorderOptions.Value;
        }

        if (Classes is not null && bitBreadcrumb.HasNotBeenSet(nameof(Classes)))
        {
            bitBreadcrumb.Classes = Classes;

            bitBreadcrumb.ClassBuilder.Reset();
        }

        if (Color.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(Color)))
        {
            bitBreadcrumb.Color = Color.Value;

            bitBreadcrumb.ClassBuilder.Reset();
        }

        if (DividerIcon is not null && bitBreadcrumb.HasNotBeenSet(nameof(DividerIcon)))
        {
            bitBreadcrumb.DividerIcon = DividerIcon;
        }

        if (DividerIconName.HasValue() && bitBreadcrumb.HasNotBeenSet(nameof(DividerIconName)))
        {
            bitBreadcrumb.DividerIconName = DividerIconName;
        }

        if (DividerIconTemplate is not null && bitBreadcrumb.HasNotBeenSet(nameof(DividerIconTemplate)))
        {
            bitBreadcrumb.DividerIconTemplate = DividerIconTemplate;
        }

        if (DividerText.HasValue() && bitBreadcrumb.HasNotBeenSet(nameof(DividerText)))
        {
            bitBreadcrumb.DividerText = DividerText;
        }

        if (ExpandOverflow.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(ExpandOverflow)))
        {
            bitBreadcrumb.ExpandOverflow = ExpandOverflow.Value;
        }

        if (MaxDisplayedItems.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(MaxDisplayedItems)))
        {
            bitBreadcrumb.MaxDisplayedItems = MaxDisplayedItems.Value;
        }

        if (MaxItemWidth.HasValue() && bitBreadcrumb.HasNotBeenSet(nameof(MaxItemWidth)))
        {
            bitBreadcrumb.MaxItemWidth = MaxItemWidth;

            bitBreadcrumb.StyleBuilder.Reset();
        }

        if (OverflowAriaLabel.HasValue() && bitBreadcrumb.HasNotBeenSet(nameof(OverflowAriaLabel)))
        {
            bitBreadcrumb.OverflowAriaLabel = OverflowAriaLabel;
        }

        if (OverflowIcon is not null && bitBreadcrumb.HasNotBeenSet(nameof(OverflowIcon)))
        {
            bitBreadcrumb.OverflowIcon = OverflowIcon;
        }

        if (OverflowIconName.HasValue() && bitBreadcrumb.HasNotBeenSet(nameof(OverflowIconName)))
        {
            bitBreadcrumb.OverflowIconName = OverflowIconName;
        }

        if (OverflowIconTemplate is not null && bitBreadcrumb.HasNotBeenSet(nameof(OverflowIconTemplate)))
        {
            bitBreadcrumb.OverflowIconTemplate = OverflowIconTemplate;
        }

        if (OverflowIndex.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(OverflowIndex)))
        {
            bitBreadcrumb.OverflowIndex = OverflowIndex.Value;
        }

        if (ReversedIcon.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(ReversedIcon)))
        {
            bitBreadcrumb.ReversedIcon = ReversedIcon.Value;
        }

        if (Scrollable.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(Scrollable)))
        {
            bitBreadcrumb.Scrollable = Scrollable.Value;

            bitBreadcrumb.ClassBuilder.Reset();
        }

        if (SelectedItemAsText.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(SelectedItemAsText)))
        {
            bitBreadcrumb.SelectedItemAsText = SelectedItemAsText.Value;
        }

        if (Size.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(Size)))
        {
            bitBreadcrumb.Size = Size.Value;

            bitBreadcrumb.ClassBuilder.Reset();
        }

        if (StructuredData.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(StructuredData)))
        {
            bitBreadcrumb.StructuredData = StructuredData.Value;
        }

        if (Styles is not null && bitBreadcrumb.HasNotBeenSet(nameof(Styles)))
        {
            bitBreadcrumb.Styles = Styles;

            bitBreadcrumb.StyleBuilder.Reset();
        }

        if (Wrap.HasValue && bitBreadcrumb.HasNotBeenSet(nameof(Wrap)))
        {
            bitBreadcrumb.Wrap = Wrap.Value;

            bitBreadcrumb.ClassBuilder.Reset();
        }
    }
}
