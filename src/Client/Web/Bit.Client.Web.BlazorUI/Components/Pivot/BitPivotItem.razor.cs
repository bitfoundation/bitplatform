using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitPivotItem : IDisposable
    {
        protected override string RootElementClass => "bit-pvt-itm";

        [CascadingParameter] protected internal BitPivot Pivot { get; set; }

        [Parameter]
        public RenderFragment HeaderContent { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment BodyContent { get; set; }

        [Parameter]
        public string HeaderText { get; set; }

        [Parameter]
        public string IconName { get; set; }

        [Parameter]
        public int? ItemCount { get; set; }

        [Parameter]
        public string ItemKey { get; set; }

        protected override Task OnInitializedAsync()
        {
            if (Pivot is not null)
            {
                Pivot.RegisterOption(this);
            }

            return base.OnInitializedAsync();
        }

        protected override void OnComponentVisibilityChanged(ComponentVisibility visibility)
        {
            Pivot.NotifyStateChanged();

            if (Pivot.Items[Pivot.SelectedKey] == this)
            {
                Pivot.SelectedKey = Pivot.Items.First().Key;
            }

            base.OnComponentVisibilityChanged(visibility);
        }

        public void Dispose()
        {
            if (Pivot is null) return;

            Pivot.UnregisterOption(this);
        }
    }
}
