﻿using System.Collections.Generic;
using Bit.BlazorUI.Demo.Web.Models;
using Bit.BlazorUI.Demo.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Web.Pages.Components.Buttons;

public partial class BitCompoundButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the compound button can have focus in disabled mode.",
        },
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the compound button for the benefit of screen readers.",
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
            Name = "ButtonSize",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "BitButtonSize.Medium",
            Description = "The size of button, Possible values: Small | Medium | Large.",
        },
        new ComponentParameter()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of compound button, Possible values: Primary | Standard",
        },
        new ComponentParameter()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "BitButtonType.Button",
            Description = "The type of the button.",
        },
        new ComponentParameter()
        {
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to, if provided, compound button renders as an anchor.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the compound button clicked.",
        },
        new ComponentParameter()
        {
            Name = "SecondaryText",
            Type = "string",
            DefaultValue = "",
            Description = "Description of the action compound button takes.",
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
            Name = "Text",
            Type = "string",
            DefaultValue = "",
            Description = "The text of compound button.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the compound button.",
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
            Id = "button-size-enum",
            Title = "BitButtonSize Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Small",
                    Description="The button size is small.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Medium",
                    Description="The button size is medium.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Large",
                    Description="The button size is large.",
                    Value="2",
                }
            }
        },
        new EnumParameter()
        {
            Id = "button-style-enum",
            Title = "BitButtonStyle Enum",
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
            Id = "button-type-enum",
            Title = "BitButtonType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
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
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container"">
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
    </BitCompoundButton>
</div>";

    private readonly string example2HTMLCode = @"
<style>
    .buttons-container {
        display: flex;
        flex-flow: row wrap;
        gap: 0.5rem;
    }

    .custom-compound-button.standard {
        width: 15rem;
        font-size: 1rem;
        color: #FFFFFF;
        background-color: #0054C6;
        border-color: #0054C6;
    }

    .custom-compound-button.standard:hover {
        background-color: #0065EF;
        border-color: #0065EF;
    }
</style>

<div class=""buttons-container"">
    <BitCompoundButton Style=""height: 80px;font-size: 16px;text-decoration: underline;"" Text=""Styled"" SecondaryText=""This is styled compound button""></BitCompoundButton>
    <BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Class=""custom-compound-button"" Text=""Classed"" SecondaryText=""This is classed compound button""></BitCompoundButton>
</div>";

    private readonly string example3HTMLCode = @"
<style>
    .buttons-container-grid {
        display: grid;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container-grid"">
    <div><BitCompoundButton Visibility=""BitComponentVisibility.Visible"" Text=""Visible"" SecondaryText=""This Button is a visible compound button""></BitCompoundButton></div>
    <div><span>Hidden Button: </span>[<BitCompoundButton Text=""Hidden"" SecondaryText=""This Button is a hidden compound button"" Visibility=""BitComponentVisibility.Hidden""></BitCompoundButton>]</div>
    <div><span>Collapsed Button: </span>[<BitCompoundButton Text=""Collapsed"" SecondaryText=""This Button is a collapsed compound button"" Visibility=""BitComponentVisibility.Collapsed""></BitCompoundButton>]</div>
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
    <BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Text=""AriaDescription"" SecondaryText=""This is a compound button with aria description"" AriaDescription=""Button with Aria Description""></BitCompoundButton>
    <BitCompoundButton Text=""AriaHidden"" SecondaryText=""This is a compound button with aria hidden"" AriaHidden=""true""></BitCompoundButton>
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
    <BitCompoundButton Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"" Text=""Open the site"" SecondaryText=""Open Bit Platform In New Tab""></BitCompoundButton>
    <BitCompoundButton Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"" Text=""Open the site"" SecondaryText=""Go To Bit Platform""></BitCompoundButton>
    <BitCompoundButton Target=""_self"" Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"" Text=""Open the site"" SecondaryText=""Go To Bit Platform""></BitCompoundButton>
</div>";

    private readonly string example6HTMLCode = @"
<div class=""buttons-container-grid"">
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
    </div>
</div>
";
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
    </div>
</div>";
}
