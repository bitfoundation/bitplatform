using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Carousel
{
    public partial class BitCarouselDemo
    {
        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            { Name = "AriaLabel", Type = "string", DefaultValue = "", Description = "The aria-label of the control for the benefit of screen readers.", },
            new()
            { Name = "Class", Type = "string", DefaultValue = "", Description = "Custom CSS class for the root element of the component." },
            new()
            { Name = "IsSlideShow", Type = "bool", DefaultValue = "false", Description = "If set to true the carousel item after last one show the first item." },
            new()
            { Name = "ChildContent", Type = "BitCarouselItem", DefaultValue = "", Description = "Item in each slide in carousel." },
            new()
            { Name = "SelectedKey", Type = "string", DefaultValue = "", Description = "The selected key of carousel item. selected key show as first item" }
        };
    }
}
