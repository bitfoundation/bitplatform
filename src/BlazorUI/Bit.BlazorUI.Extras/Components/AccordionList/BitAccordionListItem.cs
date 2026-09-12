namespace Bit.BlazorUI;

/// <summary>
/// Represents a single item (panel) of the <see cref="BitAccordionList{TItem}"/> component.
/// </summary>
public class BitAccordionListItem
{
    /// <summary>
    /// The content rendered beside the header of the item, outside of the toggle button and of the heading
    /// it sits in, so that it can hold its own interactive elements (a menu, a delete button, a switch).
    /// The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? Actions { get; set; }

    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A short description rendered in the header of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon to show in place of the expander icon while the item is expanded, using custom
    /// CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpandedExpanderIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ExpandedExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon, from the built-in Fluent UI icons, to show in place of the expander
    /// icon while the item is expanded.
    /// </summary>
    public string? ExpandedExpanderIconName { get; set; }

    /// <summary>
    /// Gets or sets the icon to display as the expander using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="ExpanderIconName"/> when both are set.
    /// </summary>
    public BitIconInfo? ExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display as the expander from the built-in Fluent UI icons.
    /// </summary>
    public string? ExpanderIconName { get; set; }

    /// <summary>
    /// The custom content to render in place of the expander icon of the item, leaving the rest of the header
    /// as it is. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? ExpanderTemplate { get; set; }

    /// <summary>
    /// The content (body) of the item that is shown when the item is expanded. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? Body { get; set; }

    /// <summary>
    /// The accessible label of the toggle button in the header of the item, for a header whose own content does
    /// not name it - an icon-only header template, most of all.
    /// </summary>
    public string? HeaderAriaLabel { get; set; }

    /// <summary>
    /// The custom template for the header of the item. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? HeaderTemplate { get; set; }

    /// <summary>
    /// Removes the expander icon from the header of the item, overriding the value of the AccordionList.
    /// </summary>
    public bool? HideExpanderIcon { get; set; }

    /// <summary>
    /// Gets or sets the icon to display at the start of the header of the item using custom CSS classes for
    /// external icon libraries. Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display at the start of the header of the item from the built-in
    /// Fluent UI icons.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Determines whether the item is expanded. This value is also assigned by the component during interactions.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// A unique value to use as the key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The click event handler of the header of the item.
    /// </summary>
    public Action<BitAccordionListItem>? OnClick { get; set; }

    /// <summary>
    /// Leaves the item where it is: its header keeps its colors and its place in the tab order, but it no
    /// longer answers the pointer or the keyboard. Overrides the value of the AccordionList.
    /// </summary>
    public bool? ReadOnly { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The title (header text) of the item.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The custom content to render in place of the <see cref="Title"/> of the item, leaving the rest of the
    /// header as it is. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? TitleTemplate { get; set; }
}
