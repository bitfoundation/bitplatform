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

        [Parameter]
        public RenderFragment ChildContent { get; set; }
        public RenderFragment Content { get; set; }


        private List<BitPivotItem> _items = new List<BitPivotItem>();

        private int _activeItemIndex = 0;

        public int ActiveTabIndex { get => _activeItemIndex; set => _activeItemIndex = value; }

        internal void ItemClicked(BitPivotItem item)
        {
            _activeItemIndex = _items.IndexOf(item);
        }

        internal void RegisterOption(BitPivotItem item)
        {
            if (IsEnabled is false)
            {
                item.IsEnabled = false;
            }
            _items.Add(item);
        }

        internal void UnregisterOption(BitPivotItem item)
        {
            _items.Remove(item);
        }
    }
}
