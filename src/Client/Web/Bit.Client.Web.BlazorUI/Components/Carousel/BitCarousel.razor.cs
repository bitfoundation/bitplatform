using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitCarousel
    {
        private List<BitCarouselItem> AllCarouselItems = new();

        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override string RootElementClass => "bit-crsl";


        internal void RegisterItem(BitCarouselItem carouselItem)
        {
            if (IsEnabled is false)
            {
                carouselItem.IsEnabled = false;
            }
            AllCarouselItems.Add(carouselItem);
        }

        internal void UnregisterItem(BitCarouselItem carouselItem)
        {
            AllCarouselItems.Remove(carouselItem);
        }
    }
}
