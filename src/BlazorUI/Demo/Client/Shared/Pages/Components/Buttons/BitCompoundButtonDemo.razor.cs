using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Buttons;

public partial class BitCompoundButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the compound button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the compound button for the benefit of screen readers.",
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
            Description = "The style of compound button, Possible values: Primary | Standard",
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
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to, if provided, compound button renders as an anchor.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the compound button clicked.",
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string",
            DefaultValue = "",
            Description = "Description of the action compound button takes.",
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
            Name = "Text",
            Type = "string",
            DefaultValue = "",
            Description = "The text of compound button.",
        },
        new()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the compound button.",
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
            Name = "BitButtonStyle",
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
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items = new()
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
<BitCompoundButton IsEnabled=""true""
                    Text=""Primary""
                    SecondaryText=""This Button is a compound button"">
</BitCompoundButton>

<BitCompoundButton IsEnabled=""true""
                    Text=""Standard""
                    AriaHidden=""true""
                    SecondaryText=""This Button is a compound button""
                    ButtonStyle=""BitButtonStyle.Standard"">
</BitCompoundButton>

<BitCompoundButton IsEnabled=""false""
                    AllowDisabledFocus=""false""
                    Text=""Disabled""
                    Class=""disable-cmp-btn""
                    AriaDescription=""Detailed description used for screen reader.""
                    SecondaryText=""This Button is a disabled compound button"">
</BitCompoundButton>";

    private readonly string example2HTMLCode = @"
<style>
    .custom-compound-button {
        width: 15rem;
        font-size: 1rem;
        color: #FFFFFF;
        background-color: #0054C6;
        border-color: #0054C6;
    }

    .custom-compound-button:hover {
        background-color: #0065EF;
        border-color: #0065EF;
    }
</style>

<BitCompoundButton Style=""height: 80px;font-size: 16px;text-decoration: underline;"" Text=""Styled"" SecondaryText=""This is styled compound button""></BitCompoundButton>

<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Class=""custom-compound-button"" Text=""Classed"" SecondaryText=""This is classed compound button""></BitCompoundButton>";

    private readonly string example3HTMLCode = @"
<div><BitCompoundButton Visibility=""BitComponentVisibility.Visible"" Text=""Visible"" SecondaryText=""This Button is a visible compound button""></BitCompoundButton></div>

<div><span>Hidden Button: </span>[<BitCompoundButton Text=""Hidden"" SecondaryText=""This Button is a hidden compound button"" Visibility=""BitComponentVisibility.Hidden""></BitCompoundButton>]</div>

<div><span>Collapsed Button: </span>[<BitCompoundButton Text=""Collapsed"" SecondaryText=""This Button is a collapsed compound button"" Visibility=""BitComponentVisibility.Collapsed""></BitCompoundButton>]</div>";

    private readonly string example4HTMLCode = @"
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Text=""AriaDescription"" SecondaryText=""This is a compound button with aria description"" AriaDescription=""Button with Aria Description""></BitCompoundButton>

<BitCompoundButton Text=""AriaHidden"" SecondaryText=""This is a compound button with aria hidden"" AriaHidden=""true""></BitCompoundButton>";

    private readonly string example5HTMLCode = @"
<BitCompoundButton Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"" Text=""Open the site"" SecondaryText=""Open Bit Platform In New Tab""></BitCompoundButton>

<BitCompoundButton Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"" Text=""Open the site"" SecondaryText=""Go To Bit Platform""></BitCompoundButton>

<BitCompoundButton Target=""_self"" Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"" Text=""Open the site"" SecondaryText=""Go To Bit Platform""></BitCompoundButton>";

    private readonly string example6HTMLCode = @"
<div>
    <BitLabel>Small size</BitLabel>
    <BitCompoundButton ButtonSize=""BitButtonSize.Small""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>

<div>
    <BitLabel>Medium size</BitLabel>
    <BitCompoundButton ButtonSize=""BitButtonSize.Medium""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>

<div>
    <BitLabel>Large size</BitLabel>
    <BitCompoundButton ButtonSize=""BitButtonSize.Large""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>";

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

<div>
    <BitLabel>Small size</BitLabel>
    <BitCompoundButton Class=""custom-btn-sm"" ButtonSize=""BitButtonSize.Small""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>
<div>
    <BitLabel>Medium size</BitLabel>
    <BitCompoundButton Class=""custom-btn-md"" ButtonSize=""BitButtonSize.Medium""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>
<div>
    <BitLabel>Large size</BitLabel>
    <BitCompoundButton Class=""custom-btn-lg"" ButtonSize=""BitButtonSize.Large""
                        Text=""Primary""
                        SecondaryText=""This Button is a compound button"">
    </BitCompoundButton>
</div>";
}
