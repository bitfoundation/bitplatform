using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitIconButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the icon button can have focus in disabled mode.",
        },
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the icon button for the benefit of screen readers.",
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
            Description = "URL the link points to, if provided, icon button renders as an anchor.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "The icon name for the icon shown in the icon button.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the icon button clicked.",
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
            Description = "The title to show when the mouse is placed on the icon button.",
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
<BitIconButton IconName=""BitIconName.Emoji"" />
<BitIconButton IconName=""BitIconName.EmojiDisappointed"" AllowDisabledFocus=""false"" IsEnabled=""false"" />";

    private readonly string example2HTMLCode = @"
<style>
    .custom-icon-button {
        height: 3rem;
        width: 3rem;
        border-radius: 0.375rem;
        background-color: #D7D7D7;
        border-color: #D7D7D7;
    }

    .custom-icon-button:hover {
        background-color: #E9E9E9;
        border-color: #E9E9E9;
    }
</style>

<BitIconButton IconName=""BitIconName.Home"" Style=""border-radius: 5px;padding: 23px;border: #D7D7D7 solid 2px;"" />
<BitIconButton IconName=""BitIconName.FileImage"" Class=""custom-icon-button"" />";

    private readonly string example3HTMLCode = @"
<style>
    .buttons-container-grid {
        display: grid;
        gap: 0.5rem;
    }
</style>

<div class=""buttons-container-grid"">
    <BitIconButton IconName=""BitIconName.List"" Visibility=""BitComponentVisibility.Visible"">Visible Button</BitIconButton>
    <div><span>Hidden Button: </span>[<BitIconButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitIconButton>]</div>
    <div><span>Collapsed Button: </span>[<BitIconButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitIconButton>]</div>
</div>";

    private readonly string example4HTMLCode = @"
<BitIconButton IconName=""BitIconName.Library"" AriaDescription=""Detailed description used for screen reader."">
    Button with Aria Description
</BitIconButton>
<BitIconButton IconName=""BitIconName.Library"" AriaHidden=""true"">
    Button with Aria Hidden
</BitIconButton>";

    private readonly string example5HTMLCode = @"
<BitIconButton IconName=""BitIconName.Website"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">
    Open Bit Platform In New Tab
</BitIconButton>
<BitIconButton IconName=""BitIconName.Website"" Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Platform
</BitIconButton>
<BitIconButton IconName=""BitIconName.Website"" Target=""_self"" Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"">
    <span>Bit Platform From Span</span>
</BitIconButton>";

    private readonly string example6HTMLCode = @"
<BitIconButton IconName=""BitIconName.Emoji2"" Title=""I'm Happy"" />";

    private readonly string example7HTMLCode = @"
<div class=""buttons-container-grid"">
    <div>
        <BitLabel>Small size</BitLabel>
        <BitIconButton ButtonSize=""BitButtonSize.Small"" IconName=""BitIconName.Emoji"" />

    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitIconButton ButtonSize=""BitButtonSize.Medium"" IconName=""BitIconName.Emoji"" />

    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitIconButton ButtonSize=""BitButtonSize.Large"" IconName=""BitIconName.Emoji"" />
    </div>
</div>";
    private readonly string example8HTMLCode = @"
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
        <BitIconButton Class=""custom-btn-sm""
                       ButtonSize=""BitButtonSize.Small"" IconName=""BitIconName.Emoji"" />

    </div>
    <div>
        <BitLabel>Medium size</BitLabel>
        <BitIconButton Class=""custom-btn-md""
                       ButtonSize=""BitButtonSize.Medium"" IconName=""BitIconName.Emoji"" />

    </div>
    <div>
        <BitLabel>Large size</BitLabel>
        <BitIconButton Class=""custom-btn-lg""
                       ButtonSize=""BitButtonSize.Large"" IconName=""BitIconName.Emoji"" />

    </div>
</div>";
}
