using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Accordion;

public partial class BitAccordionDemo
{
    private bool accordionWithDescription;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "DefaultIsExpanded",
            Type = "bool?",
            Description = "Default value of the IsExpanded."
        },
        new ComponentParameter
        {
            Name = "Description",
            Type = "string?",
            Description = "A short description in the header of Accordion."
        },
        new ComponentParameter
        {
            Name = "ContentTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize how the content inside the Accordion is rendered."
        },
        new ComponentParameter
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<bool>?",
            Description = "Used to customize how the header inside the Accordion is rendered."
        },
        new ComponentParameter
        {
            Name = "IsExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the accordion is expanding or collapses."
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the header is clicked."
        },
        new ComponentParameter
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the IsExpanded value has changed."
        },
        new ComponentParameter
        {
            Name = "Title",
            Type = "string?",
            Description = "Title in the header of Accordion."
        },
        new ComponentParameter
        {
            Name = "Text",
            Type = "string?",
            Description = "Text in the content of Accordion."
        }
    };

    private readonly string example1CSharpCode = @"
private bool accordionWithDescription;
";

    private readonly string example1HTMLCode = @"
<BitAccordion Title=""Basic Accordion ""
              Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.""
              Style=""margin-bottom: 10px;"" />

<BitAccordion Title=""Accordion with Description""
              Description=""This Accordion is two way bind""
              Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.""
              Style=""margin-bottom: 10px;""
              @bind-IsExpanded=""accordionWithDescription"" />

<BitAccordion Title=""Accordion with DefaultIsExpanded""
              Description=""This Accordion by default is expanded""
              Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.""
              DefaultIsExpanded=""true""
              Style=""margin-bottom: 10px;"" />

<BitAccordion Title=""Disabled Accordion""
              Description=""This Accordion is disable""
              Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.""
              IsEnabled=""false"" />
";

    private readonly string example2HTMLCode = @"
<BitAccordion Class=""custom-acd""
              Style=""margin-bottom: 10px;""
              Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."">
    <HeaderTemplate Context=""IsExpanded"">
        @if (IsExpanded)
        {
            <BitIconButton IconName=""BitIconName.ChevronDown"" />
        }
        else
        {
            <BitIconButton IconName=""BitIconName.ChevronRight"" />
        }
        <span class=""custom-header-title"">Accordion with HeaderTemplate</span>
        <span class=""custom-header-desc"">This Accordion header is customazed</span>
    </HeaderTemplate>
</BitAccordion>

<BitAccordion Title=""Accordion with ContentTemplate""
              Description=""This Accordion content is customazed by the BitCarousel"">
    <ContentTemplate>
        <BitCarousel AnimationDuration=""1"">
            <BitCarouselItem>
                <img src=""/images/carousel/img1.jpg"">
            </BitCarouselItem>
            <BitCarouselItem>
                <img src=""/images/carousel/img2.jpg"" />
            </BitCarouselItem>
            <BitCarouselItem>
                <img src=""/images/carousel/img3.jpg"" />
            </BitCarouselItem>
            <BitCarouselItem>
                <img src=""/images/carousel/img4.jpg"" />
            </BitCarouselItem>
        </BitCarousel>
    </ContentTemplate>
</BitAccordion>
";

}
