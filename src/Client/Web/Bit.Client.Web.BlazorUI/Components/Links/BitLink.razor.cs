
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLink
    {
        [Parameter] public string Target { get; set; }

        [Parameter] public string Href { get; set; } = string.Empty;

        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-lnk";

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }
    }
}
