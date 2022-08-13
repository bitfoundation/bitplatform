using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitToggleButtonDemo
{
    private bool TogglePrimaryButtonChecked;
    private bool ToggleStandardButtonChecked;
    private bool ToggleDisabledButtonChecked;
    private bool ToggleButtonChecked;

    private bool ToggleButtonForOnChange = true;
    private bool OnToggleButtonChanged = true;

    private bool ToggleButtonTwoWayValue = true;

    private void ToggleButtonChanged(bool newValue)
    {
        OnToggleButtonChanged = newValue;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the toggle button can have focus in disabled mode.",
        },
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the toggle button for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element.",
        },
        new ComponentParameter()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of toggle button, Possible values: Primary | Standard",
        },
        new ComponentParameter()
        {
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to, if provided, button renders as an anchor.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "The icon that shows in the toggle button.",
        },
        new ComponentParameter()
        {
            Name = "IsChecked",
            Type = "bool",
            DefaultValue = "",
            Description = "Determine if the toggle button is in checked state, default is true.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The text that shows in the label.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Callback that is called when the IsChecked value has changed.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the button clicked.",
        },
        new ComponentParameter()
        {
            Name = "Target",
            Type = "string",
            DefaultValue = "",
            Description = "If Href provided, specifies how to open the link.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the toggle button.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "button-style-enum",
            Title = "ButtonStyle enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

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
                 Label=""@(ToggleDisabledButtonChecked ? ""Disabled Mute"" : ""Disabled Unmute"")""
                 IconName=@(ToggleDisabledButtonChecked ? BitIconName.MicOff : BitIconName.Microphone)>
</BitToggleButton>";

    private readonly string example1CSharpCode = @"
private bool ToggleStandardButtonChecked;
private bool ToggleDisabledButtonChecked;
private bool TogglePrimaryButtonChecked;
";

    private readonly string example2HTMLCode = @"
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
<input type=checkbox id=""BitToggleButtonTwoWayValue"" @bind=""ToggleButtonTwoWayValue"">
<label class=""checkbox-status"" for=""BitToggleButtonTwoWayValue"">Checked Toggle Button</label>
<div>
    <BitToggleButton @bind-IsChecked=""ToggleButtonTwoWayValue""
                     Label=""@(ToggleButtonTwoWayValue ? ""Mute"" : ""Unmute"")""
                     IconName=@(ToggleButtonTwoWayValue ? BitIconName.MicOff : BitIconName.Microphone)>
    </BitToggleButton>
</div>";

    private readonly string example3CSharpCode = @"
private bool ToggleButtonTwoWayValue = true;";

    private readonly string example4HTMLCode = @"
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

    private readonly string example4CSharpCode = @"
private bool ToggleButtonChecked = false;";
}
