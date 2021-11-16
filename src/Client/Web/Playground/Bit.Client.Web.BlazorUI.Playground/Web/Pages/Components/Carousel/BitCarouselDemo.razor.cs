using System;
using System.Collections.Generic;
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
            }
        };

        private readonly string carouselSampleCode = $"<BitCarousel>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>1/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img1.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Aurora</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>2/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img2.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Beautiful Mountain</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>3/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img3.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Forest In The Valley</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>4/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img4.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Road Among The Mountains</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"</BitCarousel>{Environment.NewLine}" +
            $"<style>{Environment.NewLine}" +
            $".numbertext {{ {Environment.NewLine}" +
            $"color: #DDDDDD;{Environment.NewLine}" +
            $"font-size: 12px;{Environment.NewLine}" +
            $"padding: 12px;{Environment.NewLine}" +
            $"position: absolute;{Environment.NewLine}" +
            $"top: 12px;{Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $".text {{ {Environment.NewLine}" +
            $"color: #FFFFFF;{Environment.NewLine}" +
            $"font-size: 15px;{Environment.NewLine}" +
            $"position: absolute;{Environment.NewLine}" +
            $"bottom: 55px;{Environment.NewLine}" +
            $"width: 100%;{Environment.NewLine}" +
            $"text-align: center;{Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $"</style>";

        private readonly string slideShowCarouselSampleCode = $"<BitCarousel IsSlideShow='true'>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>1/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img1.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Aurora</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>2/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img2.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Beautiful Mountain</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>3/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img3.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Forest In The Valley</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"<BitCarouselItem>{Environment.NewLine}" +
            $"<div class='numbertext'>4/4 </div>{Environment.NewLine}" +
            $"<img src='/images/carousel/img4.jpg' style='width:100%' />{Environment.NewLine}" +
            $"<div class='text'>Road Among The Mountains</div>{Environment.NewLine}" +
            $"</BitCarouselItem>{Environment.NewLine}" +
            $"</BitCarousel>{Environment.NewLine}" +
            $"<style>{Environment.NewLine}" +
            $".numbertext {{ {Environment.NewLine}" +
            $"color: #DDDDDD;{Environment.NewLine}" +
            $"font-size: 12px;{Environment.NewLine}" +
            $"padding: 12px;{Environment.NewLine}" +
            $"position: absolute;{Environment.NewLine}" +
            $"top: 12px;{Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $".text {{ {Environment.NewLine}" +
            $"color: #FFFFFF;{Environment.NewLine}" +
            $"font-size: 15px;{Environment.NewLine}" +
            $"position: absolute;{Environment.NewLine}" +
            $"bottom: 55px;{Environment.NewLine}" +
            $"width: 100%;{Environment.NewLine}" +
            $"text-align: center;{Environment.NewLine}" +
            $"}} {Environment.NewLine}" +
            $"</style>";
    }
}
