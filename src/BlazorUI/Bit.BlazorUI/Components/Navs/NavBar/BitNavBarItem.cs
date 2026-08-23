namespace Bit.BlazorUI;

public class BitNavBarItem
{
    /// <summary>
    /// The value of the aria-current attribute of the navbar item when it is the selected one.
    /// </summary>
    public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// The accessible label of the navbar item, announced instead of its content.
    /// When it is not provided and the text of the item is not rendered (in the IconOnly or the
    /// HideUnselectedText modes), the text of the item is used, so an icon is never left unnamed.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The badge text to render on the icon of the navbar item, for a count or a short status.
    /// Takes precedence over <see cref="Dot"/> when both are set.
    /// </summary>
    public string? Badge { get; set; }

    /// <summary>
    /// The accessible description of the badge (or the dot) of the navbar item, folded into the accessible
    /// name of the item so a count that only exists as a colored bubble is not lost on a screen reader
    /// ("Messages (5 unread)"). It falls back to the <see cref="Badge"/> text, and a <see cref="Dot"/> is
    /// only announced while this is set, since a dot carries no text of its own.
    /// </summary>
    public string? BadgeAriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the navbar item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The custom data for the navbar item to provide additional state.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Renders a small dot on the icon of the navbar item, to mark it as needing attention without
    /// showing a number. Ignored while <see cref="Badge"/> is set.
    /// </summary>
    public bool Dot { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    /// <example>
    /// FontAwesome: Icon = BitIconInfo.Fa("solid house")
    /// Bootstrap: Icon = BitIconInfo.Bi("gear-fill")
    /// Custom CSS: Icon = BitIconInfo.Css("my-icon-class")
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display from the built-in Fluent UI icons.
    /// </summary>
    /// <remarks>
    /// The icon name should be from the Fluent UI icon set (e.g., <c>BitIconName.Home</c>).
    /// <br />
    /// For external icon libraries, use <see cref="Icon"/> instead.
    /// </remarks>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the navbar item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key or id of the navbar item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Modifies how the URL of the navbar item is matched against the current URL in the automatic mode.
    /// Takes precedence over the Match of the navbar itself.
    /// <br />
    /// A Wildcard (or a Regex) URL is run as the pattern it was written as, so unlike an Exact or a Prefix
    /// URL it is never normalized: it has to be app-relative and to carry its leading slash itself, as in
    /// "/products/*".
    /// </summary>
    public BitNavMatch? Match { get; set; }

    /// <summary>
    /// The icon to display while the navbar item is the selected one, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="SelectedIconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// A navigation bar conventionally marks its current destination with the filled variant of the same
    /// glyph, which is what this pair of properties is for. While neither is set, the selected item keeps
    /// its <see cref="Icon"/> / <see cref="IconName"/>.
    /// </remarks>
    public BitIconInfo? SelectedIcon { get; set; }

    /// <summary>
    /// The name of the icon to display from the built-in Fluent UI icons while the navbar item is the
    /// selected one. For external icon libraries, use <see cref="SelectedIcon"/> instead.
    /// </summary>
    public string? SelectedIconName { get; set; }

    /// <summary>
    /// Custom CSS style for the navbar item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Link target, specifies how to open the navbar item's link.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The custom template for the navbar item to render.
    /// </summary>
    public RenderFragment<BitNavBarItem>? Template { get; set; }

    /// <summary>
    /// Whether the <see cref="Template"/> of the navbar item is rendered inside the anchor (or the button)
    /// the item is, or replaces it altogether, which is what an item that is a control of its own - the
    /// center action of a mobile bar, for instance - needs, since an interactive element cannot be nested
    /// in another one. A replaced item is left out of the keyboard navigation of the navbar, and whatever it
    /// renders owns its own clicks, its own focus and its own accessible name.
    /// </summary>
    public BitNavItemTemplateRenderMode TemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Text to render for the navbar item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Text for the tooltip of the navbar item.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The navbar item's link URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected navbar item by the current URL.
    /// </summary>
    public IEnumerable<string>? AdditionalUrls { get; set; }
}
