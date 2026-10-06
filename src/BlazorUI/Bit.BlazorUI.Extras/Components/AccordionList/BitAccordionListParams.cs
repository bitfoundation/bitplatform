namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitAccordionList{TItem}"/> component.
/// </summary>
/// <remarks>
/// The items (Items, Options, ChildContent), the expanded keys, the templates typed over the item, the name
/// selectors and the event callbacks are left out on purpose: they belong to a single list rather than to a group
/// of them.
/// <br />
/// None of the parameters here depends on the type of the items, so the class is not generic: one instance
/// reaches every list under the <see cref="BitParams"/> it is given to, whichever of its three item APIs it uses.
/// </remarks>
public class BitAccordionListParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitAccordionList{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitAccordionList value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitAccordionList<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// The color kind of the background of all the accordion items.
    /// </summary>
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The color kind of the border of all the accordion items.
    /// </summary>
    public BitColorKind? Border { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the AccordionList.
    /// </summary>
    public BitAccordionListClassStyles? Classes { get; set; }

    /// <summary>
    /// Allows the expanded item to be collapsed again from its own header.
    /// </summary>
    public bool? Collapsible { get; set; }

    /// <summary>
    /// The custom content to render in place of the items when the list has none.
    /// </summary>
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>
    /// The icon to show in place of the expander icon of all items while they are expanded, using custom CSS classes
    /// for external icon libraries.
    /// </summary>
    public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// The name of the icon, from the built-in Fluent UI icons, to show in place of the expander icon of all items
    /// while they are expanded.
    /// </summary>
    public string? ExpandedExpanderIconName { get; set; }

    /// <summary>
    /// The icon to display as the expander of all items using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// The name of the icon to display as the expander of all items from the built-in Fluent UI icons.
    /// </summary>
    public string? ExpanderIconName { get; set; }

    /// <summary>
    /// The side of the header the expander icon of all the items sits on.
    /// </summary>
    public BitPlacement? ExpanderIconPlacement { get; set; }

    /// <summary>
    /// Opens the panel of every item while the page is being printed.
    /// </summary>
    public bool? ExpandOnPrint { get; set; }

    /// <summary>
    /// The space (gap) in pixels between the accordion items.
    /// </summary>
    public int? Gap { get; set; }

    /// <summary>
    /// The heading level (aria-level) reported for the header of every item.
    /// </summary>
    public int? HeadingLevel { get; set; }

    /// <summary>
    /// Hands the collapsed panel of every item to the browser as <c>hidden="until-found"</c>, so find-in-page reaches into it.
    /// </summary>
    public bool? HiddenUntilFound { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of all the items.
    /// </summary>
    public bool? HideExpanderIcon { get; set; }

    /// <summary>
    /// Joins the items into one surface with a single shared line between them.
    /// </summary>
    public bool? Joined { get; set; }

    /// <summary>
    /// Delays the first render of the content of each item until it is expanded for the first time.
    /// </summary>
    public bool? LazyContent { get; set; }

    /// <summary>
    /// The greatest number of items that can be expanded at the same time in multiple-expand mode.
    /// </summary>
    public int? MaxExpanded { get; set; }

    /// <summary>
    /// The maximum height of the content of every item (any CSS length), beyond which the content scrolls.
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Enables the multiple-expand mode in which more than one item can be expanded at the same time.
    /// </summary>
    public bool? Multiple { get; set; }

    /// <summary>
    /// Moves the focus between the headers of the items with the ArrowUp, ArrowDown, Home and End keys.
    /// </summary>
    public bool? Navigable { get; set; }

    /// <summary>
    /// Removes the default border of all the accordion items and gives a background color to their body.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Removes the <c>region</c> role from the panel of every item.
    /// </summary>
    public bool? NoContentRegion { get; set; }

    /// <summary>
    /// Keeps the expander icon of every item still instead of turning it over when the item is expanded.
    /// </summary>
    public bool? NoExpanderRotation { get; set; }

    /// <summary>
    /// Stops the keyboard navigation at the two ends of the list instead of wrapping it around.
    /// </summary>
    public bool? NoNavigationLoop { get; set; }

    /// <summary>
    /// Leaves every item where it is: the headers no longer answer the pointer or the keyboard.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// Brings the item that has just been expanded into view.
    /// </summary>
    public bool? ScrollIntoViewOnExpand { get; set; }

    /// <summary>
    /// The size of all the accordion items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the AccordionList.
    /// </summary>
    public BitAccordionListClassStyles? Styles { get; set; }

    /// <summary>
    /// The duration of the expand/collapse transition of every item in milliseconds.
    /// </summary>
    public int? TransitionDuration { get; set; }

    /// <summary>
    /// Removes the content of an item from the DOM while it is collapsed.
    /// </summary>
    public bool? UnmountOnCollapse { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitAccordionList{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitAccordionList{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitAccordionList"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitAccordionList"/>.
    /// </remarks>
    /// <param name="bitAccordionList">
    /// The <see cref="BitAccordionList{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitAccordionList<TItem> bitAccordionList) where TItem : class
    {
        if (bitAccordionList is null) return;

        UpdateBaseParameters(bitAccordionList);

        // This runs on every render of every list under the BitParams, so a value that drives the class or the style
        // of the root is only assigned - and the builder only reset - when it differs from the one the list already
        // holds: an unchanged one would rebuild both strings on every render for nothing.
        if (Background.HasValue && bitAccordionList.HasNotBeenSet(nameof(Background)))
        {
            bitAccordionList.Background = Background.Value;
        }

        if (Border.HasValue && bitAccordionList.HasNotBeenSet(nameof(Border)))
        {
            bitAccordionList.Border = Border.Value;
        }

        if (Classes is not null && bitAccordionList.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitAccordionList.Classes, Classes) is false)
        {
            bitAccordionList.Classes = Classes;

            bitAccordionList.ClassBuilder.Reset();
        }

        if (Collapsible.HasValue && bitAccordionList.HasNotBeenSet(nameof(Collapsible)))
        {
            bitAccordionList.Collapsible = Collapsible.Value;
        }

        if (EmptyContent is not null && bitAccordionList.HasNotBeenSet(nameof(EmptyContent)))
        {
            bitAccordionList.EmptyContent = EmptyContent;
        }

        // The icon takes precedence over the icon name, so a cascaded icon is only a default for a list that has
        // set neither: applied over a list's own ExpandedExpanderIconName it would override it rather than default it.
        if (ExpandedExpanderIcon is not null &&
            bitAccordionList.HasNotBeenSet(nameof(ExpandedExpanderIcon)) &&
            bitAccordionList.HasNotBeenSet(nameof(ExpandedExpanderIconName)))
        {
            bitAccordionList.ExpandedExpanderIcon = ExpandedExpanderIcon;
        }

        if (ExpandedExpanderIconName.HasValue() && bitAccordionList.HasNotBeenSet(nameof(ExpandedExpanderIconName)))
        {
            bitAccordionList.ExpandedExpanderIconName = ExpandedExpanderIconName;
        }

        // Likewise for the collapsed expander icon and the list's own ExpanderIconName.
        if (ExpanderIcon is not null &&
            bitAccordionList.HasNotBeenSet(nameof(ExpanderIcon)) &&
            bitAccordionList.HasNotBeenSet(nameof(ExpanderIconName)))
        {
            bitAccordionList.ExpanderIcon = ExpanderIcon;
        }

        if (ExpanderIconName.HasValue() && bitAccordionList.HasNotBeenSet(nameof(ExpanderIconName)))
        {
            bitAccordionList.ExpanderIconName = ExpanderIconName;
        }

        if (ExpanderIconPlacement.HasValue && bitAccordionList.HasNotBeenSet(nameof(ExpanderIconPlacement)))
        {
            bitAccordionList.ExpanderIconPlacement = ExpanderIconPlacement.Value;
        }

        if (ExpandOnPrint.HasValue && bitAccordionList.HasNotBeenSet(nameof(ExpandOnPrint)))
        {
            bitAccordionList.ExpandOnPrint = ExpandOnPrint.Value;
        }

        if (Gap.HasValue && bitAccordionList.HasNotBeenSet(nameof(Gap)) && bitAccordionList.Gap != Gap)
        {
            bitAccordionList.Gap = Gap.Value;

            bitAccordionList.StyleBuilder.Reset();
        }

        if (HeadingLevel.HasValue && bitAccordionList.HasNotBeenSet(nameof(HeadingLevel)))
        {
            bitAccordionList.HeadingLevel = HeadingLevel.Value;
        }

        if (HiddenUntilFound.HasValue && bitAccordionList.HasNotBeenSet(nameof(HiddenUntilFound)))
        {
            bitAccordionList.HiddenUntilFound = HiddenUntilFound.Value;
        }

        if (HideExpanderIcon.HasValue && bitAccordionList.HasNotBeenSet(nameof(HideExpanderIcon)))
        {
            bitAccordionList.HideExpanderIcon = HideExpanderIcon.Value;
        }

        if (Joined.HasValue && bitAccordionList.HasNotBeenSet(nameof(Joined)) && bitAccordionList.Joined != Joined.Value)
        {
            bitAccordionList.Joined = Joined.Value;

            bitAccordionList.ClassBuilder.Reset();
            bitAccordionList.StyleBuilder.Reset();
        }

        if (LazyContent.HasValue && bitAccordionList.HasNotBeenSet(nameof(LazyContent)))
        {
            bitAccordionList.LazyContent = LazyContent.Value;
        }

        if (MaxExpanded.HasValue && bitAccordionList.HasNotBeenSet(nameof(MaxExpanded)))
        {
            bitAccordionList.MaxExpanded = MaxExpanded.Value;
        }

        if (MaxHeight.HasValue() && bitAccordionList.HasNotBeenSet(nameof(MaxHeight)))
        {
            bitAccordionList.MaxHeight = MaxHeight;
        }

        if (Multiple.HasValue && bitAccordionList.HasNotBeenSet(nameof(Multiple)) && bitAccordionList.Multiple != Multiple.Value)
        {
            bitAccordionList.Multiple = Multiple.Value;

            bitAccordionList.ClassBuilder.Reset();
        }

        if (Navigable.HasValue && bitAccordionList.HasNotBeenSet(nameof(Navigable)))
        {
            bitAccordionList.Navigable = Navigable.Value;
        }

        if (NoBorder.HasValue && bitAccordionList.HasNotBeenSet(nameof(NoBorder)))
        {
            bitAccordionList.NoBorder = NoBorder.Value;
        }

        if (NoContentRegion.HasValue && bitAccordionList.HasNotBeenSet(nameof(NoContentRegion)))
        {
            bitAccordionList.NoContentRegion = NoContentRegion.Value;
        }

        if (NoExpanderRotation.HasValue && bitAccordionList.HasNotBeenSet(nameof(NoExpanderRotation)))
        {
            bitAccordionList.NoExpanderRotation = NoExpanderRotation.Value;
        }

        if (NoNavigationLoop.HasValue && bitAccordionList.HasNotBeenSet(nameof(NoNavigationLoop)))
        {
            bitAccordionList.NoNavigationLoop = NoNavigationLoop.Value;
        }

        if (ReadOnly.HasValue && bitAccordionList.HasNotBeenSet(nameof(ReadOnly)))
        {
            bitAccordionList.ReadOnly = ReadOnly.Value;
        }

        if (ScrollIntoViewOnExpand.HasValue && bitAccordionList.HasNotBeenSet(nameof(ScrollIntoViewOnExpand)))
        {
            bitAccordionList.ScrollIntoViewOnExpand = ScrollIntoViewOnExpand.Value;
        }

        if (Size.HasValue && bitAccordionList.HasNotBeenSet(nameof(Size)))
        {
            bitAccordionList.Size = Size.Value;
        }

        if (Styles is not null && bitAccordionList.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitAccordionList.Styles, Styles) is false)
        {
            bitAccordionList.Styles = Styles;

            bitAccordionList.StyleBuilder.Reset();
        }

        if (TransitionDuration.HasValue && bitAccordionList.HasNotBeenSet(nameof(TransitionDuration)))
        {
            bitAccordionList.TransitionDuration = TransitionDuration.Value;
        }

        if (UnmountOnCollapse.HasValue && bitAccordionList.HasNotBeenSet(nameof(UnmountOnCollapse)))
        {
            bitAccordionList.UnmountOnCollapse = UnmountOnCollapse.Value;
        }
    }
}
