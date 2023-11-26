﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

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
            Name = "Classes",
            Type = "BitButtonClassStyles?",
            LinkType = LinkType.Link,
            Href = "#button-class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitButton.",
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
            Name = "Icon",
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
            DefaultValue = "BitLabelPosition.Right",
            Description = "The position of the loading Label in regards to the spinner animation.",
            LinkType = LinkType.Link,
            Href = "#spinner-position-enum"
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
            Name = "Size",
            Type = "BitButtonSize",
            LinkType = LinkType.Link,
            Href = "#button-size-enum",
            DefaultValue = "null",
            Description = "The size of button, Possible values: Small | Medium | Large.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitButtonClassStyles?",
            LinkType = LinkType.Link,
            Href = "#class-styles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitButton.",
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
        },
        new()
        {
            Id = "button-icon-enum",
            Name = "BitButtonIconPosition",
            Description = "",
            Items = new()
            {
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
            }
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "button-class-styles",
            Title = "BitButtonClassStyles",
            Parameters = new()
            {
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
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitButton>Primary</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"">Standard</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"">Text</BitButton>";

    private readonly string example2RazorCode = @"
<BitButton>Primary</BitButton>
<BitButton IsEnabled=""false"">Disabled</BitButton>
<BitButton Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example3RazorCode = @"
<BitButton ButtonStyle=""BitButtonStyle.Standard"">Standard</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" IsEnabled=""false"">Disabled</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Standard"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example4RazorCode = @"
<BitButton ButtonStyle=""BitButtonStyle.Text"">Text</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"" IsEnabled=""false"">Disabled</BitButton>
<BitButton ButtonStyle=""BitButtonStyle.Text"" Href=""https://bitplatform.dev"">Link</BitButton>";

    private readonly string example5RazorCode = @"
<BitButton Icon=""@BitIconName.Emoji"">
    Default (Start)
</BitButton>

<BitButton Icon=""@BitIconName.Emoji2""
           ButtonStyle=""BitButtonStyle.Standard""
           IconPosition=""BitButtonIconPosition.End"">
    End
</BitButton>";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>";
    private readonly string example6CsharpCode = @"
private int clickCounter;";
    
    private readonly string example7RazorCode = @"
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
    
    private readonly string example8RazorCode = @"
<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Primary"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Primary"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Primary"">Large</BitButton>

<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Standard"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Standard"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Standard"">Large</BitButton>

<BitButton Size=""BitButtonSize.Small"" ButtonStyle=""BitButtonStyle.Text"">Small</BitButton>
<BitButton Size=""BitButtonSize.Medium"" ButtonStyle=""BitButtonStyle.Text"">Medium</BitButton>
<BitButton Size=""BitButtonSize.Large"" ButtonStyle=""BitButtonStyle.Text"">Large</BitButton>";

    private readonly string example9RazorCode = @"
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

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
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

    private readonly string example11RazorCode = @"
<BitButton IsLoading=""primaryIsLoading""
           Style=""min-width: 6rem;""
           ButtonStyle=""BitButtonStyle.Primary""
           OnClick=""LoadingPrimaryClick"">
    Primary
</BitButton>

<BitButton IsLoading=""standardIsLoading""
           Style=""min-width: 6rem;""
           ButtonStyle=""BitButtonStyle.Standard""
           OnClick=""LoadingStandardClick"">
    Standard
</BitButton>

<BitButton IsLoading=""textIsLoading""
           Style=""min-width: 6rem;""
           ButtonStyle=""BitButtonStyle.Text""
           OnClick=""LoadingTextClick"">
    Text
</BitButton>";
    private readonly string example11CsharpCode = @"
private bool primaryIsLoading;
private bool standardIsLoading;
private bool textIsLoading;

private async Task LoadingPrimaryClick()
{
    primaryIsLoading = true;
    await Task.Delay(3000);
    primaryIsLoading = false;
}

private async Task LoadingStandardClick()
{
    standardIsLoading = true;
    await Task.Delay(3000);
    standardIsLoading = false;
}

private async Task LoadingTextClick()
{
    textIsLoading = true;
    await Task.Delay(3000);
    textIsLoading = false;
}";

    private readonly string example12RazorCode = @"
<BitButton IsLoading=""true""
           LoadingLabel=""Right...""
           Style=""min-width: 6.5rem;""
           ButtonStyle=""BitButtonStyle.Standard""
           LoadingLabelPosition=""BitLabelPosition.Right"">
    Right
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Left...""
           Style=""min-width: 6.5rem;""
           LoadingLabelPosition=""BitLabelPosition.Left""
           ButtonStyle=""BitButtonStyle.Standard"">
    Left
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Bottom...""
           Style=""min-width: 6.5rem;""
           ButtonStyle=""BitButtonStyle.Standard""
           LoadingLabelPosition=""BitLabelPosition.Bottom"">
    Bottom
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Top...""
           Style=""min-width: 6.5rem;""
           ButtonStyle=""BitButtonStyle.Standard""
           LoadingLabelPosition=""BitLabelPosition.Top"">
    Top
</BitButton>";

    private readonly string example13RazorCode = @"
<style>
    .custom-loading {
        display: flex;
        gap: 0.3125rem;
        align-items: center;
        justify-content: center;
    }
</style>


<BitButton IsLoading=""true""
           Size=""BitButtonSize.Large""
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

    private readonly string example14RazorCode = @"
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

<BitButton Class=""custom-class"" ButtonStyle=""BitButtonStyle.Standard"">
    Classed Button
</BitButton>


<BitButton IsLoading=""stylesIsLoading""
           LoadingLabel=""Wait...""
           OnClick=""LoadingStylesClick""
           Styles=""@(new() { Root = ""border-radius: 1rem; min-width: 6rem;"",
                             LoadingLabel = ""color: tomato;"",
                             Spinner = ""border-color: goldenrod; border-top-color: tomato;"" })"">
    Primary
</BitButton>

<BitButton IsLoading=""classesIsLoading""
           LoadingLabel=""Sending...""
           OnClick=""LoadingClassesClick"" 
           ButtonStyle=""BitButtonStyle.Standard""
           Classes=""@(new() { Root = ""custom-root"",
                              LoadingContainer = ""custom-container"",
                              LoadingLabel = ""custom-label"",
                              Spinner = ""custom-spinner"" })"">
    Standard
</BitButton>";
    private readonly string example14CsharpCode = @"
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
}
