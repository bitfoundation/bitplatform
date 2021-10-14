using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCarouselItem
    {
        internal bool IsCurrent;
        
        [Parameter] public RenderFragment? ChildContent { get; set; }
        
        [CascadingParameter] protected BitCarousel? Carousel { get; set; }


        protected override Task OnInitializedAsync()
        {
            if (Carousel is not null)
            {
                Carousel.RegisterItem(this);
            }

            return base.OnInitializedAsync();
        }

        protected override string RootElementClass => "bit-crslitm";
    }
}
