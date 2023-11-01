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
            Name = "Color",
            Type = "BitButtonColor",
            LinkType = LinkType.Link,
            Href = "#button-color-enum",
            DefaultValue = "null",
            Description = "The color of the button.",
        },
        new()
        {
            Name = "Size",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "null",
            Description = "The size of button, Possible values: Small | Medium | Large.",
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
            Id = "button-size-enum",
            Name = "BitButtonSize",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Small",
                    Description="The small size button.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size button.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size button.",
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



    private readonly string bacisExampleRazorCode = @"
<BitButton>Primary</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"">Standard</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"">Text</BitButton>";

    private readonly string primaryButtonExampleRazorCode = @"
<BitButton>Primary</BitButton>
<BitButton IsEnabled=""false"">Disabled</BitButton>
<BitButton Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string standardButtonExampleRazorCode = @"
<BitButton ButtonStyle=""BitButtonStyle.Standard"">Standard</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" IsEnabled=""false"">Disabled</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string textButtonExampleRazorCode = @"
<BitButton ButtonStyle=""BitButtonStyle.Text"">Text</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"" IsEnabled=""false"">Disabled</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string handleClicksExampleRazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>";

    private readonly string handleClicksExampleCsharpCode = @"
private int clickCounter;";
    
    private readonly string example2RazorCode = @"
<BitButton Color=""BitButtonColor.Info"">Info</BitButton>
<BitButton Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Standard"">Info</BitButton>
<BitButton Color=""BitButtonColor.Info"" ButtonStyle=""BitButtonStyle.Text"">Info</BitButton>

<BitButton Color=""BitButtonColor.Success"">Success</BitButton>
<BitButton Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Standard"">Success</BitButton>
<BitButton Color=""BitButtonColor.Success"" ButtonStyle=""BitButtonStyle.Text"">Success</BitButton>

<BitButton Color=""BitButtonColor.Warning"">Warning</BitButton>
<BitButton Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Standard"">Warning</BitButton>
<BitButton Color=""BitButtonColor.Warning"" ButtonStyle=""BitButtonStyle.Text"">Warning</BitButton>

<BitButton Color=""BitButtonColor.SevereWarning"">SevereWarning</BitButton>
<BitButton Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Standard"">SevereWarning</BitButton>
<BitButton Color=""BitButtonColor.SevereWarning"" ButtonStyle=""BitButtonStyle.Text"">SevereWarning</BitButton>

<BitButton Color=""BitButtonColor.Error"">Error</BitButton>
<BitButton Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Standard"">Error</BitButton>
<BitButton Color=""BitButtonColor.Error"" ButtonStyle=""BitButtonStyle.Text"">Error</BitButton>";
    
    private readonly string example3RazorCode = @"
<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Primary"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Primary"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"">Large</BitButton>

<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Standard"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Standard"">Large</BitButton>

<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Text"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Text"">Large</BitButton>";
    
    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitButton Style=""color:darkblue; font-weight:bold"">Styled Button</BitButton>

<BitButton Class=""custom-class"" ButtonStyle=""BitButtonStyle.Standard"">Classed Button</BitButton>

<BitButton Class=""custom-class"" Style=""color:green;"" ButtonStyle=""BitButtonStyle.Text"">
    Styled Classed Button
</BitButton>";

    private readonly string example5RazorCode = @"
Visible: [ <BitButton Visibility=""BitVisibility.Visible"">Visible Button</BitButton> ]

Hidden: [ <BitButton Visibility=""BitVisibility.Hidden"">Hidden Button</BitButton> ]

Collapsed: [ <BitButton Visibility=""BitVisibility.Collapsed"">Collapsed Button</BitButton> ]";

    private readonly string example8RazorCode = @"
<style>
    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>

<BitButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <span>A Primary custom content</span>
    <BitRippleLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitIcon IconName=""@BitIconName.Accept"" />
    <span>A Standard custom content</span>
    <BitRollerLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" ButtonStyle=""BitButtonStyle.Text"">
    <BitIcon IconName=""@BitIconName.Asterisk"" />
    <span>A Text custom content</span>
    <BitCircleLoading Size=""20"" />
</BitButton>";

    private readonly string example9RazorCode = @"
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
    private readonly string example9CsharpCode = @"
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
