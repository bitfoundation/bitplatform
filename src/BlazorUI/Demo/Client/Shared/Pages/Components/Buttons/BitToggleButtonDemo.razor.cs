using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Buttons;

public partial class BitToggleButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the toggle button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the toggle button for the benefit of screen readers.",
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
            Description = "The style of toggle button, Possible values: Primary | Standard",
        },
        new()
        {
            Name = "DefaultIsChecked",
            Type = "bool?",
            DefaultValue = "",
            Description = "Default value of the IsChecked.",
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
            Name = "IconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "The icon that shows in the toggle button.",
        },
        new()
        {
            Name = "IsChecked",
            Type = "bool",
            DefaultValue = "",
            Description = "Determine if the toggle button is in checked state, default is true.",
        },
        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The text that shows in the label.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback that is called when the IsChecked value has changed.",
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
            Description = "The title to show when the mouse is placed on the toggle button.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-size-enum",
            Name = "BitButtonSize",
            Description = "",
            Items = new()
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
            Name = "ButtonStyle",
            Description = "",
            Items = new()
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
        }
    };



    private bool TogglePrimaryButtonChecked;
    private bool ToggleStandardButtonChecked;
    private bool ToggleDisabledButtonChecked;
    private bool ToggleButtonChecked;

    private bool ToggleButtonForOnChange = true;
    private bool OnToggleButtonChanged = true;

    private bool ToggleButtonValue = true;

    private bool ToggleButtonTwoWayValue = true;

    private void ToggleButtonChanged(bool newValue)
    {
        OnToggleButtonChanged = newValue;
    }


    private readonly string example1HTMLCode = @"
<BitToggleButton @bind-IsChecked=""TogglePrimaryButtonChecked""
                 Class=""tgl-btn""
                 Label=""@(TogglePrimaryButtonChecked ? ""Primary Mute"":""Primary Unmute"")""
                 IconName=@(TogglePrimaryButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)
                 ButtonStyle=""BitButtonStyle.Primary"">
</BitToggleButton>
<BitToggleButton @bind-IsChecked=""ToggleStandardButtonChecked""
                 Class=""tgl-btn""
                 Label=""@(ToggleStandardButtonChecked ? ""Standard Mute"":""Standard Unmute"")""
                 IconName=@(ToggleStandardButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)
                 ButtonStyle=""BitButtonStyle.Standard"">
</BitToggleButton>
<BitToggleButton @bind-IsChecked=""ToggleDisabledButtonChecked""
                 Class=""tgl-btn""
                 IsEnabled=""false""
                 Label=""@(ToggleDisabledButtonChecked ? ""Primary disabled Mute"" : ""Primary disabled Unmute"")""
                 IconName=@(ToggleDisabledButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)>
</BitToggleButton>
<BitToggleButton @bind-IsChecked=""ToggleDisabledButtonChecked""
                 Class=""tgl-btn""
                 IsEnabled=""false""
                 Label=""@(ToggleDisabledButtonChecked ? ""Standard disabled Mute"" : ""Standard disabled Unmute"")""
                 IconName=@(ToggleDisabledButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)
                 ButtonStyle=""BitButtonStyle.Standard"">
</BitToggleButton>";
    private readonly string example1CSharpCode = @"
private bool ToggleStandardButtonChecked;
private bool ToggleDisabledButtonChecked;
private bool TogglePrimaryButtonChecked;";

    private readonly string example2HTMLCode = @"
<div>Check status is: <strong>@OnToggleButtonChanged</strong></div>

<BitToggleButton @bind-IsChecked=""ToggleButtonForOnChange""
                 Label=""@(ToggleButtonForOnChange ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleButtonForOnChange ? BitIconName.MicOff : BitIconName.Microphone)
                 OnChange=""ToggleButtonChanged"">
</BitToggleButton>";
    private readonly string example2CSharpCode = @"
private bool ToggleButtonForOnChange = true;
private bool OnToggleButtonChanged = true;

private void ToggleButtonChanged(bool newValue)
{
    OnToggleButtonChanged = newValue;
}";

    private readonly string example3HTMLCode = @"
<BitToggleButton @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)>
</BitToggleButton>

<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""ToggleButtonTwoWayValue"" />";
    private readonly string example3CSharpCode = @"
private bool ToggleButtonTwoWayValue = true;";

    private readonly string example4HTMLCode = @"
<BitToggleButton OnChange=""((e) => ToggleButtonDefaultValue = e)""
                 DefaultIsChecked=""ToggleButtonDefaultValue""
                 Label=""@(ToggleButtonDefaultValue ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleButtonDefaultValue ? BitIconName.MicOff : BitIconName.Microphone)>
</BitToggleButton>";
    private readonly string example4CSharpCode = @"
private bool ToggleButtonDefaultValue = true;";

    private readonly string example5HTMLCode = @"
<BitToggleButton @bind-IsChecked=""ToggleButtonChecked""
                 Label=""@(ToggleButtonChecked ? ""Aria Description Mute"" : ""Aria Description Unmute"")""
                 IconName=""@(ToggleButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                 AriaDescription=""Detailed description used for screen reader"">
</BitToggleButton>

<BitToggleButton @bind-IsChecked=""ToggleButtonChecked""
                 Class=""aria-hidden-tgl-btn""
                 Label=""@(ToggleButtonChecked ? ""Aria Hidden Mute"" : ""Aria Hidden Unmute"")""
                 IconName=""@(ToggleButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                 AriaHidden=""true"">
</BitToggleButton>";
    private readonly string example5CSharpCode = @"
private bool ToggleButtonChecked = false;";

    private readonly string example6HTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitToggleButton ButtonSize=""BitButtonSize.Small"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>

<BitLabel>Medium size</BitLabel>
<BitToggleButton ButtonSize=""BitButtonSize.Medium"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>

<BitLabel>Large size</BitLabel>
<BitToggleButton ButtonSize=""BitButtonSize.Large"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>";

    private readonly string example7HTMLCode = @"
<style>
    .custom-btn-sm {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }
    
    .custom-btn-md {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }
    
    .custom-btn-lg {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitToggleButton Class=""custom-btn-sm"" ButtonSize=""BitButtonSize.Small"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>

<BitLabel>Medium size</BitLabel>
<BitToggleButton Class=""custom-btn-md"" ButtonSize=""BitButtonSize.Medium"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>
        
<BitLabel>Large size</BitLabel>
<BitToggleButton Class=""custom-btn-lg"" ButtonSize=""BitButtonSize.Large"" @bind-IsChecked=""ToggleButtonTwoWayValue""
                 Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)"">
</BitToggleButton>";
}
