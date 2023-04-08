using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Buttons;

public partial class BitButtonDemo
{

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
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
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "The content of button, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to, if provided, button renders as an anchor.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the button clicked.",
        },
        new()
        {
            Name = "Target",
            Type = "string",
            DefaultValue = "",
            Description = "If Href provided, specifies how to open the link.",
        },
        new()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the button.",
        }
    };

    private readonly List<ComponentSubEnum> enumParameters = new()
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



    public int PrimaryCounter;
    public int StandardCounter;


    private readonly string example1HTMLCode = @"
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container"">
    <BitButton IsEnabled=""true"" OnClick=""() => PrimaryCounter++"">
        Primary (@PrimaryCounter)
    </BitButton>
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" IsEnabled=""true"" OnClick=""() => StandardCounter++"">
        Standard (@StandardCounter)
    </BitButton>
    <BitButton IsEnabled=""false"" AllowDisabledFocus=""false"">
        Disabled
    </BitButton>
    <BitButton Class=""label-btn"" IsEnabled=""true"">
        <label>A Text from label element</label>
    </BitButton>
</div>";
    private readonly string example1CSharpCode = @"
public int PrimaryCounter;
public int StandardCounter;";

    private readonly string example2HTMLCode = @"
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }

    .custom-button.primary {
        height: 2.5rem;
        width: 10.375rem;
        font-size: 1rem;
        background-color: #0054C6;
        border-color: #0054C6;
    }

    .custom-button.primary:hover {
        background-color: #01367e;
        border-color: #01367e;
    }
</style>

<div class=""buttons-container"">
    <BitButton Style=""height: 40px;width: 166px;font-size: 16px;"">
        Styled Button
    </BitButton>
    <BitButton Class=""custom-button"">
        Classed Button
    </BitButton>
</div>";

    private readonly string example3HTMLCode = @"
<style>
    .buttons-container-grid {
        display: grid;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container-grid"">
    <div><BitButton Visibility=""BitComponentVisibility.Visible"">Visible Button</BitButton></div>
    <div>Hidden Button: [<BitButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitButton>]</div>
    <div>Collapsed Button: [<BitButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitButton>]</div>
</div>";

    private readonly string example4HTMLCode = @"
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container"">
    <BitButton AriaDescription=""Detailed description used for screen reader."">
        Button with Aria Description
    </BitButton>
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" AriaHidden=""true"">
        Button with Aria Hidden
    </BitButton>
</div>";

    private readonly string example5HTMLCode = @"
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container"">
    <BitButton Title=""Primary"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">
        Open Bit Platform In New Tab
    </BitButton>
    <BitButton Title=""Standard"" Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"">
        Go To Bit Platform
    </BitButton>
    <BitButton Target=""_self"" Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"">
        <span>Bit Platform From Span</span>
    </BitButton>
</div>";

    private readonly string example6HTMLCode = @"
<style>
    .buttons-container-grid {
        display: grid;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitButton ButtonSize=""BitButtonSize.Small"">Button</BitButton>
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitButton ButtonSize=""BitButtonSize.Medium"">Button</BitButton>
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitButton ButtonSize=""BitButtonSize.Large"">Button</BitButton>
    </div>
</div>";

    private readonly string example7HTMLCode = @"
<style>
    .buttons-container-grid {
        display: grid;
        gap: 0.5rem;
    }
    
    .custom-btn-sm.small {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }
    
    .custom-btn-md.medium {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }
    
    .custom-btn-lg.large {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitButton Class=""custom-btn-sm""
                    ButtonSize=""BitButtonSize.Small"">Button</BitButton>
    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitButton Class=""custom-btn-md""
                    ButtonSize=""BitButtonSize.Medium"">Button</BitButton>
    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitButton Class=""custom-btn-lg""
                    ButtonSize=""BitButtonSize.Large"">Button</BitButton>
    </div>
</div>";
}
