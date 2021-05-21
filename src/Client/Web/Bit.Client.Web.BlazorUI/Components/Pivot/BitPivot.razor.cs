using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivot
    {
        protected override string RootElementClass => "bit-pvt";

        private string selectedKey;
        private OverflowBehavior _overflowBehavior = OverflowBehavior.None;
        private LinkFormat _linkFormat = LinkFormat.Links;
        private LinkSize _linkSize = LinkSize.Normal;

        [Parameter]
        public string DefaultSelectedKey { get; set; } = "0";

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public OverflowBehavior OverflowBehavior
        {
            get => _overflowBehavior;
            set
            {
                _overflowBehavior = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public LinkFormat LinkFormat
        {
            get => _linkFormat;
            set
            {
                _linkFormat = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public LinkSize LinkSize
        {
            get => _linkSize;
            set
            {
                _linkSize = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter]
        public bool HeadersOnly { get; set; } = false;


        [Parameter]
        public EventCallback<BitPivotItem> OnLinkClick { get; set; }

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

        [Parameter]
        public EventCallback<string> SelectedKeyChanged { get; set; }

        public IDictionary<string, BitPivotItem> Items = new Dictionary<string, BitPivotItem>();

        protected override void OnInitialized()
        {
            selectedKey = selectedKey ?? DefaultSelectedKey;
            base.OnInitialized();
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
                                      : string.Empty);
        }

        internal async Task ItemClicked(KeyValuePair<string, BitPivotItem> item)
        {
            if (item.Value.IsEnabled is false) return;

            await OnLinkClick.InvokeAsync(item.Value);

            if (SelectedKeyChanged.HasDelegate)
            {
                await SelectedKeyChanged.InvokeAsync(item.Key);
            }
            else
            {
                SelectedKey = item.Key;
            }
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

        internal void NotifyStateChanged() => StateHasChanged();
    }
}
