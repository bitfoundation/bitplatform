namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public partial class BitToggleDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Classes",
            Type = "BittoggleClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#toggle-class-styles",
            Description = "Custom CSS classes for different parts of the BitToggle.",
        },
        new()
        {
            Name = "DefaultText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default text of the toggle when it is neither ON or OFF.",
        },
        new()
        {
            Name = "IsInlineLabel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the label (not the onText/offText) should be positioned inline with the toggle control. Left (right in RTL) side when on/off text provided VS right (left in RTL) side when there is no on/off text.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label of the toggle.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom label of the toggle.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the checked value has changed.",
        },
        new()
        {
            Name = "OffText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text to display when toggle is OFF.",
        },
        new()
        {
            Name = "OnText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text to display when toggle is ON.",
        },
        new()
        {
            Name = "Role",
            Type = "string",
            DefaultValue = "switch",
            Description = "Denotes role of the toggle, default is switch.",
        },
        new()
        {
            Name = "Styles",
            Type = "BittoggleClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#toggle-class-styles",
            Description = "Custom CSS styles for different parts of the BitToggle.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "toggle-class-styles",
            Title = "BitToggleClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the toggle."
                },
                new()
                {
                    Name = "Checked",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the checked state of the toggle."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the toggle."
                },
                new()
                {
                    Name = "Thumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the thumb of the toggle."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the toggle."
                },
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toggle's root element."
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text of the toggle."
                }
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitToggle Label=""Basic"" @bind-Value=""BasicValue"" />

<BitToggle Label=""Disabled"" @bind-Value=""DisabledValue"" IsEnabled=""false"" />";
    private readonly string example1CsharpCode = @"
private bool BasicValue;
private bool DisabledValue;";

    private readonly string example2RazorCode = @"
<BitToggle Label=""OnText And OffText"" @bind-Value=""OnTextValue"" OnText=""On"" OffText=""Off"" />

<BitToggle Label=""Inline Label"" @bind-Value=""InLineLabelValue"" IsInlineLabel=""true"" />

<BitToggle @bind-Value=""DefaultTextValue"" DefaultText=""Default Text"" />";
    private readonly string example2CsharpCode = @"
private bool OnTextValue;
private bool InLineLabelValue;
private bool DefaultTextValue;";

    private readonly string example3RazorCode = @"
<BitToggle @bind-Value=""LabelTemplateValue"">
    <LabelTemplate>
        <BitLabel Style=""color: green;"">This is custom Label</BitLabel>
        <BitIcon IconName=""@BitIconName.Filter"" />
    </LabelTemplate>
</BitToggle>";
    private readonly string example3CsharpCode = @"
private bool LabelTemplateValue;";

    private readonly string example4RazorCode = @"
<BitToggle Value=""OneWayValue"" Label=""One-way"" OnText=""On"" OffText=""Off"" />
<BitToggleButton @bind-IsChecked=""OneWayValue"" OnText=""On"" OffText=""Off"" />

<BitToggle @bind-Value=""TwoWayValue"" Label=""Two-way"" OnText=""On"" OffText=""Off"" />
<BitToggleButton @bind-IsChecked=""TwoWayValue"" OnText=""On"" OffText=""Off"" />";
    private readonly string example4CsharpCode = @"
private bool OneWayValue;
private bool TwoWayValue;";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        width: max-content;
        margin-left: 0.5rem;
        border: 1px solid red;
        box-shadow: aqua 0 0 1rem;
    }

    .custom-text {
        color: lightskyblue;
        text-shadow: 0 0 0.5rem red;
    }

    .custom-button {
        border-radius: 1rem;
        background-color: darkslategray;
    }

    .custom-check .custom-button {
        border-radius: 1rem;
        background-color: red;
    }

    .custom-check .custom-text {
        color: rebeccapurple;
    }
</style>

<BitToggle @bind-Value=""@StyleValue""
           DefaultText=""Custom style""
           Style=""background-color: forestgreen; border-radius: 1rem; padding: 0.5rem;"" />
<BitToggle @bind-Value=""@ClassValue""
           DefaultText=""Custom class""
           Class=""custom-class"" />

<BitToggle @bind-Value=""@StylesValue""
           Label=""Custom label style""
           Styles=""@(new() { Root = ""background-color: pink; padding: 0.5rem;"",
                             Thumb = ""background-color: darkorange;"",
                             Button = ""border-radius: 0.5rem 0 0.5rem 0;"",
                             Label = ""color: blue; font-weight: 900; font-size: 1.25rem;"" } )"" />
<BitToggle @bind-Value=""@ClassesValue""
           DefaultText=""Custom text class""
           Classes=""@(new() { Text = ""custom-text"",
                              Button = ""custom-button"",
                              Check = ""custom-check"" } )"" />
";
    private readonly string example5CsharpCode = @"
private bool StyleValue;
private bool ClassValue;
private bool StylesValue;
private bool ClassesValue;";

    private readonly string example6RazorCode = @"
Visible: [ <BitToggle @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Visible"" DefaultText=""Visible Toggle"" /> ]
Hidden: [ <BitToggle @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Hidden"" DefaultText=""Hidden Toggle"" />  ]
Collapsed: [ <BitToggle @bind-Value=""@VisibilityValue"" Visibility=""BitVisibility.Collapsed"" DefaultText=""Collapsed Toggle"" />  ]";
    private readonly string example6CsharpCode = @"
private bool VisibilityValue;";

    private readonly string example7RazorCode = @"
<style>
    .validation-summary {
        border-left: rem(5px) solid $Red10;
        background-color: $ErrorBlockRed;
        overflow: hidden;
        margin-bottom: rem(10px);
    }

    .validation-message {
        color: $Red20;
        font-size: rem(12px);
    }

    .validation-errors {
        margin: rem(5px);
    }
</style>

@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""ValidationForm"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitToggle @bind-Value=""ValidationForm.TermsAgreement"" DefaultText=""I agree with the terms and conditions."" />
            <ValidationMessage For=""@(() => ValidationForm.TermsAgreement)"" />
        </div>

        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";
    private readonly string example7CsharpCode = @"
public class BitToggleValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; } = true;
}

public BitToggleValidationModel ValidationForm { get; set; } = new();
private string SuccessMessage = string.Empty;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";
}
