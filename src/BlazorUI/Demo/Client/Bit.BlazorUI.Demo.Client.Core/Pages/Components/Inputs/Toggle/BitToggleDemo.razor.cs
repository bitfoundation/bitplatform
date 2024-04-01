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
    .custom-class {
        padding: 0.5rem;
        width: max-content;
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

<BitToggle Label=""Styled"" Style=""width:fit-content;background:forestgreen;border-radius:1rem;padding:1rem"" />
<BitToggle Label=""Classed"" Class=""custom-class"" />


<BitToggle Label=""Styles""
           Styles=""@(new() { Root = ""width:fit-content;background:pink;padding:1rem"",
                             Thumb = ""background:darkorange"",
                             Button = ""border-radius:0.5rem 0 0.5rem 0"",
                             Label = ""color:blue;font-weight:900;font-size:1.25rem"" } )"" />
<BitToggle Label=""Classes""
           DefaultText=""GooGooLi""
           Classes=""@(new() { Text = ""custom-text"",
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
