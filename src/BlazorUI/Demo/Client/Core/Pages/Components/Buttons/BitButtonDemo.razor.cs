using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the button for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element.",
        },
        new()
        {
            Name = "ButtonSize",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "BitButtonSize.Medium",
            Description = "The size of button, Possible values: Small | Medium | Large.",
        },
        new()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of button, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL the link points to, if provided, button renders as an anchor.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the button clicked.",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "If Href provided, specifies how to open the link.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the button.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-size-enum",
            Name = "BitButtonSize",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Small",
                    Description="The button size is small.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The button size is medium.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The button size is large.",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        }
    };



    private readonly string example1HTMLCode = @"
<BitButton OnClick=""() => PrimaryCounter++"">Primary (@PrimaryCounter)</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => StandardCounter++"">
    Standard (@StandardCounter)
</BitButton>

<BitButton IsEnabled=""false"" AllowDisabledFocus=""false"">Disabled</BitButton>";
    private readonly string example1CSharpCode = @"
private int primaryCounter;
private int standardCounter;";

    private readonly string example2HTMLCode = @"
<style>
    .custom-button {
        color: white;
        height: 2.5rem;
        font-size: 1rem;
        width: 10.375rem;
        border-radius: 1.25rem;
        border-color: orangered;
        background-color: orangered;
    }

    .custom-button:hover {
        border-color: darkorange;
        background-color: darkorange;
    }

    .custom-button:active {
        border-color: orange;
        background-color: orange;
    }
</style>

<BitButton Style=""border-radius: 1.25rem; font-weight: bold"">
    Styled Button
</BitButton>

<BitButton Class=""custom-button"">
    Classed Button
</BitButton>";

    private readonly string example3HTMLCode = @"
<div>Visible Button: [ <BitButton Visibility=""BitComponentVisibility.Visible"">Visible Button</BitButton> ]</div>

<div>Hidden Button: [ <BitButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitButton> ]</div>

<div>Collapsed Button: [ <BitButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitButton> ]</div>";

    private readonly string example4HTMLCode = @"
<BitButton AriaDescription=""Detailed description used for screen reader."">
    Button with Aria Description
</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" AriaHidden=""true"">
    Button with Aria Hidden
</BitButton>";

    private readonly string example5HTMLCode = @"
<BitButton Title=""Primary"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">
    Open Bit Platform In New Tab
</BitButton>

<BitButton Title=""Standard"" Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Platform
</BitButton>";

    private readonly string example6HTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitButton ButtonSize=""BitButtonSize.Small"">Button</BitButton>

<BitLabel>Medium size</BitLabel>
<BitButton ButtonSize=""BitButtonSize.Medium"">Button</BitButton>

<BitLabel>Large size</BitLabel>
<BitButton ButtonSize=""BitButtonSize.Large"">Button</BitButton>";

    private readonly string example7HTMLCode = @"
<style>
    .custom-btn-ctn {
        gap: 0.5rem;
        display: flex;
    }
</style>

<BitButton Class=""custom-btn-ctn"" IsEnabled=""true"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <BitLabel>A Text from BitLabel</BitLabel>
    <BitRippleLoading Size=""30""/>
</BitButton>";
}
