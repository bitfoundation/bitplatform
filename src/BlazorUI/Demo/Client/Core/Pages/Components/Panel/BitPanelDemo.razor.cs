using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Panel;

public partial class BitPanelDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the auto scrollbar toggle behavior of the Panel.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of Panel, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "ClassStyles",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Href = "#class-styles",
            LinkType = LinkType.Link,
            Description = "Custom CSS classes/styles for different parts of the BitPanel component."
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the footer inside the Panel is rendered.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the header inside the Panel is rendered.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Header text of Panel.",
        },
        new()
        {
            Name = "IsBlocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).",
        },
        new()
        {
            Name = "IsModeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog is displayed.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Panel is dismissed.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPanelPosition",
            LinkType = LinkType.Link,
            Href = "#component-position-enum",
            DefaultValue = "BitPanelPosition.Right",
            Description = "Position of the modal on the screen.",
        },
        new()
        {
            Name = "Size",
            Type = "double",
            DefaultValue = "320",
            Description = "Provides Height or Width for the Panel.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Panel disables its scroll if applicable.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the close button of the Panel.",
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Panel, if any.",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Panel, if any.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitPanelClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Container",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the Panel container.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "ScrollContainer",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the panel scroll container.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "Header",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the panel header.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "Body",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the panel body.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "Footer",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the panel footer.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               }
            }
        },
        new()
        {
            Id = "class-style-pair",
            Title = "BitClassStylePair",
            Parameters = new()
            {
               new()
               {
                   Name = "Class",
                   Type = "string?",
                   Description = "Custom CSS class."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom CSS style."
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "component-position-enum",
            Name = "BitPanelPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() { Name = "Right", Value = "0" },
                new() { Name = "Left", Value = "1" },
                new() { Name = "Top", Value = "2" },
                new() { Name = "Bottom", Value = "3" }
            }
        }
    };



    private readonly string example1HTMLCode = @"
<BitButton OnClick=@(() => IsBasicPanelOpen = true)>Open Panel</BitButton>

<BitPanel @bind-IsOpen=""IsBasicPanelOpen"">
    Content goes here.
</BitPanel>";
    private readonly string example1CSharpCode = @"
private bool IsBasicPanelOpen = false;";

    private readonly string example2HTMLCode = @"
<BitLabel>Panel with header text</BitLabel>
<BitButton OnClick=@(() => IsPanelWithHeaderTextOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with custom header content</BitLabel>
<BitButton OnClick=@(() => IsPanelWithCustomHeaderOpen = true)>Open Panel</BitButton>

<BitPanel HeaderText=""Simple header"" @bind-IsOpen=""IsPanelWithHeaderTextOpen"">
    <ChildContent>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </ChildContent>
</BitPanel>

<BitPanel @bind-IsOpen=""IsPanelWithCustomHeaderOpen"">
    <HeaderTemplate>
        <div>
            <BitSearchBox Placeholder=""Search here..."" />
            <p>
                BitPanel with custom header content
            </p>
        </div>
    </HeaderTemplate>
    <ChildContent>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </ChildContent>
</BitPanel>";
    private readonly string example2CSharpCode = @"
private bool IsPanelWithHeaderTextOpen = false;
private bool IsPanelWithCustomHeaderOpen = false;";

    private readonly string example3HTMLCode = @"
<BitButton OnClick=@(() => IsPanelWithFooterOpen = true)>Open Panel</BitButton>

<BitPanel Title=""BitPanel with custom footer content"" @bind-IsOpen=""IsPanelWithFooterOpen"">
    <ChildContent>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </ChildContent>
    <FooterTemplate>
        <BitButton OnClick=@(() => IsPanelWithFooterOpen = false)>Save</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=@(() => IsPanelWithFooterOpen = false)>Close</BitButton>
    </FooterTemplate>
</BitPanel>";
    private readonly string example3CSharpCode = @"
private bool IsPanelWithFooterOpen = false;";
    
    private readonly string example4HTMLCode = @"
<BitLabel>Panel with IsBlocking = true</BitLabel>
<BitButton OnClick=@(() => IsBlockingPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with IsModeless = true</BitLabel>
<BitButton OnClick=@(() => IsModelessPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with AutoToggleScroll = false</BitLabel>
<BitButton OnClick=@(() => IsAutoToggleScrollPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with ShowCloseButton = false</BitLabel>
<BitButton OnClick=@(() => IsPanelWithoutCloseButtonOpen = true)>Open Panel</BitButton>

<BitPanel HeaderText=""IsBlocking = true"" @bind-IsOpen=""IsBlockingPanelOpen"" IsBlocking=""true"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel HeaderText=""IsModeless = true"" @bind-IsOpen=""IsModelessPanelOpen"" IsModeless=""true"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel HeaderText=""AutoToggleScroll = false"" @bind-IsOpen=""IsAutoToggleScrollPanelOpen"" AutoToggleScroll=""false"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel HeaderText=""ShowCloseButton = false"" @bind-IsOpen=""IsPanelWithoutCloseButtonOpen"" ShowCloseButton=""false"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=@(() => IsPanelWithoutCloseButtonOpen = false)>Close</BitButton>
</BitPanel>";
    private readonly string example4CSharpCode = @"
private bool IsBlockingPanelOpen = false;
private bool IsModelessPanelOpen = false;
private bool IsAutoToggleScrollPanelOpen = false;
private bool IsPanelWithoutCloseButtonOpen = false;";

    private readonly string example5HTMLCode = @"
<BitSpinButton @bind-Value=""CustomPanelSize"" Label=""Custom size"" />

<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Left)"">Left</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Right)"">Right</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Top)"">Top</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Bottom)"">Bottom</BitButton>

<BitPanel @bind-Size=""CustomPanelSize"" Title=""Panel types"" @bind-IsOpen=""IsOpenInPosition"" Position=""position"">
    <p>
        BitPanel with custom position and size. Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </p>
    <BitSpinButton @bind-Value=""CustomPanelSize"" Label=""Custom size"" />
</BitPanel>";
    private readonly string example5CSharpCode = @"
private bool IsOpenInPosition = false;
private double CustomPanelSize = 320;
private BitPanelPosition position;

private void OpenPanelInPosition(BitPanelPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";
}
