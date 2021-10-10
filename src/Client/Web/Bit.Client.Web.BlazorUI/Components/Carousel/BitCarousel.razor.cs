using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI.Components.Carousel
{
    public partial class BitCarousel
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override string RootElementClass => "bit-crsl";
    }
}
