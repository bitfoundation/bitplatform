namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "ButtonType",
            Type = "BitButtonType?",
            DefaultValue = "null",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
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
            Name = "Classes",
            Type = "BitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitButton.",
            LinkType = LinkType.Link,
            Href = "#button-class-styles",
        },
        new()
        {
            Name = "Content",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias of ChildContent",
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
            Name = "IconName",
            Type = "string",
            DefaultValue = "null",
            Description = "The icon to show inside the BitButton."
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
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determine whether the button is in loading mode or not."
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The loading label to show next to the spinner."
        },
        new()
        {
            Name = "LoadingLabelPosition",
            Type = "BitLabelPosition",
            DefaultValue = "BitLabelPosition.End",
            Description = "The position of the loading Label in regards to the spinner animation.",
            LinkType = LinkType.Link,
            Href = "#button-labelPosition-enum"
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the content inside the Button in the Loading state.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the button clicked.",
        },
        new()
        {
            Name = "Severity",
            Type = "BitSeverity?",
            DefaultValue = "null",
            Description = "The severity of the button.",
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
            Type = "BitButtonClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitButton.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
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
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the button.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "button-class-styles",
            Title = "BitButtonClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitButton."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitButton."
               },
               new()
               {
                   Name = "LoadingContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the internal container of the BitButton."
               },
               new()
               {
                   Name = "Spinner",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the spinner section of the BitButton."
               },
               new()
               {
                   Name = "LoadingLabel",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label section of the BitButton."
               },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new()
                {
                    Name= "Fill",
                    Description="Fill styled variant.",
                    Value="0",
                },
                new()
                {
                    Name= "Outline",
                    Description="Outline styled variant.",
                    Value="1",
                },
                new()
                {
                    Name= "Text",
                    Description="Text styled variant.",
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
        },
        new()
        {
            Id = "button-labelPosition-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the button.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows on the end of the button.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the button.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows on the start of the button.",
                    Value="3",
                },
            ]
        }
    ];



    private readonly string example1RazorCode = @"
<BitButton>Button</BitButton>";

    private readonly string example2RazorCode = @"
<BitButton Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"">Text</BitButton>";

    private readonly string example3RazorCode = @"
<BitButton>Fill</BitButton>
<BitButton IsEnabled=""false"">Disabled</BitButton>
<BitButton Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example4RazorCode = @"
<BitButton Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Disabled</BitButton>
<BitButton Variant=""BitVariant.Outline"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example5RazorCode = @"
<BitButton Variant=""BitVariant.Text"">Text</BitButton>
<BitButton Variant=""BitVariant.Text"" IsEnabled=""false"">Disabled</BitButton>
<BitButton Variant=""BitVariant.Text"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example6RazorCode = @"
<BitButton IconName=""@BitIconName.Emoji"">
    Default (Start)
</BitButton>

<BitButton IconName=""@BitIconName.Emoji2""
           Variant=""BitVariant.Outline""
           IconPosition=""BitButtonIconPosition.End"">
    End
</BitButton>";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>";
    private readonly string example7CsharpCode = @"
private int clickCounter;";

    private readonly string example8RazorCode = @"
<BitButton Severity=""BitSeverity.Info"">Info</BitButton>
<BitButton Severity=""BitSeverity.Info"" Variant=""BitVariant.Outline"">Info</BitButton>
<BitButton Severity=""BitSeverity.Info"" Variant=""BitVariant.Text"">Info</BitButton>

<BitButton Severity=""BitSeverity.Success"">Success</BitButton>
<BitButton Severity=""BitSeverity.Success"" Variant=""BitVariant.Outline"">Success</BitButton>
<BitButton Severity=""BitSeverity.Success"" Variant=""BitVariant.Text"">Success</BitButton>

<BitButton Severity=""BitSeverity.Warning"">Warning</BitButton>
<BitButton Severity=""BitSeverity.Warning"" Variant=""BitVariant.Outline"">Warning</BitButton>
<BitButton Severity=""BitSeverity.Warning"" Variant=""BitVariant.Text"">Warning</BitButton>

<BitButton Severity=""BitSeverity.SevereWarning"">SevereWarning</BitButton>
<BitButton Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning</BitButton>
<BitButton Severity=""BitSeverity.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning</BitButton>

<BitButton Severity=""BitSeverity.Error"">Error</BitButton>
<BitButton Severity=""BitSeverity.Error"" Variant=""BitVariant.Outline"">Error</BitButton>
<BitButton Severity=""BitSeverity.Error"" Variant=""BitVariant.Text"">Error</BitButton>";

    private readonly string example9RazorCode = @"
<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Fill"">Small</BitButton>
<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Fill"">Medium</BitButton>
<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Fill"">Large</BitButton>

<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Outline"">Small</BitButton>
<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Outline"">Medium</BitButton>
<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Outline"">Large</BitButton>

<BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"">Small</BitButton>
<BitButton Size=""BitSize.Medium"" Variant=""BitVariant.Text"">Medium</BitButton>
<BitButton Size=""BitSize.Large"" Variant=""BitVariant.Text"">Large</BitButton>";

    private readonly string example10RazorCode = @"
<style>
    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" />
    <span>A Fill custom content</span>
    <BitRippleLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Accept"" />
    <span>An Outline custom content</span>
    <BitRollerLoading Size=""20"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Asterisk"" />
    <span>A Text custom content</span>
    <BitCircleLoading Size=""20"" />
</BitButton>";

    private readonly string example11RazorCode = @"
<EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitTextField Label=""Required"" Required @bind-Value=""buttonValidationModel.RequiredText"" />
    <ValidationMessage For=""() => buttonValidationModel.RequiredText"" />
    <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
    <ValidationMessage For=""() => buttonValidationModel.NonRequiredText"" />
    <div>
        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
        <BitButton ButtonType=""BitButtonType.Reset"">Reset</BitButton>
        <BitButton ButtonType=""BitButtonType.Button"">Button</BitButton>
    </div>
</EditForm>";
    private readonly string example11CsharpCode = @"
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

    private readonly string example12RazorCode = @"
<BitButton IsLoading=""fillIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Fill""
           OnClick=""LoadingFillClick"">
    Fill
</BitButton>

<BitButton IsLoading=""outlineIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Outline""
           OnClick=""LoadingOutlineClick"">
    Outline
</BitButton>

<BitButton IsLoading=""textIsLoading""
           Style=""min-width: 6rem;""
           Variant=""BitVariant.Text""
           OnClick=""LoadingTextClick"">
    Text
</BitButton>";
    private readonly string example12CsharpCode = @"
private bool fillIsLoading;
private bool outlineIsLoading;
private bool textIsLoading;

private async Task LoadingFillClick()
{
    fillIsLoading = true;
    await Task.Delay(3000);
    fillIsLoading = false;
}

private async Task LoadingOutlineClick()
{
    outlineIsLoading = true;
    await Task.Delay(3000);
    outlineIsLoading = false;
}

private async Task LoadingTextClick()
{
    textIsLoading = true;
    await Task.Delay(3000);
    textIsLoading = false;
}";

    private readonly string example13RazorCode = @"
<BitButton IsLoading=""true""
           LoadingLabel=""End...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.End"">
    End
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Start...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Start"">
    Start
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Bottom...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Bottom"">
    Bottom
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Top...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Top"">
    Top
</BitButton>";

    private readonly string example14RazorCode = @"
<style>
    .custom-loading {
        display: flex;
        gap: 0.3125rem;
        align-items: center;
        justify-content: center;
    }
</style>


<BitButton IsLoading=""true""
           Size=""BitSize.Large""
           Title=""Ellipsis loading..."">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitEllipsisLoading Size=""20"" />
            <span>Wait...</span>
        </div>
    </LoadingTemplate>
    <Content>
        Ellipsis...
    </Content>
</BitButton>";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        color: blueviolet;
        border-radius: 1rem;
    }

    .custom-root {
        min-width: 7.2rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-container {
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-label {
        color: goldenrod;
    }

    .custom-spinner {
        border-color: aqua;
        border-top-color: goldenrod;
    }
</style>


<BitButton Style=""color:darkblue; font-weight:bold"">
    Styled Button
</BitButton>

<BitButton Class=""custom-class"" Variant=""BitVariant.Outline"">
    Classed Button
</BitButton>


<BitButton IsLoading=""stylesIsLoading""
           LoadingLabel=""Wait...""
           OnClick=""LoadingStylesClick""
           Styles=""@(new() { Root = ""border-radius: 1rem; min-width: 6rem;"",
                             LoadingLabel = ""color: tomato;"",
                             Spinner = ""border-color: goldenrod; border-top-color: tomato;"" })"">
    Fill
</BitButton>

<BitButton IsLoading=""classesIsLoading""
           LoadingLabel=""Sending...""
           OnClick=""LoadingClassesClick"" 
           Variant=""BitVariant.Outline""
           Classes=""@(new() { Root = ""custom-root"",
                              LoadingContainer = ""custom-container"",
                              LoadingLabel = ""custom-label"",
                              Spinner = ""custom-spinner"" })"">
    Outline
</BitButton>";
    private readonly string example15CsharpCode = @"
private bool stylesIsLoading;
private bool classesIsLoading;

private async Task LoadingStylesClick()
{
    stylesIsLoading = true;
    await Task.Delay(3000);
    stylesIsLoading = false;
}

private async Task LoadingClassesClick()
{
    classesIsLoading = true;
    await Task.Delay(3000);
    classesIsLoading = false;
}";

    private readonly string example16RazorCode = @"
<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Fill"">
    دکمه با نماد
</BitButton>

<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Outline"">
    دکمه با نماد
</BitButton>

<BitButton Dir=""BitDir.Rtl""
           IconName=""@BitIconName.Emoji""
           Variant=""BitVariant.Text"">
    دکمه با نماد
</BitButton>


<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""دکمه ی بارگذاری""
           Variant=""BitVariant.Fill"" />

<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""دکمه ی بارگذاری""
           Variant=""BitVariant.Outline"" />

<BitButton IsLoading
           Dir=""BitDir.Rtl""
           LoadingLabel=""دکمه ی بارگذاری""
           Variant=""BitVariant.Text"" />
";
}
