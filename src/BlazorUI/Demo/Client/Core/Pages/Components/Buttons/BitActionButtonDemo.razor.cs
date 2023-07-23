﻿using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitActionButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the action button can have focus in disabled mode.",
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
            Description = "The content of action button, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "URL the link points to, if provided, action button renders as an anchor.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the icon shown in the action button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the action button clicked.",
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
            Description = "The title to show when the mouse is placed on the action button.",
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
<BitActionButton IconName=""@BitIconName.AddFriend"">Create account</BitActionButton>

<BitActionButton AllowDisabledFocus=""false"" IsEnabled=""false"" IconName=""@BitIconName.UserRemove"">Remove user</BitActionButton>";

    private readonly string example2HTMLCode = @"
<style>
    .custom-action-button {
        color: #111;
        width: 14.5rem;
        font-size: 1rem;
        margin-top: 0.625rem;
        border-color: #D7D7D7;
        background-color: #CCC;
        justify-content: center;
    }
</style>

<BitActionButton IconName=""@BitIconName.ThisPC"" Style=""font-size:16px;border:2px solid #32385B;justify-content: center;width: 232px;"">
    Styled Action Button
</BitActionButton>

<BitActionButton IconName=""@BitIconName.ThisPC"" Class=""custom-action-button"">
    Classed Action Button
</BitActionButton>";

    private readonly string example3HTMLCode = @"
    <BitActionButton IconName=""@BitIconName.AddEvent"" Visibility=""BitComponentVisibility.Visible"">Add Event</BitActionButton>

    <div><span>Hidden Button: </span>[<BitActionButton Visibility=""BitComponentVisibility.Hidden"">Hidden Action Button</BitActionButton>]</div>

    <div><span>Collapsed Button: </span>[<BitActionButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Action Button</BitActionButton>]</div>";

    private readonly string example4HTMLCode = @"
<BitActionButton IconName=""@BitIconName.Library"" AriaDescription=""Detailed description used for screen reader."">
    Action Button with Aria Description
</BitActionButton>

<BitActionButton IconName=""@BitIconName.Library"" AriaHidden=""true"">
    Action Button with Aria Hidden
</BitActionButton>";

    private readonly string example5HTMLCode = @"
<BitActionButton IconName=""@BitIconName.Website"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"">
    Open Bit Platform In New Tab
</BitActionButton>

<BitActionButton IconName=""@BitIconName.Website"" Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Platform
</BitActionButton>

<BitActionButton IconName=""@BitIconName.Website"" Target=""_self"" Href=""https://github.com/bitfoundation/bitplatform"" IsEnabled=""false"">
    <span>Bit Platform From Span</span>
</BitActionButton>";

    private readonly string example6HTMLCode = @"
<BitLabel>Small size</BitLabel>
<BitActionButton ButtonSize=""BitButtonSize.Small"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>

<BitLabel>Medium size</BitLabel>
<BitActionButton ButtonSize=""BitButtonSize.Medium"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>

<BitLabel>Large size</BitLabel>
<BitActionButton ButtonSize=""BitButtonSize.Large"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>";

    private readonly string example7HTMLCode = @"
<style>
    .custom-btn-sm.bit-acb-sm {
        padding: 4px 8px;
        font-size: 8px;
        line-height: 1.5;
        border-radius: 3px;
    }
    
    .custom-btn-md.bit-acb-md {
        padding: 12px 24px;
        font-size: 16px;
        line-height: 1.4;
        border-radius: 4px;
    }
    
    .custom-btn-lg.bit-acb-lg {
        padding: 20px 32px;
        font-size: 32px;
        line-height: 1.33;
        border-radius: 6px;
    }
</style>

<BitLabel>Small size</BitLabel>
<BitActionButton Class=""custom-btn-sm"" ButtonSize=""BitButtonSize.Small"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>
    
<BitLabel>Medium size</BitLabel>
<BitActionButton Class=""custom-btn-md"" ButtonSize=""BitButtonSize.Medium"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>

<BitLabel>Large size</BitLabel>
<BitActionButton Class=""custom-btn-lg"" ButtonSize=""BitButtonSize.Large"" IconName=""@BitIconName.AddFriend"">Button</BitActionButton>";
}
