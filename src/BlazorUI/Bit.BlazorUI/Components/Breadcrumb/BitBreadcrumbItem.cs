using System;

namespace Bit.BlazorUI;

public class BitBreadcrumbItem
{
    /// <summary>
    /// Text to display in the breadcrumb item.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// URL to navigate to when this breadcrumb item is clicked.
    /// If provided, the breadcrumb will be rendered as a link.
    /// </summary>
    public string? href { get; set; }

    /// <summary>
    /// Callback for when the breadcrumb item clicked
    /// </summary>
    public Action OnClick { get; set; } = default!;

    /// <summary>
    /// class HTML attribute for breadcrumb item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    public string? Style { get; set; }
}
