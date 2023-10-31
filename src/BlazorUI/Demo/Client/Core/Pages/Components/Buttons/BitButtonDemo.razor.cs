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
            Description = "The style of the button, Possible values: Primary | Standard | Text.",
        },
        new()
        {
            Name = "ButtonColor",
            Type = "BitButtonColor",
            LinkType = LinkType.Link,
            Href = "#button-color-enum",
            DefaultValue = "null",
            Description = "The color of the button.",
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
                    Description="The button for primary actions that are high-emphasis.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The button for important actions that are medium-emphasis.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="The button for less-pronounced actions.",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "button-color-enum",
            Name = "BitButtonColor",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Info",
                    Description="Info styled Button.",
                    Value="0",
                },
                new()
                {
                    Name= "Success",
                    Description="Success styled Button.",
                    Value="1",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning styled Button.",
                    Value="2",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="Severe Warning styled Button.",
                    Value="3",
                },
                new()
                {
                    Name= "Error",
                    Description="Error styled Button.",
                    Value="4",
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



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => primaryCounter++"">Primary (@primaryCounter)</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => standardCounter++"">
    Standard (@standardCounter)
</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Text"" OnClick=""() => textCounter++"">
    Text (@textCounter)
</BitButton>

<BitButton IsEnabled=""false"">Disabled</BitButton>";
    private readonly string example1CsharpCode = @"
private int primaryCounter;
private int standardCounter;
private int textCounter;";
    
    private readonly string example2RazorCode = @"
<BitButton ButtonColor=""BitButtonColor.Info"">Info</BitButton>
<BitButton ButtonColor=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"">Info</BitButton>
<BitButton ButtonColor=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"">Info</BitButton>

<BitButton ButtonColor=""BitButtonColor.Success"">Success</BitButton>
<BitButton ButtonColor=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"">Success</BitButton>
<BitButton ButtonColor=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"">Success</BitButton>

<BitButton ButtonColor=""BitButtonColor.Warning"">Warning</BitButton>
<BitButton ButtonColor=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"">Warning</BitButton>
<BitButton ButtonColor=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"">Warning</BitButton>

<BitButton ButtonColor=""BitButtonColor.SevereWarning"">SevereWarning</BitButton>
<BitButton ButtonColor=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"">SevereWarning</BitButton>
<BitButton ButtonColor=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"">SevereWarning</BitButton>

<BitButton ButtonColor=""BitButtonColor.Error"">Error</BitButton>
<BitButton ButtonColor=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"">Error</BitButton>
<BitButton ButtonColor=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"">Error</BitButton>";
    
    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitButton Style=""color:darkblue; font-weight:bold"">Styled Button</BitButton>

<BitButton Class=""custom-class"" ButtonStyle=""BitButtonStyle.Standard"">Classed Button</BitButton>";

    private readonly string example4RazorCode = @"
Visible: [ <BitButton Visibility=""BitVisibility.Visible"">Visible Button</BitButton> ]

Hidden: [ <BitButton Visibility=""BitVisibility.Hidden"">Hidden Button</BitButton> ]

Collapsed: [ <BitButton Visibility=""BitVisibility.Collapsed"">Collapsed Button</BitButton> ]";

    private readonly string example5RazorCode = @"
<BitButton AriaDescription=""Detailed description used for screen reader."">
    Button with AriaDescription
</BitButton>

<BitButton ButtonStyle=""BitButtonStyle.Standard"" AriaHidden=""true"">
    Button with AriaHidden
</BitButton>";

    private readonly string example6RazorCode = @"
<BitButton Title=""Primary"" Target=""_blank"" Href=""https://bitplatform.dev"">
    Open bit platform In New Tab
</BitButton>

<BitButton Title=""Standard"" Href=""https://bitplatform.dev"" ButtonStyle=""BitButtonStyle.Standard"">
    Go To bit platform
</BitButton>";

    private readonly string example7RazorCode = @"
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

    private readonly string example8RazorCode = @"
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
        The form submitted successfully.
    </BitMessageBar>
}";
    private readonly string example8CsharpCode = @"
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
