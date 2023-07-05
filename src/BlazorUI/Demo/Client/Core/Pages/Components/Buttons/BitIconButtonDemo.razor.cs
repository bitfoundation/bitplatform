using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitIconButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the icon button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string",
            DefaultValue = "",
            Description = "Detailed description of the icon button for the benefit of screen readers.",
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
            Name = "ClassStyles",
            Type = "BitIconButtonClassStyles",
            DefaultValue = "",
            Href = "class-styles",
            LinkType = LinkType.Link,
            Description = "Custom CSS classes/styles for different parts of the component."
        },
        new()
        {
            Name = "Href",
            Type = "string",
            DefaultValue = "",
            Description = "URL the link points to, if provided, icon button renders as an anchor.",
        },
        new()
        {
            Name = "IconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "The icon name for the icon shown in the icon button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the icon button clicked.",
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
            Description = "The title to show when the mouse is placed on the icon button.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitIconButtonClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Container",
                   Type = "BitClassStylePair?",
                   Description = "Custom CSS classes/styles for the modal container.",
                   Href = "class-style-pair",
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
                   Description = "Custom CSS class."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
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
