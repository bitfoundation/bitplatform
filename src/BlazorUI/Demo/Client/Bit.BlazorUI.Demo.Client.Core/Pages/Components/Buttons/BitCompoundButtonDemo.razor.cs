namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitCompoundButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the BitCompoundButton can have focus in disabled mode.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the BitCompoundButton for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, adds an aria-hidden attribute instructing screen readers to ignore the element.",
        },
        new()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of the BitCompoundButton.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType?",
            DefaultValue = "null",
            Description = "The value of the type attribute of the button rendered by the BitCompoundButton.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of primary section of the BitCompoundButton.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitCompoundButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCompoundButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Href",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the href attribute of the link rendered by the BitCompoundButton. If provided, the component will be rendered as an anchor.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon to show inside the BitCompoundButton.",
        },
        new()
        {
            Name = "IconPosition",
            Type = "BitButtonIconPosition",
            DefaultValue = "BitButtonIconPosition.Start",
            Description = "Specifies Icon position which can be rendered either on start or end of the component.",
            LinkType = LinkType.Link,
            Href = "#button-icon-enum"
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "The callback for the click event of the BitCompoundButton.",
        },
        new()
        {
            Name = "PrimaryTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of primary section of the BitCompoundButton (alias of the ChildContent).",
        },
        new()
        {
            Name = "SecondaryText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the secondary section of the BitCompoundButton.",
        },
        new()
        {
            Name = "SecondaryTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The RenderFragment for the secondary section of the BitCompoundButton.",
        },
        new()
        {
            Name = "Severity",
            Type = "BitSeverity?",
            DefaultValue = "null",
            Description = "The severity of the compound button.",
            LinkType = LinkType.Link,
            Href = "#severity-enum",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of button, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCompoundButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCompoundButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Target",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies target attribute of the link when the BitComponentButton renders as an anchor.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the button.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCompoundButtonClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitCompoundButton."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitCompoundButton."
               },
               new()
               {
                   Name = "TextContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the text (Primary and Secoundary) container of the BitCompoundButton."
               },
               new()
               {
                   Name = "Primary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the primary section of the BitCompoundButton."
               },
               new()
               {
                   Name = "Secondary",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the secondary section of the BitCompoundButton."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "severity-enum",
            Name = "BitSeverity",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "button-size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "button-icon-enum",
            Name = "BitButtonIconPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Start",
                    Description="Renders the icon at the start of component.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="Renders the icon at the end of component.",
                    Value="1",
                }
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitCompoundButton SecondaryText=""This is the secondary text"">Primary</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" SecondaryText=""This is the secondary text"">Standard</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Text"" SecondaryText=""This is the secondary text"">Text</BitCompoundButton>";

    private readonly string example2RazorCode = @"
<BitCompoundButton SecondaryText=""This is the secondary text"">Primary</BitCompoundButton>
<BitCompoundButton SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example3RazorCode = @"
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" SecondaryText=""This is the secondary text"">Standard</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example4RazorCode = @"
<BitCompoundButton ButtonStyle=""BitButtonStyle.Text"" SecondaryText=""This is the secondary text"">Text</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Text"" SecondaryText=""This is the secondary text"" IsEnabled=""false"">Disabled</BitCompoundButton>
<BitCompoundButton ButtonStyle=""BitButtonStyle.Text"" SecondaryText=""This is the secondary text"" Href=""https://bitplatform.dev"">Link</BitCompoundButton>";

    private readonly string example5RazorCode = @"
<BitCompoundButton IconName=""@BitIconName.Emoji"" SecondaryText=""IconPosition Start"">
    Default (Start)
</BitCompoundButton>

<BitCompoundButton IconName=""@BitIconName.Emoji2""
                   IconPosition=""BitButtonIconPosition.End""
                   SecondaryText=""IconPosition End""
                   ButtonStyle=""BitButtonStyle.Standard"">
    End
</BitCompoundButton>";

    private readonly string example6RazorCode = @"
<BitCompoundButton OnClick=""() => clickCounter++"" SecondaryText=""@($""Click count is: {@clickCounter}"")"">Click me</BitCompoundButton>";
    private readonly string example6CsharpCode = @"
private int clickCounter;";

    private readonly string example7RazorCode = @"
<EditForm Model=""validationButtonModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />

    <BitTextField Label=""Required"" Required @bind-Value=""validationButtonModel.RequiredText"" />
    <ValidationMessage For=""() => validationButtonModel.RequiredText"" />
    <br />
    <BitTextField Label=""Non Required"" @bind-Value=""validationButtonModel.NonRequiredText"" />
    <ValidationMessage For=""() => validationButtonModel.NonRequiredText"" />
    <br />
    <div class=""buttons-container"">
        <BitCompoundButton ButtonType=BitButtonType.Submit Text=""Submit"" SecondaryText=""This is a Submit button"" />
        <BitCompoundButton ButtonType=BitButtonType.Reset Text=""Reset"" SecondaryText=""This is a Reset button"" />
        <BitCompoundButton ButtonType=BitButtonType.Button Text=""Button"" SecondaryText=""This is just a button"" />
    </div>
</EditForm>";
    private readonly string example7CsharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(2000);

    buttonValidationModel = new();

    StateHasChanged();
}";

    private readonly string example8RazorCode = @"
<BitCompoundButton>
    <PrimaryTemplate>
        <span style=""color:red"">Primary Template!</span>
    </PrimaryTemplate>
    <SecondaryTemplate>
        <BitIcon IconName=""@BitIconName.AirplaneSolid"" />
        <span style=""color:blueviolet"">Secondary Template goes here!</span>
    </SecondaryTemplate>
</BitCompoundButton>

<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"">
    <PrimaryTemplate>
        <span style=""color:darkcyan"">Primary Template!</span>
    </PrimaryTemplate>
    <SecondaryTemplate>
        <span style=""color:blueviolet"">Secondary Template goes here!</span>
        <BitGridLoading Size=""20"" Color=""@($""var({BitCss.Var.Color.Foreground.Primary})"")"" />
    </SecondaryTemplate>
</BitCompoundButton>";

    private readonly string example9RazorCode = @"
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"">Info</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Info</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Info"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Info</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"">Success</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Success</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Success"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Success</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"">Warning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Warning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Warning"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Warning</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"">SevereWarning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">SevereWarning</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.SevereWarning"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">SevereWarning</BitCompoundButton>

<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"">Error</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Error</BitCompoundButton>
<BitCompoundButton Severity=""BitSeverity.Error"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Error</BitCompoundButton>";

    private readonly string example10RazorCode = @"
<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Primary"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Primary"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Primary"">Large</BitCompoundButton>

<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Standard"">Large</BitCompoundButton>

<BitCompoundButton Size=""BitSize.Small"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Small</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Medium"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Medium</BitCompoundButton>
<BitCompoundButton Size=""BitSize.Large"" SecondaryText=""This is the secondary text"" ButtonStyle=""BitButtonStyle.Text"">Large</BitCompoundButton>";

    private readonly string example11RazorCode = @"
<style>
    .custom-container {
        line-height: 2;
    }

    .custom-primary {
        line-height: 2;
        font-weight: 900;
        color: goldenrod;
    }

    .custom-secondary {
        line-height: 2;
        font-weight: 600;
        color: orangered;
    }
</style>


<BitCompoundButton SecondaryText=""This is secondary text""
                   Styles=""@(new() { TextContainer = ""line-height: 2;"",
                                     Primary = ""color: darkmagenta;"",
                                     Secondary = ""color: darkslateblue;"" })"">
    Primary
</BitCompoundButton>

<BitCompoundButton SecondaryText=""This is secondary text""
                   ButtonStyle=""BitButtonStyle.Standard""
                   Classes=""@(new() { TextContainer = ""custom-container"",
                                      Primary = ""custom-primary"",
                                      Secondary = ""custom-secondary"" })"">
    Standard
</BitCompoundButton>";

    private readonly string example12RazorCode = @"
Visible: [ <BitCompoundButton Visibility=""BitVisibility.Visible"" Text=""Visible"" SecondaryText=""This Button is a visible compound button""></BitCompoundButton> ]
Hidden: [ <BitCompoundButton Text=""Hidden"" SecondaryText=""This Button is a hidden compound button"" Visibility=""BitVisibility.Hidden""></BitCompoundButton> ]
Collapsed: [ <BitCompoundButton Text=""Collapsed"" SecondaryText=""This Button is a collapsed compound button"" Visibility=""BitVisibility.Collapsed""></BitCompoundButton> ]";

    private readonly string example13RazorCode = @"
<BitCompoundButton Dir=""BitDir.Rtl"" 
                   IconName=""@BitIconName.Emoji"" 
                   SecondaryText=""محل قرار گیری نماد در ابتدا"">
    پیش فرض (ابتدا)
</BitCompoundButton>

<BitCompoundButton Dir=""BitDir.Rtl""
                   IconName=""@BitIconName.Emoji2""
                   ButtonStyle=""BitButtonStyle.Standard""
                   IconPosition=""BitButtonIconPosition.End""
                   SecondaryText=""محل قرار گیری نماد در انتها"">
    انتها
</BitCompoundButton>";
}
