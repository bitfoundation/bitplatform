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
    public string? Href { get; set; }

    /// <summary>
    /// class HTML attribute for breadcrumb item.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Style HTML attribute for breadcrumb item.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Display the item as a current item.
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// Whether an item is enabled or not.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
