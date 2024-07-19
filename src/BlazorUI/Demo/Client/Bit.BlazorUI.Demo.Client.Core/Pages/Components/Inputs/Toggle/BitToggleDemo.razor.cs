namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Toggle;

public partial class BitToggleDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Classes",
            Type = "BitToggleClassStyles?",
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
            Description = "Default text used when the On or Off texts are null.",
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
            }
        }
    };



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
<BitToggle Label=""DefaultText"" DefaultText=""This is a good toggle!"" />
<BitToggle Label=""OnText & OffText"" OnText=""Toggle is On"" OffText=""Toggle is Off"" />";

    private readonly string example3RazorCode = @"
<BitToggle Label=""This is an inline label"" IsInlineLabel=""true"" />

<BitToggle>
    <LabelTemplate>
        <div style=""display:flex;align-items:center;gap:10px"">
            <BitLabel Style=""color:green"">This is custom Label</BitLabel>
            <BitIcon IconName=""@BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitToggle>";

    private readonly string example4RazorCode = @"
<BitToggle Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" OnText=""On"" OffText=""Off"" />

<BitToggle Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" OnText=""On"" OffText=""Off"" />";
    private readonly string example4CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;";

    private readonly string example5RazorCode = @"
<style>
    .custom-thumb {
        background: #fff;
        width: rem2(30px);
        height: rem2(30px);
    }

    .custom-button {
        padding: 0;
        border: none;
        background: #ccc;
        width: rem2(52px);
        height: rem2(22px);
        border-radius: rem2(11px);
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

    private readonly string example6RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitToggle Label=""Terms and conditions"" DefaultText=""I agree."" @bind-Value=""validationModel.TermsAgreement"" />
    <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example6CsharpCode = @"
public class BitToggleValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; } = true;
}

public BitToggleValidationModel validationModel { get; set; } = new();

private async Task HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example7RazorCode = @"
<BitToggle Dir=""BitDir.Rtl"" OnText=""روشن"" OffText=""خاموش"" />";
}
