using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitToggleButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the toggle button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
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
            Name = "ClassStyles",
            Type = "BitToggleButtonClassStyles?",
            DefaultValue = "null",
            Href = "#class-styles",
            LinkType = LinkType.Link,
            Description = "Custom CSS classes/styles for different parts of the component."
        },
        new()
        {
            Name = "DefaultIsChecked",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value of the IsChecked.",
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
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon that shows in the toggle button.",
        },
        new()
        {
            Name = "IsChecked",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determine if the toggle button is in checked state, default is true.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text that shows in the label.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the IsChecked value has changed.",
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
            Description = "The title to show when the mouse is placed on the toggle button.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitToggleButtonClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Icon",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon element.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "Container",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon and label container.",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link
               },
               new()
               {
                   Name = "Label",
                   Type = "BitClassStylePair?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label element.",
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
                   DefaultValue = "null",
                   Description = "Custom CSS class."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS style."
               }
            }
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



    private readonly string example1HTMLCode = @"
<BitToggleButton @bind-IsChecked=""TogglePrimaryButtonChecked""
                    Label=""@(TogglePrimaryButtonChecked ? ""Primary Mute"":""Primary Unmute"")""
                    IconName=""@(TogglePrimaryButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                    ButtonStyle=""BitButtonStyle.Primary"" />
<BitToggleButton @bind-IsChecked=""ToggleStandardButtonChecked""
                    Label=""@(ToggleStandardButtonChecked ? ""Standard Mute"":""Standard Unmute"")""
                    IconName=""@(ToggleStandardButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                    ButtonStyle=""BitButtonStyle.Standard"" />
<BitToggleButton @bind-IsChecked=""ToggleDisabledButtonChecked""
                    IsEnabled=""false""
                    Label=""@(ToggleDisabledButtonChecked ? ""Disabled Mute"" : ""Disabled Unmute"")""
                    IconName=""@(ToggleDisabledButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)"" />";
    private readonly string example1CSharpCode = @"
private bool TogglePrimaryButtonChecked;
private bool ToggleStandardButtonChecked;
private bool ToggleDisabledButtonChecked;";

    private readonly string example2HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitToggleButton @bind-IsChecked=""ToggleStyleButtonChecked""
                 Style=""color:darkblue; font-weight:bold""
                 Label=""@(ToggleStyleButtonChecked ? ""Styled Button : Mute"" : ""Styled Button : Unmute"")""
                 IconName=""@(ToggleStyleButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)"" />
<BitToggleButton @bind-IsChecked=""ToggleClassButtonChecked""
                 Class=""custom-class""
                 ButtonStyle=""BitButtonStyle.Standard""
                 Label=""@(ToggleClassButtonChecked ? ""Classed Button : Mute"" : ""Classed Button : Unmute"")""
                 IconName=""@(ToggleClassButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)"" />"; 
    private readonly string example2CSharpCode = @"
private bool ToggleStyleButtonChecked;
private bool ToggleClassButtonChecked;";

    private readonly string example3HTMLCode = @"
<BitLabel>Check status is: @OnToggleButtonChanged</BitLabel>

<BitToggleButton @bind-IsChecked=""ToggleButtonForOnChange""
                 Label=""@(ToggleButtonForOnChange ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleButtonForOnChange ? BitIconName.MicOff : BitIconName.Microphone)
                 OnChange=""ToggleButtonChanged"" />";
    private readonly string example3CSharpCode = @"
private bool ToggleButtonForOnChange = true;
private bool OnToggleButtonChanged = true;

private void ToggleButtonChanged(bool newValue)
{
    OnToggleButtonChanged = newValue;
}";

    private readonly string example4HTMLCode = @"
<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""ToggleTwoWayButtonValue"" />

<BitToggleButton @bind-IsChecked=""ToggleTwoWayButtonValue""
                 Label=""@(ToggleTwoWayButtonValue ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleTwoWayButtonValue ? BitIconName.MicOff : BitIconName.Microphone) />";
    private readonly string example4CSharpCode = @"
private bool ToggleTwoWayButtonValue = true;";

    private readonly string example5HTMLCode = @"
<BitToggleButton OnChange=""((e) => ToggleButtonDefaultValue = e)""
                 DefaultIsChecked=""ToggleButtonDefaultValue""
                 Label=""@(ToggleButtonDefaultValue ? ""Mute"" : ""Unmute"")""
                 IconName=@(ToggleButtonDefaultValue ? BitIconName.MicOff : BitIconName.Microphone) />";
    private readonly string example5CSharpCode = @"
private bool ToggleButtonDefaultValue = true;";

    private readonly string example6HTMLCode = @"
<BitToggleButton @bind-IsChecked=""ToggleAriaButtonChecked""
                 Label=""@(ToggleAriaButtonChecked ? ""Aria Description Mute"" : ""Aria Description Unmute"")""
                 IconName=""@(ToggleAriaButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                 AriaDescription=""Detailed description used for screen reader"" />
<BitToggleButton @bind-IsChecked=""ToggleAriaHiddenButtonChecked""
                 ButtonStyle=""BitButtonStyle.Standard""
                 Label=""@(ToggleAriaHiddenButtonChecked ? ""Aria Hidden Mute"" : ""Aria Hidden Unmute"")""
                 IconName=""@(ToggleAriaHiddenButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)""
                 AriaHidden=""true"" />";
    private readonly string example6CSharpCode = @"
private bool ToggleAriaButtonChecked = false;
private bool ToggleAriaHiddenButtonChecked = false;";

    private readonly string example7HTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitToggleButton ButtonSize=BitButtonSize.Small @bind-IsChecked=""ToggleTwoWayButtonSizeValue""
                 Label=""@(ToggleTwoWayButtonSizeValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleTwoWayButtonSizeValue ? BitIconName.MicOff : BitIconName.Microphone)"" />

<BitLabel>Medium size</BitLabel>
<BitToggleButton ButtonSize=BitButtonSize.Medium @bind-IsChecked=""ToggleTwoWayButtonSizeValue""
                 Label=""@(ToggleTwoWayButtonSizeValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleTwoWayButtonSizeValue ? BitIconName.MicOff : BitIconName.Microphone)"" />

<BitLabel>Large size</BitLabel>
<BitToggleButton ButtonSize=BitButtonSize.Large @bind-IsChecked=""ToggleTwoWayButtonSizeValue""
                 Label=""@(ToggleTwoWayButtonSizeValue ? ""Mute"" : ""Unmute"")""
                 IconName=""@(ToggleTwoWayButtonSizeValue ? BitIconName.MicOff : BitIconName.Microphone)"" />";
    private readonly string example7CSharpCode = @"
private bool ToggleTwoWayButtonSizeValue = true;";
}
