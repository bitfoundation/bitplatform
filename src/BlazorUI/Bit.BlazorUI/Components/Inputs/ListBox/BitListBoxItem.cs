namespace Bit.BlazorUI;

/// <summary>
/// An item of a <see cref="BitListBox{TItem, TValue}"/>.
/// </summary>
public class BitListBoxItem<TValue>
{
    /// <summary>
    /// The aria-label of the item, which replaces its text and description as what a screen reader names it by.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A second line of text under the text of the item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The icon shown before the text of the item, using custom CSS classes for external icon libraries. Takes
    /// precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The name of the icon shown before the text of the item, from the built-in Fluent UI icons. For external icon
    /// libraries, use <see cref="Icon"/> instead.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether the item is disabled: it cannot be selected, and the keyboard passes it by.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// A unique value to use as the key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// The custom CSS styles of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The text of the item, which typeahead matches too.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title (tooltip) of the item.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The value of the item: what selecting it writes to the Value (or adds to the Values) of the list box.
    /// </summary>
    public TValue? Value { get; set; }
}
