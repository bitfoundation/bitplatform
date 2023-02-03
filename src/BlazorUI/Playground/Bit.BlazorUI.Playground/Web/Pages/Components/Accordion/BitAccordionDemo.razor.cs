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
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of the Accordion."
        },
        new ComponentParameter
        {
            Name = "Description",
            Type = "string?",
            Description = "A short description in the header of Accordion."
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
        }
    };

    private readonly string example1HTMLCode = @"
<BitAccordion Title=""Accordion 1"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
</BitAccordion>

<div class=""example-desc"">You can define multiple accordions together.</div>
<div class=""accordion-example-box"">
    <BitAccordion Title=""Accordion 1"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl.
    </BitAccordion>
    <BitAccordion Title=""Accordion 2"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor.
    </BitAccordion>
    <BitAccordion Title=""Accordion 3"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
    </BitAccordion>
</div>
";
    private readonly string example2HTMLCode = @"
<div class=""accordion-example-box"">
    <BitAccordion Title=""General settings"" Description=""I am an accordion"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu.
    </BitAccordion>
    <BitAccordion Title=""Users"" Description=""You are currently not an owner"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique.
    </BitAccordion>
    <BitAccordion Title=""Advanced settings"" Description=""Filtering has been entirely disabled for whole web server"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit.
    </BitAccordion>
    <BitAccordion Title=""Advanced settings"" Description=""Filtering has been entirely disabled for whole web server"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae.
    </BitAccordion>
</div>
";
    private readonly string example3HTMLCode = @"
<div class=""accordion-example-box"">
    <BitAccordion Title=""General settings""
                  Description=""I am an accordion""
                  OnClick=""() => controlledAccordionExpandedItem = 1""
                  IsExpanded=""controlledAccordionExpandedItem == 1"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl.
    </BitAccordion>
    <BitAccordion Title=""Users""
                  Description=""You are currently not an owner""
                  OnClick=""() => controlledAccordionExpandedItem = 2""
                  IsExpanded=""controlledAccordionExpandedItem == 2"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor.
    </BitAccordion>
    <BitAccordion Title=""Advanced settings""
                  Description=""Filtering has been entirely disabled for whole web server""
                  OnClick=""() => controlledAccordionExpandedItem = 3""
                  IsExpanded=""controlledAccordionExpandedItem == 3"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu.
    </BitAccordion>
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
                  IsEnabled=""@AccordionToggleIsEnabled""
                  @bind-IsExpanded=""AccordionToggleIsExpanded"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit.
    </BitAccordion>
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
    <BitAccordion Class=""custom-acd"">
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
        <ChildContent>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.
        </ChildContent>
    </BitAccordion>
</div>

<div class=""accordion-example-box"">
    <BitAccordion Title=""Nature"" Description=""I am an accordion"">
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
