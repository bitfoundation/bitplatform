using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class Breadcrumb
    {
        protected override string RootElementClass => "bit-brc";
        /// <summary>
        /// Collection of breadcrumbs to render
        /// </summary>
        [Parameter] public List<BreadcrumbItem> Items { get; set; } = new List<BreadcrumbItem>();
        /// <summary>
        /// The maximum number of breadcrumbs to display before coalescing.
        /// If not specified, all breadcrumbs will be rendered.
        /// </summary>
        [Parameter] public byte? MaxDisplayedItems { get; set; }

        /// <summary>
        /// Aria label for the overflow button.
        /// </summary>
        [Parameter] public string? OverflowAriaLabel { get; set; } = "bit-icon--More";

        /// <summary>
        /// Optional index where overflow items will be collapsed.
        /// </summary>
        [Parameter] public byte? OverflowIndex { get; set; }
        /// <summary>
        /// Render a custom divider in place of the default chevron >
        /// </summary>
        [Parameter] public string DividerAs { get; set; } = "bit-icon--ChevronRight";

        [Parameter] public string OnRenderOverflowIcon { get; set; } = "bit-icon--More";
    }
}
