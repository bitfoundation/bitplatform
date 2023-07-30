using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the button can have focus in disabled mode.",
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
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
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
            Description = "The content of button, It can be Any custom tag or a text.",
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
            Description = "The title to show when the mouse is placed on the button.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new List<ComponentEnumItem>()
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
<BitButton OnClick=""() => primaryCounter++"">Primary (@primaryCounter)</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => standardCounter++"">
    Standard (@standardCounter)
</BitButton>

<BitButton IsEnabled=""false"">Disabled</BitButton>";
    private readonly string example1CSharpCode = @"
private int primaryCounter;
private int standardCounter;";

    private readonly string example2HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitButton Style=""color:darkblue; font-weight:bold"">Styled Button</BitButton>

<BitButton Class=""custom-class"" ButtonStyle=""BitButtonStyle.Standard"">Classed Button</BitButton>";

    private readonly string example3HTMLCode = @"
Visible Button: [ <BitButton Visibility=""BitComponentVisibility.Visible"">Visible Button</BitButton> ]

Hidden Button: [ <BitButton Visibility=""BitComponentVisibility.Hidden"">Hidden Button</BitButton> ]

Collapsed Button: [ <BitButton Visibility=""BitComponentVisibility.Collapsed"">Collapsed Button</BitButton> ]";

    private readonly string example4HTMLCode = @"
<BitButton AriaDescription=""Detailed description used for screen reader."">
    Button with Aria Description
</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" AriaHidden=""true"">
    Button with Aria Hidden
</BitButton>";

    private readonly string example5HTMLCode = @"
<BitButton Title=""Primary"" Target=""_blank"" Href=""https://bitplatform.dev"">
    Open Bit Platform In New Tab
</BitButton>

<BitButton Title=""Standard"" Href=""https://bitplatform.dev"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To Bit Platform
</BitButton>";

    private readonly string example6HTMLCode = @"
<style>
    .custom-button {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>

<BitButton Class=""custom-button"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <span>A custom text</span>
    <BitRippleLoading Size=""30""/>
</BitButton>

<BitButton Class=""custom-button"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitIcon IconName=""@BitIconName.Accept"" />
    <span>A Standard custom text</span>
    <BitRollerLoading Size=""30"" />
</BitButton>";

    private readonly string example7HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""buttonValidationModel.RequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.RequiredText"" />

        <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.NonRequiredText"" />

        <div>
            <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
            <BitButton ButtonType=""BitButtonType.Reset"">Reset</BitButton>
            <BitButton ButtonType=""BitButtonType.Button"">Button</BitButton>
        </div>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form is valid to submit successfully.
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
