namespace Bit.BlazorUI;

public class BitNavBarItem
{
    /// <summary>
    /// Custom CSS class for the nav item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// The custom data for the nav item to provide additional state.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Name of an icon to render next to the nav item.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Whether or not the nav item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

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
    public RenderFragment<BitNavBarItem>? Template { get; set; }

    /// <summary>
    /// Text to render for the nav item.
    /// </summary>
    public string? Text { get; set; }

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
