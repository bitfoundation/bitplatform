using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
namespace Bit.Client.Web.BlazorUI
{
    public class BreadcrumbItem
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
        /// Callback for when the dropdown clicked
        /// </summary>
        public EventCallback<MouseEventArgs> OnClick { get; set; }
        /// <summary>
        /// Optional prop to render the item as a heading of your choice.
        /// You can also use this to force items to render as links instead of buttons 
        /// (by default, any item with a href renders as a link, and any item without a href renders as a button).
        /// This is not generally recommended because it may prevent activating the link using the keyboard.
        /// </summary>
        public RenderMode As { get; set; }

    }
    public enum RenderMode
    {
        a,
        h1,
        h2,
        h3,
        h4,
        h5,
        h6,
    }
}
