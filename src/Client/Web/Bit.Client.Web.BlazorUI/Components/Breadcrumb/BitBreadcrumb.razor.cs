using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitBreadcrumb
    {
        protected override string RootElementClass => "bit-brc";

        /// <summary>
        /// Collection of breadcrumbs to render
        /// </summary>
        [Parameter] public List<BitBreadcrumbItem> Items { get; set; } = new();

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
        [Parameter] public byte OverflowIndex { get; set; }

        /// <summary>
        /// Render a custom divider in place of the default chevron >
        /// </summary>
        [Parameter] public string DividerAs { get; set; } = "bit-icon--ChevronRight";

        [Parameter] public string OnRenderOverflowIcon { get; set; } = "bit-icon--More";


        private List<BitBreadcrumbItem> _removedItemsFromBreadcrumb = new();

        private List<BitBreadcrumbItem> GetBreadcrumbItemsToShow()
        {
            if (MaxDisplayedItems == null || MaxDisplayedItems >= Items.Count)
            {
                return Items;
            }

            if (OverflowIndex >= MaxDisplayedItems!)
                OverflowIndex = 0;

            var overFlowItemsCount = Items.Count - (int)MaxDisplayedItems;

            var itemsToShowInBreadcrumb = new List<BitBreadcrumbItem>();

            foreach ((BitBreadcrumbItem item, int index) item in Items.Select((item, index) => (item, index)))
            {
                if (OverflowIndex <= item.index && item.index < overFlowItemsCount + (int)OverflowIndex)
                {
                    if (item.index == OverflowIndex)
                        itemsToShowInBreadcrumb.Add(item.item);

                    _removedItemsFromBreadcrumb.Add(item.item);
                }
                else
                {
                    itemsToShowInBreadcrumb.Add(item.item);
                }
            }

            return itemsToShowInBreadcrumb;
        }
    }
}
