namespace Bit.BlazorUI;

public class BitNavItem
{
    /// <summary>
    /// Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'.
    /// </summary>
    public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// The accessible name of the nav item, in place of its text. It also names a group header, whose
    /// CollapseAriaLabel/ExpandAriaLabel only apply when it is not set.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// The badge text to render after the text of the nav item, for a count or a short status ("3", "New").
    /// The item keeps its icon, its text and its states, and the badge is rendered after a custom template that
    /// renders in the Normal mode too. Takes precedence over <see cref="Dot"/> when both are set.
    /// </summary>
    /// <remarks>
    /// A group header in the Grouped render type does not render it, and neither does a separator or a template
    /// rendered in the Replace mode, which takes the place of everything the item would render. The IconOnly nav pins
    /// it to the corner of the icon.
    /// </remarks>
    public string? Badge { get; set; }

    /// <summary>
    /// The accessible description of the badge (or the dot) of the nav item, folded into the accessible name of
    /// the item so a count that only exists as a colored bubble is not lost on a screen reader ("Inbox (3 unread)").
    /// It falls back to the <see cref="Badge"/> text, and a <see cref="Dot"/> is only announced while this is set,
    /// since a dot carries no text of its own.
    /// </summary>
    public string? BadgeAriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the nav item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A list of items to render as children of the current nav item.
    /// </summary>
    public List<BitNavItem> ChildItems { get; set; } = [];

    /// <summary>
    /// Aria label of the toggle button when the nav item is expanded and can be collapsed.
    /// </summary>
    public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The custom data for the nav item to provide additional state.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// The description for the nav item.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Renders a small dot after the text of the nav item, to mark it as needing attention without showing a
    /// number. Ignored while <see cref="Badge"/> is set.
    /// </summary>
    public bool Dot { get; set; }

    /// <summary>
    /// Aria label of the toggle button when the nav item is collapsed and can be expanded.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Forces an anchor element render instead of button.
    /// </summary>
    public bool ForceAnchor { get; set; }

    /// <summary>
    /// Icon to render next to the nav item.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// Use this property to render icons from external libraries like FontAwesome, Material Icons, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </summary>
    /// <example>
    /// Bootstrap: Icon=BitIconInfo.Bi("gear-fill")
    /// FontAwesome: Icon=BitIconInfo.Fa("solid house")
    /// Custom CSS: Icon=BitIconInfo.Css("my-icon-class")
    /// </example>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav item is disabled.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Whether or not the nav item is in an expanded state.
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Indicates that the nav item should render as a separator.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// A unique value to use as a key or id of the nav item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a value representing the URL matching behavior of the nav item.
    /// </summary>
    public BitNavMatch? Match { get; set; }

    /// <summary>
    /// The relationship of the link of the nav item to the current document, rendered as the rel attribute of its anchor.
    /// </summary>
    /// <remarks>
    /// A link opening a new tab (<see cref="Target"/> of <c>_blank</c>) gets <c>noopener</c> added to whatever this
    /// says, unless it already says what the opener relationship should be (<see cref="BitLinkRels.NoOpener"/>,
    /// <see cref="BitLinkRels.NoReferrer"/> or <see cref="BitLinkRels.Opener"/>). Nothing adds <c>noreferrer</c> on its
    /// own: ask for it here where the page the link leads to must not learn which page it was followed from.
    /// </remarks>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Custom CSS style for the nav item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The custom template to render at the end of the nav item, after its text and ahead of its badge, while
    /// the item keeps its default content. Takes precedence over the ItemSuffixTemplate of the nav.
    /// </summary>
    /// <remarks>
    /// It is rendered inside the anchor (or the button) the item is, so it holds content to look at - a counter,
    /// a status, a shortcut - and never a control of its own: a nested interactive element cannot be reached or
    /// named by assistive technologies. It is part of the accessible name of the item unless the item is named
    /// through an aria-label, so mark what it draws with <c>aria-hidden="true"</c> when it repeats the badge. The
    /// IconOnly nav, which is down to the icons of its items, does not render it.
    /// </remarks>
    public RenderFragment<BitNavItem>? SuffixTemplate { get; set; }

    /// <summary>
    /// Link target, specifies how to open the nav item's link.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The custom template for the nav item to render.
    /// </summary>
    public RenderFragment<BitNavItem>? Template { get; set; }

    /// <summary>
    /// The render mode of the nav item's custom template.
    /// </summary>
    public BitNavItemTemplateRenderMode TemplateRenderMode { get; set; } = BitNavItemTemplateRenderMode.Normal;

    /// <summary>
    /// Text to render for the nav item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for the tooltip of the nav item.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The nav item's link URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Alternative URLs to be considered when auto mode tries to detect the selected nav item by the current URL.
    /// </summary>
    public IEnumerable<string>? AdditionalUrls { get; set; }
}
