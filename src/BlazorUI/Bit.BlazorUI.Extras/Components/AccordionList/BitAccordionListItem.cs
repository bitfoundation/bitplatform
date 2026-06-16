namespace Bit.BlazorUI;

/// <summary>
/// Represents a single item (panel) of the <see cref="BitAccordionList{TItem}"/> component.
/// </summary>
public class BitAccordionListItem
{
    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A short description rendered in the header of the item.
    /// </summary>
    public string? Description { get; set; }

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
    /// The content (body) of the item that is shown when the item is expanded. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? Body { get; set; }

    /// <summary>
    /// The custom template for the header of the item. The context value provides the item itself.
    /// </summary>
    public RenderFragment<BitAccordionListItem>? HeaderTemplate { get; set; }

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
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The title (header text) of the item.
    /// </summary>
    public string? Title { get; set; }
}
