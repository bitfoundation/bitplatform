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
        [Parameter] public int MaxDisplayedItems { get; set; }

        /// <summary>
        /// Aria label for the overflow button.
        /// </summary>
        [Parameter] public string? OverflowAriaLabel { get; set; }

        /// <summary>
        /// Optional index where overflow items will be collapsed.
        /// </summary>
        [Parameter] public int OverflowIndex { get; set; }

        /// <summary>
        /// Render a custom divider in place of the default chevron >
        /// </summary>
        [Parameter] public BitIconName DividerAs { get; set; } = BitIconName.ChevronRight;

        [Parameter] public BitIconName OnRenderOverflowIcon { get; set; } = BitIconName.More;


        private List<BitBreadcrumbItem> _overflowItems = new();
        private List<BitBreadcrumbItem> _itemsToShowInBreadcrumb = new();

        private List<BitBreadcrumbItem> GetBreadcrumbItemsToShow()
        {
            if (MaxDisplayedItems == 0 || MaxDisplayedItems >= Items.Count)
            {
                return _itemsToShowInBreadcrumb = Items;
            }

            _itemsToShowInBreadcrumb.Clear();

            if (OverflowIndex >= MaxDisplayedItems)
                OverflowIndex = 0;

            var overflowItemsCount = Items.Count - MaxDisplayedItems;

            foreach ((BitBreadcrumbItem item, int index) item in Items.Select((item, index) => (item, index)))
            {
                if (OverflowIndex <= item.index && item.index < overflowItemsCount + OverflowIndex)
                {
                    if (item.index == OverflowIndex)
                        _itemsToShowInBreadcrumb.Add(item.item);

                    _overflowItems.Add(item.item);
                }
                else
                {
                    _itemsToShowInBreadcrumb.Add(item.item);
                }
            }

            return _itemsToShowInBreadcrumb;
        }


        private bool IsLastItem(int index)
        {
            return index == _itemsToShowInBreadcrumb.Count - 1;
        }
    }
}
