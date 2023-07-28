using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Accordion;

public partial class BitAccordionDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "DefaultIsExpanded",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value of the IsExpanded."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the Accordion."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short description in the header of Accordion."
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "Used to customize how the header inside the Accordion is rendered."
        },
        new()
        {
            Name = "IsExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the accordion is expanding or collapses."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the header is clicked."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the IsExpanded value has changed."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title in the header of Accordion."
        }
    };



    private readonly string example1HTMLCode = @"
<BitAccordion Title=""Accordion 1"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
</BitAccordion>

<BitAccordion Title=""Accordion 1"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl.
</BitAccordion>
<BitAccordion Title=""Accordion 2"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor.
</BitAccordion>
<BitAccordion Title=""Accordion 3"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit. Sed consequat condimentum massa, non euismod magna gravida vitae. Donec rhoncus suscipit blandit. Nunc ultrices vulputate nisl. Duis lobortis tristique nunc, id egestas ligula condimentum quis. Integer elementum tempor cursus. Phasellus vestibulum neque non laoreet faucibus. Nunc eu congue urna, in dapibus justo.
</BitAccordion>";

    private readonly string example2HTMLCode = @"
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
</BitAccordion>";

    private readonly string example3HTMLCode = @"
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
</BitAccordion>";
    private readonly string example3CSharpCode = @"
private byte controlledAccordionExpandedItem = 1;";

    private readonly string example4HTMLCode = @"
<BitToggle @bind-Value=""AccordionToggleIsEnabled"" OnText=""Enabled"" OffText=""Disabled"" Style=""margin-right: 10px;"" />
<BitToggle @bind-Value=""AccordionToggleIsEnabled"" OnText=""Expanded"" OffText=""Collapsed"" />

<BitAccordion Title=""Accordion 1""
                Description=""I am an accordion""
                IsEnabled=""@AccordionToggleIsEnabled""
                @bind-IsExpanded=""AccordionToggleIsExpanded"">
    Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis a elit vel lacus tincidunt dignissim. Phasellus mollis mauris orci, eget fermentum diam porta eu. Integer a consequat sapien, pellentesque aliquam velit. Nullam quis ligula vitae nisi accumsan auctor. Ut faucibus nulla a est commodo, vel sagittis neque tristique. In nec urna hendrerit, iaculis turpis sed, dictum elit. Sed id sagittis nunc, vitae ornare elit.
</BitAccordion>";
    private readonly string example4CSharpCode = @"
private bool AccordionToggleIsEnabled;
private bool AccordionToggleIsExpanded;";

    private readonly string example5HTMLCode = @"
<style>
    .custom-header {
        flex-grow: 1;
        display: flex;
        color: #0054C6;
        line-height: 1.5;
    }

    .custom-title {
        width: 30%;
        font-weight: 600;
        font-size: rem(16px);
    }
</style>

<BitAccordion>
    <HeaderTemplate Context=""isExpanded"">
        <BitIconButton IconName=""@(isExpanded ? BitIconName.ChevronDown : BitIconName.ChevronRight)"" />
        <div class=""custom-header"">
            <span class=""custom-title"">Accordion 1</span>
            <span>I am an accordion</span>
        </div>
    </HeaderTemplate>
    <ChildContent>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse malesuada lacus ex, sit amet blandit leo lobortis eget.
    </ChildContent>
</BitAccordion>

<BitAccordion Title=""Nature"" Description=""I am an accordion"">
    <BitCarousel AnimationDuration=""1"">
        <BitCarouselItem>
            <img src=""img1.jpg"">
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img2.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img3.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img4.jpg"" />
        </BitCarouselItem>
    </BitCarousel>
</BitAccordion>";
}
