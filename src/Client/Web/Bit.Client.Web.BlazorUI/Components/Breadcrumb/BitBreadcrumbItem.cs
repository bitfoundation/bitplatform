using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI;

public class BitBreadcrumbItem
{
    /// <summary>
    /// Arbitrary unique string associated with the breadcrumb item.
    /// </summary>
    public string? Key { get; set; }

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
    /// Whether this is the breadcrumb item the user is currently navigated to.
    /// If true, aria-current="page" will be applied to this breadcrumb item.
    /// </summary>
    public bool IsCurrentItem { get; set; }

    /// <summary>
    /// Callback for when the breadcrumb item clicked
    /// </summary>
    public Action OnClick { get; set; } = default!;

}
