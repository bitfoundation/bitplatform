using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivot
    {
        protected override string RootElementClass => "bit-pvt";

        private string? selectedKey;
        private OverflowBehavior overflowBehavior = OverflowBehavior.None;
        private LinkFormat linkFormat = LinkFormat.Links;
        private LinkSize linkSize = LinkSize.Normal;
        private bool SelectedKeyHasBeenSet;
        private bool hasSetSelectedKey;

        /// <summary>
        /// Default selected key for the pivot
        /// </summary>
        [Parameter]
        public string DefaultSelectedKey { get; set; } = "0";

        /// <summary>
        /// The content of pivot, It can be Any custom tag
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Overflow behavior when there is not enough room to display all of the links/tabs
        /// </summary>
        [Parameter]
        public OverflowBehavior OverflowBehavior
        {
            get => overflowBehavior;
            set
            {
                overflowBehavior = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Pivot link format, display mode for the pivot links
        /// </summary>
        [Parameter]
        public LinkFormat LinkFormat
        {
            get => linkFormat;
            set
            {
                linkFormat = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Pivot link size
        /// </summary>
        [Parameter]
        public LinkSize LinkSize
        {
            get => linkSize;
            set
            {
                linkSize = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// Whether to skip rendering the tabpanel with the content of the selected tab
        /// </summary>
        [Parameter]
        public bool HeadersOnly { get; set; } = false;

        /// <summary>
        /// Callback for when the selected pivot item is changed
        /// </summary>
        [Parameter]
        public EventCallback<BitPivotItem> OnLinkClick { get; set; }

        /// <summary>
        /// Key of the selected pivot item. Updating this will override the Pivot's selected item state
        /// </summary>
        [Parameter]
        public string SelectedKey
        {
            get => selectedKey;
            set
            {
                if (value == selectedKey) return;
                selectedKey = value;
                _ = SelectedKeyChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Callback for when the selected key changed
        /// </summary>
        [Parameter]
        public EventCallback<string> SelectedKeyChanged { get; set; }

        internal IDictionary<string, BitPivotItem> Items = new Dictionary<string, BitPivotItem>();

        protected override void OnInitialized()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            base.OnInitialized();
        }

        protected override Task OnParametersSetAsync()
        {
            return base.OnParametersSetAsync();
        }

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => LinkSize == LinkSize.Large ? $"{RootElementClass}-large-{VisualClassRegistrar()}"
                                      : LinkSize == LinkSize.Normal ? $"{RootElementClass}-normal-{VisualClassRegistrar()}"
                                      : string.Empty);

            ClassBuilder.Register(() => LinkFormat == LinkFormat.Links ? $"{RootElementClass}-links-{VisualClassRegistrar()}"
                                      : LinkFormat == LinkFormat.Tabs ? $"{RootElementClass}-tabs-{VisualClassRegistrar()}"
                                      : string.Empty);

            ClassBuilder.Register(() => OverflowBehavior == OverflowBehavior.Menu ? $"{RootElementClass}-menu-{VisualClassRegistrar()}"
                                      : OverflowBehavior == OverflowBehavior.Scroll ? $"{RootElementClass}-scroll-{VisualClassRegistrar()}"
                                      : OverflowBehavior == OverflowBehavior.None ? $"{RootElementClass}-none-{VisualClassRegistrar()}"
                                      : string.Empty);
        }

        internal async Task ItemClicked(KeyValuePair<string, BitPivotItem> item)
        {
            if (item.Value.IsEnabled is false) return;

            await OnLinkClick.InvokeAsync(item.Value);

            if (hasSetSelectedKey && SelectedKeyChanged.HasDelegate is false) return;
            SelectedKey = item.Key;
        }

        internal void RegisterOption(BitPivotItem item)
        {
            if (IsEnabled is false)
            {
                item.IsEnabled = false;
            }

            if (item.ItemKey is null)
            {
                item.ItemKey = GenerateUniqueKey();
            }

            Items.Add(item.ItemKey, item);
            StateHasChanged();
        }

        internal void UnregisterOption(BitPivotItem item)
        {
            Items.Remove(item.ItemKey);
        }

        private string GenerateUniqueKey()
        {
            var key = 0;

            while (Items.Keys.Contains(key.ToString()))
            {
                key++;
            }
            return key.ToString();
        }

        private string GetItemClass(BitPivotItem item)
        {
            return Items[SelectedKey] == item ? "selected-item" : string.Empty;
        }

        private string GetItemStyle(BitPivotItem item)
        {
            return item.Visibility == ComponentVisibility.Collapsed ? "display:none" : item.Visibility == ComponentVisibility.Hidden ? "visibility:hidden" : string.Empty;
        }

        internal void NotifyStateChanged() => StateHasChanged();
    }
}
