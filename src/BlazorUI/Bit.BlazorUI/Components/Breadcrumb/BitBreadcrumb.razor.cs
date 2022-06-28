using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;


namespace Bit.Client.Web.BlazorUI
{
    public partial class BitBreadcrumb
    {
        protected override string RootElementClass => "bit-brc";

        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// Collection of breadcrumbs to render
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        [Parameter] public IList<BitBreadcrumbItem> Items { get; set; } = new List<BitBreadcrumbItem>();
#pragma warning restore CA2227 // Collection properties should be read only

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
        [Parameter] public BitIconName DividerIcon { get; set; } = BitIconName.ChevronRight;

        /// <summary>
        /// Render a custom overflow icon in place of the default icon
        /// </summary>
        [Parameter] public BitIconName OnRenderOverflowIcon { get; set; } = BitIconName.More;

        public string BreadcrumbItemsWrapperId { get; set; } = string.Empty;
        public string OverflowDropDownId { get; set; } = string.Empty;
        public string OverflowDropDownMenuCalloutId { get; set; } = string.Empty;
        public string OverflowDropDownMenuOverlayId { get; set; } = string.Empty;

        private IList<BitBreadcrumbItem> _overflowItems = new List<BitBreadcrumbItem>();
        private IList<BitBreadcrumbItem> _itemsToShowInBreadcrumb = new List<BitBreadcrumbItem>();
        private bool isOpen;

        protected async override Task OnParametersSetAsync()
        {
            BreadcrumbItemsWrapperId = $"breadcrumb-items-wrapper-{UniqueId}";
            OverflowDropDownId = $"overflow-dropdown-{UniqueId}";
            OverflowDropDownMenuOverlayId = $"overflow-dropdown-overlay-{UniqueId}";
            OverflowDropDownMenuCalloutId = $"overflow-dropdown-callout{UniqueId}";

            GetBreadcrumbItemsToShow();

            await base.OnParametersSetAsync();
        }

        private async Task CloseCallout()
        {
            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, BreadcrumbItemsWrapperId, OverflowDropDownId, OverflowDropDownMenuCalloutId, OverflowDropDownMenuOverlayId, isOpen);
            isOpen = false;
            StateHasChanged();
        }

        private async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled is false || JSRuntime is null) return;

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitOverflowDropDownMenu.toggleOverflowDropDownMenuCallout", obj, BreadcrumbItemsWrapperId, OverflowDropDownId, OverflowDropDownMenuCalloutId, OverflowDropDownMenuOverlayId, isOpen);
            isOpen = !isOpen;
        }

        private IList<BitBreadcrumbItem> GetBreadcrumbItemsToShow()
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
