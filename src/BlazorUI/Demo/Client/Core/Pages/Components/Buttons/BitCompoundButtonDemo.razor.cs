using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitCompoundButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
            LinkType = LinkType.Link,
            Href = "#button-style-enum",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of the BitCompoundButton.",
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "null",
            Description = "The value of the type attribute of the button rendered by the BitCompoundButton.",
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
            Name = "ClassStyles",
            Type = "BitCompoundButtonClassStyles?",
            LinkType = LinkType.Link,
            Href = "#class-styles",
            DefaultValue = "null",
            Description = "Custom CSS classes/styles for different parts of the BitCompoundButton.",
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
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitCompoundButtonClassStyles",
            Parameters = new()
            {
               new()
               {
                   Name = "Container",
                   Type = "BitClassStylePair?",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
                   Description = "Custom CSS classes/styles for the internal container of the BitCompoundButton."
               },
               new()
               {
                   Name = "Primary",
                   Type = "BitClassStylePair?",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
                   Description = "Custom CSS classes/styles for the primary section of the BitCompoundButton."
               },
               new()
               {
                   Name = "Secondary",
                   Type = "BitClassStylePair?",
                   Href = "#class-style-pair",
                   LinkType = LinkType.Link,
                   Description = "Custom CSS classes/styles for the secondary section of the BitCompoundButton."
               },
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
                   Description = "Custom CSS classes."
               },
               new()
               {
                   Name = "Style",
                   Type = "string?",
                   Description = "Custom CSS styles."
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new()
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
<BitCompoundButton Text=""Primary"" SecondaryText=""This Button is a compound button"" />
<BitCompoundButton Text=""Standard"" SecondaryText=""This Button is a compound button"" ButtonStyle=""BitButtonStyle.Standard"" />
<BitCompoundButton IsEnabled=""false"" Text=""Disabled"" SecondaryText=""This Button is a disabled compound button"" />";

    private readonly string example2HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitCompoundButton Style=""color:darkblue; font-weight:bold"" Text=""Styled"" SecondaryText=""This is styled compound button"" />
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Class=""custom-class"" Text=""Classed"" SecondaryText=""This is classed compound button"" />";

    private readonly string example3HTMLCode = @"
Visible Button: [ <BitCompoundButton Visibility=""BitComponentVisibility.Visible"" Text=""Visible"" SecondaryText=""This Button is a visible compound button""></BitCompoundButton> ]
Hidden Button: [ <BitCompoundButton Text=""Hidden"" SecondaryText=""This Button is a hidden compound button"" Visibility=""BitComponentVisibility.Hidden""></BitCompoundButton> ]
Collapsed Button: [ <BitCompoundButton Text=""Collapsed"" SecondaryText=""This Button is a collapsed compound button"" Visibility=""BitComponentVisibility.Collapsed""></BitCompoundButton> ]";

    private readonly string example4HTMLCode = @"
<BitCompoundButton ButtonStyle=""BitButtonStyle.Standard"" Text=""AriaDescription"" SecondaryText=""This is a compound button with aria description"" AriaDescription=""Button with Aria Description"" />

<BitCompoundButton Text=""AriaHidden"" SecondaryText=""This is a compound button with aria hidden"" AriaHidden=""true"" />";

    private readonly string example5HTMLCode = @"
<BitCompoundButton Target=""_blank"" Href=""https://github.com/bitfoundation/bitplatform"" Text=""Open the site"" SecondaryText=""Open Bit Platform In New Tab"" />

<BitCompoundButton Href=""https://github.com/bitfoundation/bitplatform"" ButtonStyle=""BitButtonStyle.Standard"" Text=""Open the site"" SecondaryText=""Go To Bit Platform"" />";

    private readonly string example6HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationButtonModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""validationButtonModel.RequiredText"" />
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

    private readonly string example7HTMLCode = @"
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

    private readonly string example8HTMLCode = @"
 <BitCompoundButton SecondaryText=""This is secondary text""
                    ClassStyles=""@(new() {
                                              Container = new() { Style = ""line-height:2"" },
                                              Primary = new() { Class = ""custom-primary"" },
                                              Secondary = new() { Class = ""custom-secondary""}
                                          })"">
     Primary
 </BitCompoundButton>

 <BitCompoundButton SecondaryText=""This is secondary text""
                    ButtonStyle=""BitButtonStyle.Standard""
                    ClassStyles=""@(new() {
                                              Container = new() { Style = ""line-height:2"" },
                                              Primary = new() { Class = ""custom-primary"" },
                                              Secondary = new() { Class = ""custom-secondary""}
                                          })"">
     Standard
 </BitCompoundButton>";
}
