using Bit.BlazorUI.Demo.Client.Core.Models;
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
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "null",
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

<BitActionButton AllowDisabledFocus=""false"" IsEnabled=""false"" IconName=""@BitIconName.UserRemove"">Remove user (Disabled)</BitActionButton>";

    private readonly string example2HTMLCode = @"
<style>
    .custom-action-button {
        color: #111;
        font-size: 1rem;
        border-radius: 1rem;
        border-color: #D7D7D7;
        background-color: #CCC;
    }
</style>

<BitActionButton IconName=""@BitIconName.ThisPC"" Style=""font-size:16px; border:2px solid #32385B; border-radius: 1rem;"">
    Styled Action Button
</BitActionButton>

<BitActionButton IconName=""@BitIconName.ThisPC"" Class=""custom-action-button"">
    Classed Action Button
</BitActionButton>";

    private readonly string example3HTMLCode = @"
Visible Button: [ <BitActionButton IconName=""@BitIconName.AddEvent"" Visibility=""BitComponentVisibility.Visible"">Visible Action Button</BitActionButton> ]

Hidden Button: [ <BitActionButton Visibility=""BitComponentVisibility.Hidden"">Hidden Action Button</BitActionButton> ]

Collapsed Button: [ <BitActionButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Action Button</BitActionButton> ]";

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
</BitActionButton>";

    private readonly string example6HTMLCode = @"
<style>
    .custom-btn-ctn {
        gap: 0.5rem;
        display: flex;
    }
</style>

<BitActionButton IconName=""@BitIconName.AddFriend"">
    <div class=""custom-btn-ctn"">
        <BitLabel>A Text from BitLabel</BitLabel>
        <BitSpinnerLoading Size=""30"" />
    </div>
</BitActionButton>";

    private readonly string example7HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationButtonModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <ValidationSummary />

        <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""validationButtonModel.RequiredText"" />
        <ValidationMessage For=""() => validationButtonModel.RequiredText"" />

        <BitTextField Label=""Nonrequired"" @bind-Value=""validationButtonModel.NonRequiredText"" />
        <ValidationMessage For=""() => validationButtonModel.NonRequiredText"" />

        <div>
            <BitActionButton IconName=""@BitIconName.SendMirrored"" ButtonType=""BitButtonType.Submit"">Submit</BitActionButton>
            <BitActionButton IconName=""@BitIconName.Reset""  ButtonType=""BitButtonType.Reset"">Reset</BitActionButton>
            <BitActionButton IconName=""@BitIconName.ButtonControl"" ButtonType=""BitButtonType.Button"">Button</BitActionButton>
        </div>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form submitted successfully.
    </BitMessageBar>
}";
    private readonly string example7CSharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private bool formIsValidSubmit;
private ButtonValidationModel validationButtonModel = new();

private async Task HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(2000);

    validationButtonModel = new();

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}";

}
