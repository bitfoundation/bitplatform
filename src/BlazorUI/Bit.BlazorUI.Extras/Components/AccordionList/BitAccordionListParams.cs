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

        if (Background.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Background), Background.Value, static a => a.Background, static (a, v) => a.Background = v);
        }

        if (Border.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Border), Border.Value, static a => a.Border, static (a, v) => a.Border = v);
        }

        if (Classes is not null)
        {
            bitAccordionList.TakeFromCascade(nameof(Classes), Classes, static a => a.Classes, static (a, v) => a.Classes = v);
        }

        if (Collapsible.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Collapsible), Collapsible.Value, static a => a.Collapsible, static (a, v) => a.Collapsible = v);
        }

        if (EmptyContent is not null)
        {
            bitAccordionList.TakeFromCascade(nameof(EmptyContent), EmptyContent, static a => a.EmptyContent, static (a, v) => a.EmptyContent = v);
        }

        // The icon takes precedence over the icon name, so a cascaded icon is only a default for a list that has
        // set neither: applied over a list's own ExpandedExpanderIconName it would override it rather than default it.
        if (ExpandedExpanderIcon is not null)
        {
            if (bitAccordionList.HasNotBeenSet(nameof(ExpandedExpanderIconName)))
            {
                bitAccordionList.TakeFromCascade(nameof(ExpandedExpanderIcon), ExpandedExpanderIcon, static a => a.ExpandedExpanderIcon, static (a, v) => a.ExpandedExpanderIcon = v);
            }
            else
            {
                bitAccordionList.ReleaseFromCascade(nameof(ExpandedExpanderIcon));
            }
        }

        if (ExpandedExpanderIconName.HasValue())
        {
            bitAccordionList.TakeFromCascade(nameof(ExpandedExpanderIconName), ExpandedExpanderIconName, static a => a.ExpandedExpanderIconName, static (a, v) => a.ExpandedExpanderIconName = v);
        }

        // Likewise for the collapsed expander icon and the list's own ExpanderIconName.
        if (ExpanderIcon is not null)
        {
            if (bitAccordionList.HasNotBeenSet(nameof(ExpanderIconName)))
            {
                bitAccordionList.TakeFromCascade(nameof(ExpanderIcon), ExpanderIcon, static a => a.ExpanderIcon, static (a, v) => a.ExpanderIcon = v);
            }
            else
            {
                bitAccordionList.ReleaseFromCascade(nameof(ExpanderIcon));
            }
        }

        if (ExpanderIconName.HasValue())
        {
            bitAccordionList.TakeFromCascade(nameof(ExpanderIconName), ExpanderIconName, static a => a.ExpanderIconName, static (a, v) => a.ExpanderIconName = v);
        }

        if (ExpanderIconPlacement.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(ExpanderIconPlacement), ExpanderIconPlacement.Value, static a => a.ExpanderIconPlacement, static (a, v) => a.ExpanderIconPlacement = v);
        }

        if (ExpandOnPrint.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(ExpandOnPrint), ExpandOnPrint.Value, static a => a.ExpandOnPrint, static (a, v) => a.ExpandOnPrint = v);
        }

        if (Gap.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Gap), Gap.Value, static a => a.Gap, static (a, v) => a.Gap = v);
        }

        if (HeadingLevel.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(HeadingLevel), HeadingLevel.Value, static a => a.HeadingLevel, static (a, v) => a.HeadingLevel = v);
        }

        if (HiddenUntilFound.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(HiddenUntilFound), HiddenUntilFound.Value, static a => a.HiddenUntilFound, static (a, v) => a.HiddenUntilFound = v);
        }

        if (HideExpanderIcon.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(HideExpanderIcon), HideExpanderIcon.Value, static a => a.HideExpanderIcon, static (a, v) => a.HideExpanderIcon = v);
        }

        if (Joined.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Joined), Joined.Value, static a => a.Joined, static (a, v) => a.Joined = v);
        }

        if (LazyContent.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(LazyContent), LazyContent.Value, static a => a.LazyContent, static (a, v) => a.LazyContent = v);
        }

        if (MaxExpanded.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(MaxExpanded), MaxExpanded.Value, static a => a.MaxExpanded, static (a, v) => a.MaxExpanded = v);
        }

        if (MaxHeight.HasValue())
        {
            bitAccordionList.TakeFromCascade(nameof(MaxHeight), MaxHeight, static a => a.MaxHeight, static (a, v) => a.MaxHeight = v);
        }

        if (Multiple.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Multiple), Multiple.Value, static a => a.Multiple, static (a, v) => a.Multiple = v);
        }

        if (Navigable.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Navigable), Navigable.Value, static a => a.Navigable, static (a, v) => a.Navigable = v);
        }

        if (NoBorder.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(NoBorder), NoBorder.Value, static a => a.NoBorder, static (a, v) => a.NoBorder = v);
        }

        if (NoContentRegion.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(NoContentRegion), NoContentRegion.Value, static a => a.NoContentRegion, static (a, v) => a.NoContentRegion = v);
        }

        if (NoExpanderRotation.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(NoExpanderRotation), NoExpanderRotation.Value, static a => a.NoExpanderRotation, static (a, v) => a.NoExpanderRotation = v);
        }

        if (NoNavigationLoop.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(NoNavigationLoop), NoNavigationLoop.Value, static a => a.NoNavigationLoop, static (a, v) => a.NoNavigationLoop = v);
        }

        if (ReadOnly.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(ReadOnly), ReadOnly.Value, static a => a.ReadOnly, static (a, v) => a.ReadOnly = v);
        }

        if (ScrollIntoViewOnExpand.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(ScrollIntoViewOnExpand), ScrollIntoViewOnExpand.Value, static a => a.ScrollIntoViewOnExpand, static (a, v) => a.ScrollIntoViewOnExpand = v);
        }

        if (Size.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(Size), Size.Value, static a => a.Size, static (a, v) => a.Size = v);
        }

        if (Styles is not null)
        {
            bitAccordionList.TakeFromCascade(nameof(Styles), Styles, static a => a.Styles, static (a, v) => a.Styles = v);
        }

        if (TransitionDuration.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(TransitionDuration), TransitionDuration.Value, static a => a.TransitionDuration, static (a, v) => a.TransitionDuration = v);
        }

        if (UnmountOnCollapse.HasValue)
        {
            bitAccordionList.TakeFromCascade(nameof(UnmountOnCollapse), UnmountOnCollapse.Value, static a => a.UnmountOnCollapse, static (a, v) => a.UnmountOnCollapse = v);
        }
    }
}
