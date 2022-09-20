using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Accordion;

public partial class BitAccordionDemo
{
    private byte controlledAccordionExpandedItem = 1;
    private bool AccordionToggleIsEnabled;
    private bool AccordionToggleIsExpanded;

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

    private readonly string example1HTMLCode = @"
<div class=""accordion-example-box"">
    <BitAccordion Title=""Accordion 1""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."" />
</div>

<div class=""example-desc"">You can define multiple accordions together.</div>
<div class=""accordion-example-box"">
    <BitAccordion Title=""Accordion 1""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."" />
    <BitAccordion Title=""Accordion 2""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."" />
    <BitAccordion Title=""Accordion 3""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."" />
</div>
";
    private readonly string example2HTMLCode = @"
<div class=""accordion-example-box"">
    <BitAccordion Title=""General settings""
                    Description=""I am an accordion""
                    Text=""Nulla facilisi. Phasellus sollicitudin nulla et quam mattis feugiat. Aliquam eget maximus est, id dignissim quam."" />
    <BitAccordion Title=""Users""
                    Description=""You are currently not an owner""
                    Text=""Donec placerat, lectus sed mattis semper, neque lectus feugiat lectus, varius pulvinar diam eros in elit. Pellentesque convallis laoreet laoreet."" />
    <BitAccordion Title=""Advanced settings""
                    Description=""Filtering has been entirely disabled for whole web server""
                    Text=""Nunc vitae orci ultricies, auctor nunc in, volutpat nisl. Integer sit amet egestas eros, vitae egestas augue. Duis vel est augue."" />
    <BitAccordion Title=""Advanced settings""
                    Description=""Filtering has been entirely disabled for whole web server""
                    Text=""Nunc vitae orci ultricies, auctor nunc in, volutpat nisl. Integer sit amet egestas eros, vitae egestas augue. Duis vel est augue."" />
</div>
";
    private readonly string example3HTMLCode = @"
<div class=""accordion-example-box"">
    <BitAccordion Title=""General settings""
                    Description=""I am an accordion""
                    Text=""Nulla facilisi. Phasellus sollicitudin nulla et quam mattis feugiat. Aliquam eget maximus est, id dignissim quam.""
                    OnClick=""() => controlledAccordionExpandedItem = 1""
                    IsExpanded=""controlledAccordionExpandedItem == 1"" />
    <BitAccordion Title=""Users""
                    Description=""You are currently not an owner""
                    Text=""Donec placerat, lectus sed mattis semper, neque lectus feugiat lectus, varius pulvinar diam eros in elit. Pellentesque convallis laoreet laoreet.""
                    OnClick=""() => controlledAccordionExpandedItem = 2""
                    IsExpanded=""controlledAccordionExpandedItem == 2"" />
    <BitAccordion Title=""Advanced settings""
                    Description=""Filtering has been entirely disabled for whole web server""
                    Text=""Nunc vitae orci ultricies, auctor nunc in, volutpat nisl. Integer sit amet egestas eros, vitae egestas augue. Duis vel est augue.""
                    OnClick=""() => controlledAccordionExpandedItem = 3""
                    IsExpanded=""controlledAccordionExpandedItem == 3"" />
</div>
";
    private readonly string example4HTMLCode = @"
<div class=""example-operator-box"">
    <BitToggle @bind-Value=""AccordionToggleIsEnabled"" OnText=""Enabled"" OffText=""Disabled"" Style=""margin-right: 10px;"" />
    <BitToggle @bind-Value=""AccordionToggleIsEnabled"" OnText=""Expanded"" OffText=""Collapsed"" />
</div>
<div class=""accordion-example-box disabled-example"">
    <BitAccordion Title=""Accordion 1""
                    Description=""I am an accordion""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.""
                    IsEnabled=""@AccordionToggleIsEnabled""
                    @bind-IsExpanded=""AccordionToggleIsEnabled"" />
</div>
";
    private readonly string example5HTMLCode = @"
<style>
    ::deep .custom-acd {
        .header {
            align-items: center;
        }

        .custom-header-intro {
            display: flex;
            flex-grow: 1;
            line-height: 1.5;
            color: $B1Color;

            .custom-header-title {
                font-size: rem(16px);
                font-weight: 600;
                width: 30%;
            }

            @media only screen and (max-width: #{em(639px)}) {
                flex-direction: column;

                .custom-header-title {
                    width: 100%;
                }
            }
        }
    }
</style>

<div class=""accordion-example-box"">
    <BitAccordion Class=""custom-acd""
                    Text=""Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget."">
        <HeaderTemplate Context=""isExpanded"">
            @if (isExpanded)
            {
                <BitIconButton IconName=""BitIconName.ChevronDown"" />
            }
            else
            {
                <BitIconButton IconName=""BitIconName.ChevronRight"" />
            }
            <div class=""custom-header-intro"">
                <span class=""custom-header-title"">Accordion 1</span>
                <span>I am an accordion</span>
            </div>
        </HeaderTemplate>
    </BitAccordion>
</div>

<div class=""accordion-example-box"">
    <BitAccordion Title=""Nature""
                    Description=""I am an accordion"">
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
</div>
";
    private readonly string example3CSharpCode = @"
private byte controlledAccordionExpandedItem = 1;
";
    private readonly string example4CSharpCode = @"
private bool AccordionToggleIsEnabled;
private bool AccordionToggleIsExpanded;
";
}
