namespace Bit.BlazorUI;

public class BitNavItem
{
    /// <summary>
    /// Aria-current token for active nav item. Must be a valid token value, and defaults to 'page'.
    /// </summary>
    public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// Aria label for nav item. Ignored if CollapseAriaLabel or ExpandAriaLabel is provided.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Custom CSS class for the nav item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// A list of items to render as children of the current nav item.
    /// </summary>
    public List<BitNavItem> ChildItems { get; set; } = [];

    /// <summary>
    /// Aria label when nav item is collapsed and can be expanded.
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
    /// Aria label when nav item is collapsed and can be expanded.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Forces an anchor element render instead of button.
    /// </summary>
    public bool ForceAnchor { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

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
    /// Custom CSS style for the nav item.
    /// </summary>
    public string? Style { get; set; }

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
