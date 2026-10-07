namespace Bit.BlazorUI;

public class BitBreadcrumbItem
{
    /// <summary>
    /// The accessible label of the breadcrumb item, replacing its text content for assistive technologies.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// CSS class attribute for breadcrumb item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// URL to navigate to when the breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Icon to render next to the item text.
    /// </summary>
    public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// Name of an icon to render next to the item text.
    /// </summary>
    public string? IconName { get; set; }

    /// <summary>
    /// Where the icon is rendered relative to the text, in place of the IconPlacement of the breadcrumb.
    /// </summary>
    public BitPlacement? IconPlacement { get; set; }

    /// <summary>
    /// Whether an item is disabled or not.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Display the breadcrumb item as the selected item.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// A unique value to use as a key of the breadcrumb item.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Click event handler of the breadcrumb item.
    /// </summary>
    public Action<BitBreadcrumbItem>? OnClick { get; set; }

    /// <summary>
    /// The custom template for the item in overflow list.
    /// </summary>
    public RenderFragment<BitBreadcrumbItem>? OverflowTemplate { get; set; }

    /// <summary>
    /// The relationship of the link of the breadcrumb item to the current document, rendered as the rel attribute of its anchor.
    /// </summary>
    /// <remarks>
    /// A link opening a new tab (<see cref="Target"/> of <c>_blank</c>) gets <c>noopener</c> added to whatever this
    /// says, unless it already says what the opener relationship should be (<see cref="BitLinkRels.NoOpener"/>,
    /// <see cref="BitLinkRels.NoReferrer"/> or <see cref="BitLinkRels.Opener"/>). Nothing adds <c>noreferrer</c> on its
    /// own: ask for it here where the page the link leads to must not learn which page it was followed from.
    /// </remarks>
    public BitLinkRels? Rel { get; set; }

    /// <summary>
    /// Style attribute for breadcrumb item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// The target of the link of the breadcrumb item (for example "_blank"), applied when the Href is provided.
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The custom template for the item.
    /// </summary>
    public RenderFragment<BitBreadcrumbItem>? Template { get; set; }

    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The title (tooltip) of the breadcrumb item, useful to reveal the full text of a truncated item.
    /// </summary>
    public string? Title { get; set; }
}
