using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Carousel;

public partial class BitCarouselDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "The aria-label of the control for the benefit of screen readers."
        },
        new()
        {
            Name = "Class",
            Type = "string",
            DefaultValue = "",
            Description = "Custom CSS class for the root element of the component."
        },
        new()
        {
            Name = "IsSlideShow",
            Type = "bool",
            DefaultValue = "false",
            Description = "If enabled the carousel items will navigate in a loop (first item comes after last item and last item comes before first item)."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Items of the carousel."
        },
        new()
        {
            Name = "SelectedKey",
            Type = "string",
            DefaultValue = "",
            Description = "The Key of the current item of the carousel."
        },
        new()
        {
            Name = "ShowDots",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the Dots indicator at the bottom of the BitCarousel."
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<style>
    .number {
        position: absolute;
        top: 0.75rem;
        padding: 0.75rem;
        font-size: 0.75rem;
        color: #D7D7D7;
    }
</style>

<BitCarousel>
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img src=""/images/carousel/img1.jpg"" style=""width:100%"">
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img src=""/images/carousel/img2.jpg"" style=""width:100%"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img src=""/images/carousel/img3.jpg"" style=""width:100%"" />
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img src=""/images/carousel/img4.jpg"" style=""width:100%"" />
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example2HTMLCode = @"
<style>
    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel IsSlideShow=""true"">
    <BitCarouselItem>
        <img src=""/images/carousel/img1.jpg"" style=""width:100%"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img2.jpg"" style=""width:100%"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img3.jpg"" style=""width:100%"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img4.jpg"" style=""width:100%"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";

    private readonly string example3HTMLCode = @"
<style>
    .text-title {
        position: absolute;
        bottom: 3.4375rem;
        width: 100%;
        font-size: 0.9375rem;
        text-align: center;
        color: #FFFFFF;
    }

    .text-description {
        position: absolute;
        bottom: 1.875rem;
        width: 100%;
        font-size: 0.6875rem;
        text-align: center;
        color: #FFFFFF;
    }
</style>

<BitCarousel IsSlideShow=""true"" ShowDots=""false"">
    <BitCarouselItem>
        <img src=""/images/carousel/img1.jpg"" style=""width:100%"" />
        <div class=""text-title"">Aurora</div>
        <div class=""text-description"">This is Aurora and it's fantastic</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img2.jpg"" style=""width:100%"" />
        <div class=""text-title"">Beautiful Mountain</div>
        <div class=""text-description"">This is a Beautiful Mountain and it's gorgeous</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img3.jpg"" style=""width:100%"" />
        <div class=""text-title"">Forest In The Valley</div>
        <div class=""text-description"">This is a Forest In The Valley and it's beautiful</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <img src=""/images/carousel/img4.jpg"" style=""width:100%"" />
        <div class=""text-title"">Road Among The Mountains</div>
        <div class=""text-description"">This is a Road Among The Mountains and it's amazing</div>
    </BitCarouselItem>
</BitCarousel>";
}
