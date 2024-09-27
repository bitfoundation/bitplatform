namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public partial class BitToggleDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Classes",
            Type = "BitToggleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the toggle.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "bool?",
            DefaultValue = "null",
            Description = "The default value of the toggle when the value parameter has not been assigned.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the toggle in full width of its container while putting space between the label and the knob.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the label and the knob in a single line together.",
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
            Type = "BitToggleClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the toggle.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Text",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default text used when the On or Off texts are null.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitToggleClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitToggle."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the BitToggle."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the BitToggle."
                },
                new()
                {
                    Name = "Button",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button of the BitToggle."
                },
                new()
                {
                    Name = "Checked",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the checked state of the BitToggle."
                },
                new()
                {
                    Name = "Thumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the thumb of the BitToggle."
                },
                new()
                {
                    Name = "Text",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the text of the BitToggle."
                }
            ]
        }
    ];



    private bool oneWayValue;
    private bool twoWayValue;

    public BitToggleValidationModel validationModel { get; set; } = new();
    private string SuccessMessage = string.Empty;

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }



    private readonly string example1RazorCode = @"
<BitToggle Label=""Basic"" />
<BitToggle Label=""Disabled"" IsEnabled=""false"" />";

    private readonly string example2RazorCode = @"
<BitToggle Label=""Text"" Text=""This is a toggle!"" />
<BitToggle Label=""OnText & OffText"" OnText=""Toggle is On"" OffText=""Toggle is Off"" />";

    private readonly string example3RazorCode = @"
<BitToggle Label=""This is an inline label"" Inline />

<BitToggle>
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitToggle>";

    private readonly string example4RazorCode = @"
<BitToggle Label=""This is a reversed label"" Reversed />

<BitToggle Label=""This is a reversed inline label"" Reversed Inline />";

    private readonly string example5RazorCode = @"
<BitToggle Label=""This is a full-width toggle"" FullWidth Inline />

<BitToggle Label=""This is a reversed full-width toggle"" Reversed FullWidth Inline />";

    private readonly string example6RazorCode = @"
<BitToggle Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" OnText=""On"" OffText=""Off"" />

<BitToggle Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" OnText=""On"" OffText=""Off"" />";
    private readonly string example6CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;";

    private readonly string example7RazorCode = @"
<style>
    .custom-thumb {
        background: #fff;
        width: 30px;
        height: 30px;
    }

    .custom-button {
        padding: 0;
        width: 52px;
        height: 22px;
        border: none;
        background: #ccc;
        border-radius: 11px;
    }

    .custom-check .custom-thumb {
        background: #ff6868;
    }

    .custom-check .custom-button {
        background: #ffcece;
    }

    .custom-check .custom-button:hover .custom-thumb {
        background: #ff6868;
    }
</style>


<BitToggle Label=""Styles""
           Styles=""@(new() { Root = ""--toggle-background: lightgray;"", Checked = ""--toggle-background: #2ecc71;"",
                             Thumb = ""background: whitesmoke; height: 28px; width: 28px;"",
                             Button = ""background: var(--toggle-background); border: none; border-radius: 60px; padding: 0; height: 30px; width: 50px;"" } )"" />

<BitToggle Label=""Classes""
           Classes=""@(new() { Thumb = ""custom-thumb"",
                              Button = ""custom-button"",
                              Checked = ""custom-check"" } )"" />";

    private readonly string example8RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitToggle Label=""Terms and conditions"" Text=""I agree."" @bind-Value=""validationModel.TermsAgreement"" />
    <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class BitToggleValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; } = true;
}

public BitToggleValidationModel validationModel { get; set; } = new();

private async Task HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example9RazorCode = @"
<BitToggle Label=""این یک تاگل است"" Dir=""BitDir.Rtl"" OnText=""روشن"" OffText=""خاموش"" />

<BitToggle Label=""این یک تاگل خطی است"" Dir=""BitDir.Rtl"" Inline />

<BitToggle Label=""این یک تاگل خطی برعکس است"" Dir=""BitDir.Rtl"" Reversed Inline />";
}
