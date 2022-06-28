using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Carousel
{
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
                Description = "If set true the carousel item comes after last one and shows as the first item."
            },
            new()
            { 
                Name = "ChildContent",
                Type = "BitCarouselItem",
                DefaultValue = "",
                Description = "Item in each slide of carousel."
            },
            new()
            { 
                Name = "SelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "The selected key of carousel item. selected key shows as the first item."
            },
            new ComponentParameter()
            {
                Name = "Visibility",
                Type = "BitComponentVisibility",
                LinkType = LinkType.Link,
                Href = "#component-visibility-enum",
                DefaultValue = "BitComponentVisibility.Visible",
                Description = "Whether the component is Visible,Hidden,Collapsed.",
            },
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

        private readonly string example1HTMLCode = @"<BitCarousel>
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img src=""/images/carousel/img1.jpg"" style=""width:100%"">
        <div class=""text"">Aurora</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img src=""/images/carousel/img2.jpg"" style=""width:100%"" />
        <div class=""text"">Beautiful Mountain</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img src=""/images/carousel/img3.jpg"" style=""width:100%"" />
        <div class=""text"">Forest In The Valley</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img src=""/images/carousel/img4.jpg"" style=""width:100%"" />
        <div class=""text"">Road Among The Mountains</div>
    </BitCarouselItem>
</BitCarousel>";

        private readonly string example2HTMLCode = @"<BitCarousel IsSlideShow=""true"">
    <BitCarouselItem>
        <div class=""numbertext"">1 / 4</div>
        <img src=""/images/carousel/img1.jpg"" style=""width:100%"" />
        <div class=""text"">Aurora</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">2 / 4</div>
        <img src=""/images/carousel/img2.jpg"" style=""width:100%"" />
        <div class=""text"">Beautiful Mountain</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">3 / 4</div>
        <img src=""/images/carousel/img3.jpg"" style=""width:100%"" />
        <div class=""text"">Forest In The Valley</div>
    </BitCarouselItem>
    <BitCarouselItem>
        <div class=""numbertext"">4 / 4</div>
        <img src=""/images/carousel/img4.jpg"" style=""width:100%"" />
        <div class=""text"">Road Among The Mountains</div>
    </BitCarouselItem>
</BitCarousel>";
    }
}
