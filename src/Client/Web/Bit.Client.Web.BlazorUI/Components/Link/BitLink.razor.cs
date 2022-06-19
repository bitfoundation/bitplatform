using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitLink
    {
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

        /// <summary>
        /// URL the link points to
        /// </summary>
        [Parameter] public string? Href { get; set; } = string.Empty;

        /// <summary>
        /// If Href provided, specifies how to open the link
        /// </summary>
        [Parameter] public string? Target { get; set; }

        /// <summary>
        /// The content of link, can be any custom tag or a text
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Whether the link is styled with an underline or not. Should be used when the link is placed alongside other text content
        /// </summary>
        [Parameter] public bool HasUnderline { get; set; } = false;

        /// <summary>
        /// Callback for when the link clicked
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        protected override string RootElementClass => "bit-lnk";

        protected virtual async Task HandleClick(MouseEventArgs e)
        {
            if (IsEnabled)
            {
                await OnClick.InvokeAsync(e);
            }
        }

        private async Task ScrollToFragment()
        {
            await JSRuntime.BitLinkScrollToFragmentOnClickEvent(Href![1..]);
        }
    }
}
