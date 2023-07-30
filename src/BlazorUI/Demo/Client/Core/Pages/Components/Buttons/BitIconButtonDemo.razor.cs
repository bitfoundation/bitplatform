﻿using Bit.BlazorUI.Demo.Client.Core.Models;
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
            DefaultValue = "true",
            Description = "Whether the icon button can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
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
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "null",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "URL the link points to, if provided, icon button renders as an anchor.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the icon shown in the icon button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the icon button clicked.",
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
<BitIconButton IconName=""@BitIconName.Emoji"" />
<BitIconButton IconName=""@BitIconName.EmojiDisappointed"" IsEnabled=""false"" />";

    private readonly string example2HTMLCode = @"
<style>
    .custom-icon-button {
        width: 3rem;
        height: 3rem;
        border-color: #D7D7D7;
        background-color: #CCC;
        border-radius: 0.375rem;
    }

    .custom-icon-button:hover {
        border-color: #E9E9E9;
        background-color: #E9E9E9;
    }
</style>

<BitIconButton IconName=""@BitIconName.Home"" Style=""border-radius: 5px;padding: 23px;border: #D7D7D7 solid 2px;"" />
<BitIconButton IconName=""@BitIconName.FileImage"" Class=""custom-icon-button"" />";

    private readonly string example3HTMLCode = @"
Visible Button: [ <BitIconButton IconName=""@BitIconName.List"" Visibility=""BitComponentVisibility.Visible"" /> ]
Hidden Button: <BitIconButton Visibility=""BitComponentVisibility.Hidden"" /> ]
Collapsed Button: [<BitIconButton Visibility=""BitComponentVisibility.Collapsed"" /> ]";

    private readonly string example4HTMLCode = @"
<BitIconButton IconName=""@BitIconName.Library"" AriaDescription=""Detailed description used for screen reader."" Title=""Button with Aria Description"" />
<BitIconButton IconName=""@BitIconName.Library"" AriaHidden=""true"" Title=""Button with Aria Hidden"" />";

    private readonly string example5HTMLCode = @"
<BitIconButton IconName=""@BitIconName.Website"" Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"" Title=""Open Bit Platform In New Tab"" />
<BitIconButton IconName=""@BitIconName.Website"" Href=""https://github.com/bitfoundation/bitplatform"" Title=""Go To Bit Platform"" />";

    private readonly string example6HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""buttonValidationModel.RequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.RequiredText"" />

        <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.NonRequiredText"" />

        <BitIconButton IconName=""@BitIconName.SendMirrored"" ButtonType=""BitButtonType.Submit"" Title=""Submit"" />
        <BitIconButton IconName=""@BitIconName.Reset"" ButtonType=""BitButtonType.Reset"" Title=""Reset"" />
        <BitIconButton IconName=""@BitIconName.ButtonControl"" ButtonType=""BitButtonType.Button"" Title=""Button"" />
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form submitted successfully.
    </BitMessageBar>
}";
    private readonly string example6CSharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private bool formIsValidSubmit;
private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(2000);

    buttonValidationModel = new();

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}";
}
