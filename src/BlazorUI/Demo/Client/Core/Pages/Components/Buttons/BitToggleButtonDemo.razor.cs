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
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of toggle button, Possible values: Primary | Standard",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of BitToggleButton.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitToggleButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
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
            Name = "OffIconName",
            Type = "string?",
            Description = "The icon of the BitToggleButton when it is not checked.",
        },
        new()
        {
            Name = "OffText",
            Type = "string?",
            Description = "The text of the BitToggleButton when it is not checked.",
        },
        new()
        {
            Name = "OffTitle",
            Type = "string?",
            Description = "The title of the BitToggleButton when it is not checked.",
        },
        new()
        {
            Name = "OnIconName",
            Type = "string?",
            Description = "The icon of the BitToggleButton when it is checked.",
        },
        new()
        {
            Name = "OnText",
            Type = "string?",
            Description = "The text of the BitToggleButton when it is checked.",
        },
        new()
        {
            Name = "OnTitle",
            Type = "string?",
            Description = "The title of the BitToggleButton when it is checked.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitToggleButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the BitToggleButton.",
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
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon element.",
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon and label container.",
               },
               new()
               {
                   Name = "Text",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text element.",
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
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


    private bool example31Value;
    private bool example32Value;


    private readonly string example1RazorCode = @"
<BitToggleButton OffText=""Primary Unmute"" OnText=""Primary Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton OffText=""Standard Unmute"" OnText=""Standard Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff""
                 ButtonStyle=""BitButtonStyle.Standard"" />

<BitToggleButton IsEnabled=""false"" Text=""Disabled"" IconName=""@BitIconName.MicOff"" />";
    private readonly string example1CsharpCode = @"";

    private readonly string example2RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }

    .custom-container {
        font-size: 12px;
    }

    .custom-icon {
        color: blue;
    }

    .custom-text {
        color: red;
    }
</style>

<BitToggleButton Style=""color:darkblue; font-weight:bold""
                 OffText=""Styled Button : Unmute"" OnText=""Styled Button : Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton Class=""custom-class""
                 ButtonStyle=""BitButtonStyle.Standard""
                 OffText=""Classed Button : Unmute"" OnText=""Classed Button : Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />


<BitToggleButton OffText=""Styled Button : Unmute"" OnText=""Styled Button : Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff""
                 Styles=""@(new() { Container=""font-size:18px"", Icon=""color:red"", Text=""color:blue"" })"" />

<BitToggleButton ButtonStyle=""BitButtonStyle.Standard""
                 OffText=""Classed Button : Unmute"" OnText=""Classed Button : Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff""
                 Classes=""@(new() { Container=""custom-container"", Icon=""custom-icon"", Text=""custom-text"" })"" />";
    private readonly string example2CsharpCode = @"";

    private readonly string example3RazorCode = @"
Visible: [ <BitToggleButton Visibility=""BitVisibility.Visible"">Visible toggle button</BitToggleButton> ]
Hidden: [ <BitToggleButton Visibility=""BitVisibility.Hidden"">Hidden toggle button</BitToggleButton> ]
Collapsed: [ <BitToggleButton Visibility=""BitVisibility.Collapsed"">Collapsed toggle button</BitToggleButton> ]";

    private readonly string example4RazorCode = @"
<BitToggleButton DefaultIsChecked=""true""
                 OffText=""Unmute"" OnText=""Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />

<BitToggleButton @bind-IsChecked=""example31Value""
                 Text=""@(example31Value ? ""Mute"" : ""Unmute"")""
                 IconName=""@(example31Value ? BitIconName.MicOff : BitIconName.Microphone)"" />
<BitCheckbox Label=""Checked Toggle Button"" @bind-Value=""example31Value"" />

<BitToggleButton OnChange=""v => example32Value = v""
                 OffText=""Unmute"" OnText=""Mute""
                 OffIconName=""@BitIconName.Microphone"" OnIconName=""@BitIconName.MicOff"" />
<BitLabel>Check status is: @example32Value</BitLabel>";
    private readonly string example4CsharpCode = @"
private bool example31Value;
private bool example32Value;";
}
