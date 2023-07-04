using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Toggle;

public partial class BitToggleDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "DefaultText",
            Type = "string?",
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
            Description = "Label of the toggle.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
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
            Description = "Text to display when toggle is OFF.",
        },
        new()
        {
            Name = "OnText",
            Type = "string?",
            Description = "Text to display when toggle is ON.",
        },
        new()
        {
            Name = "Role",
            Type = "string",
            DefaultValue = "switch",
            Description = "Denotes role of the toggle, default is switch.",
        },
    };



    private bool BasicValue;
    private bool DisabledValue;
    private bool OnTextValue;
    private bool InLineLabelValue;
    private bool DefaultTextValue;

    private bool LabelTemplateValue;

    private bool OneWayValue;
    private bool TwoWayValue;

    public BitToggleValidationModel ValidationForm { get; set; } = new();
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


    private readonly string example1HTMLCode = @"
<BitToggle Label=""Basic"" @bind-Value=""BasicValue"" />

<BitToggle Label=""Disabled"" @bind-Value=""DisabledValue"" IsEnabled=""false"" />

<BitToggle Label=""OnText And OffText"" @bind-Value=""OnTextValue"" OnText=""On"" OffText=""Off"" />

<BitToggle Label=""Inline Label"" @bind-Value=""InLineLabelValue"" IsInlineLabel=""true"" />

<BitToggle @bind-Value=""DefaultTextValue"" DefaultText=""Default Text"" />";
    private readonly string example1CSharpCode = @"
private bool BasicValue;
private bool DisabledValue;
private bool OnTextValue;
private bool InLineLabelValue;
private bool DefaultTextValue;";

    private readonly string example2HTMLCode = @"
<BitToggle @bind-Value=""LabelTemplateValue"">
    <LabelTemplate>
        <span style=""color: green;"">This is custom Label</span>
        <BitIcon IconName=""BitIconName.Filter"" />
    </LabelTemplate>
</BitToggle>";
    private readonly string example2CSharpCode = @"
private bool LabelTemplateValue;";

    private readonly string example3HTMLCode = @"
<BitToggle Value=""OneWayValue"" Label=""One-way"" OnText=""Off"" OffText=""On"" />
<BitToggleButton @bind-IsChecked=""OneWayValue"" Label=""@(OneWayValue ? ""Off"" : ""On"")"" />

<BitToggle @bind-Value=""TwoWayValue"" Label=""Two-way"" OnText=""Off"" OffText=""On"" />
<BitToggleButton @bind-IsChecked=""TwoWayValue"" Label=""@(TwoWayValue ? ""Off"" : ""On"")"" />";
    private readonly string example3CSharpCode = @"
private bool OneWayValue;
private bool TwoWayValue;";

    private readonly string example4HTMLCode = @"
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
    private readonly string example4CSharpCode = @"
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
