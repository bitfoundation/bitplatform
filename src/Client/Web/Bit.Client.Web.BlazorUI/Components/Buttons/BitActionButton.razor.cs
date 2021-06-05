using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitActionButton
    {
        private int? tabIndex;

        [Parameter] public bool AllowDisabledFocus { get; set; } = true;
        [Parameter] public string? AriaDescription { get; set; }

        [Parameter] public bool AriaHidden { get; set; }

        [Parameter] public string? AriaLabel { get; set; }

        [Parameter] public string? IconName { get; set; }

        [Parameter] public RenderFragment? ChildContent { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-act-btn";

        protected override async Task OnInitializedAsync()
        {
            if (!IsEnabled)
            {
                tabIndex = AllowDisabledFocus ? null : -1;
            }

            await base.OnInitializedAsync();
        }

        protected virtual async Task HandleOnClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }
    }
}
