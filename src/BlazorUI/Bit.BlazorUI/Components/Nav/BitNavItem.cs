﻿
namespace Bit.BlazorUI;

public class BitNavItem
{
    /// <summary>
    /// Aria label for nav link.
    /// Ignored if collapseAriaLabel or expandAriaLabel is provided
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Aria-current token for active nav links.
    /// Must be a valid token value, and defaults to 'page'
    /// </summary>
    public BitNavAriaCurrent AriaCurrent { get; set; } = BitNavAriaCurrent.Page;

    /// <summary>
    /// Aria label when items is collapsed and can be expanded
    /// </summary>
    public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// Aria label when group is collapsed and can be expanded.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// (Optional) By default, any link with onClick defined will render as a button. 
    /// Set this property to true to override that behavior. (Links without onClick defined will render as anchors by default.)
    /// </summary>
    public bool ForceAnchor { get; set; }

    /// <summary>
    /// A list of items to render as children of the current item
    /// </summary>
    public List<BitNavItem> ChildItems { get; set; } = new List<BitNavItem>();

    /// <summary>
    /// Name of an icon to render next to this link button
    /// </summary>
    public BitIconName? IconName { get; set; }

    /// <summary>
    /// Whether or not the link is in an expanded state
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// Whether or not the link is disabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// A unique value to use as a key or id of the item
    /// </summary>
    public string? Key { get; set; }

    /// <summary>
    /// Custom style for the each item element.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Text to render for this link.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Text for title tooltip.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Link target, specifies how to open the link
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// URL to navigate to for this link
    /// </summary>
#pragma warning disable CA1056 // URI-like properties should not be strings
    public string? Url { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings
}
