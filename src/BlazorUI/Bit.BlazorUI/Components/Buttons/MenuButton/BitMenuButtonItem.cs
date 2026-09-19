namespace Bit.BlazorUI;

public class BitMenuButtonItem
{
    /// <summary>
    /// The accessible name of the item, for the benefit of screen readers.
    /// Set it on an item whose visible label is an icon alone, or is too terse to stand on its own.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Turns the item into a check item: it is announced as a checkbox inside the menu, carries its
    /// <see cref="IsChecked"/> state as a check mark, and flips that state when it is clicked.
    /// </summary>
    /// <remarks>
    /// A menu of check items is usually one the user works inside of, so pair it with
    /// <c>CloseOnItemClick="false"</c> to keep the callout open between the toggles.
    /// </remarks>
    public bool Checkable { get; set; }

    /// <summary>
    /// The custom CSS classes of the item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The value of the href attribute of the item. If provided, the item renders as an anchor tag instead of button.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// Bootstrap: Icon="BitIconInfo.Bi("gear-fill")"
    /// FontAwesome: Icon = BitIconInfo.Fa("solid house")
    /// Custom CSS: Icon = BitIconInfo.Css("my-icon-class")
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// The checked state of a <see cref="Checkable"/> item. It is written back by the menu button as the item
    /// is clicked, so a plain field on the item is enough to keep the state.
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Whether or not the item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// If true, the item renders as the label of the group of items that follow it, instead of as a clickable
    /// item. It is presentational: the keyboard navigation steps over it.
    /// </summary>
    public bool IsHeader { get; set; }

    /// <summary>
    /// Determines the selection state of the item.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// If true, the item renders as a separator line instead of a clickable item.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// A unique value to use as a key of the item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the item.
    /// </summary>
    public Action<BitMenuButtonItem>? OnClick { get; set; }

    /// <summary>
    /// The trailing text of the item, shown at its far end and read after its label - a keyboard shortcut,
    /// a count, a short hint.
    /// </summary>
    public string? SecondaryText { get; set; }

    /// <summary>
    /// The custom value for the style attribute of the item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The value of the target attribute of the item when the item renders as an anchor tag (by providing the Href value).
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The custom template for the item.
    /// </summary>
    public RenderFragment<BitMenuButtonItem>? Template { get; set; }

    /// <summary>
    /// Text to render in the item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the item.
    /// </summary>
    public string? Title { get; set; }
}
