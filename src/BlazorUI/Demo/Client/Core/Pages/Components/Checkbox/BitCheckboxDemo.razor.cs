namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Checkbox;

public partial class BitCheckboxDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the checkbox for the benefit of screen readers.",
        },
        new()
        {
            Name = "AriaLabelledby",
            Type = "string?",
            DefaultValue = "null",
            Description = "ID for element that contains label information for the checkbox.",
        },
        new()
        {
            Name = "AriaPositionInSet",
            Type = "int?",
            DefaultValue = "null",
            Description = "The position in the parent set (if in a set) for aria-posinset.",
        },
        new()
        {
            Name = "AriaSetSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The total size of the parent set (if in a set) for aria-setsize.",
        },
        new()
        {
            Name = "BoxSide",
            Type = "BitCheckBoxSide",
            LinkType = LinkType.Link,
            Href = "#box-side-enum",
            DefaultValue = "BitCheckBoxSide.Start",
            Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
        },
        new()
        {
            Name = "CheckmarkIconName",
            Type = "string",
            DefaultValue = "Accept",
            Description = "Custom icon for the check mark rendered by the checkbox instead of default check mark icon.",
        },
        new()
        {
            Name = "CheckmarkIconAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the content of checkbox(Label and Box).",
        },
        new()
        {
            Name = "DefaultIsIndeterminate",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default indeterminate visual state for checkbox.",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.",
        },
        new()
        {
            Name = "IsIndeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "Callback that is called when the IsIndeterminate parameter changed.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Descriptive label for the checkbox.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the checkbox.",
        },
        new()
        {
            Name = "Name",
            Type = "string?",
            DefaultValue = "null",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the checked value has changed.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the checkbox clicked.",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title text applied to the root element and the hidden checkbox input.",
        },

        new()
        {
            Name = "Value",
            Type = "bool",
            DefaultValue = "false",
            Description = "Checkbox state, control the checked state at a higher level.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the Value parameter changed.",
        }
    };
    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "box-side-enum",
            Name = "BitBoxSide",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Start",
                    Description="The checkbox shows before the label.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The checkbox shows after the label.",
                    Value="1",
                }
            }
        }
    };



    private bool IsIndeterminate = true;
    private bool IsChecked_OneWay;
    private bool IsChecked_TwoWay;
    private bool IsIndeterminate_OneWay = true;
    private bool IsIndeterminate_TwoWay = true;
    private bool IsCheckedLabelTemplate;
    private bool IsCheckedCustomCheckBox;
    private bool IsCheckedCustomIndeterminateCheckBox;
    private bool IsIndeterminateCustomCheckBox = true;
    private BitCheckboxValidationModel ValidationForm = new();
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
<BitCheckbox Label=""Basic Checkbox"" />
<BitCheckbox Label=""One-way Checked Checkbox (Fixed)"" Value=""true"" />
<BitCheckbox Label=""Disable Checkbox"" IsEnabled=""false"" />
<BitCheckbox Label=""Disable Checked Checkbox"" IsEnabled=""false"" Value=""true"" />
<BitCheckbox Label=""Custom checkmark Checkbox"" CheckmarkIconName=""@BitIconName.Heart"" />";

    private readonly string example2TMLCode = @"
<BitCheckbox Label=""Reversed - Basic Checkbox"" BoxSide=""@BitCheckBoxSide.End"" />
<BitCheckbox Label=""Reversed - One-way Checked Checkbox (Fixed)"" BoxSide=""@BitCheckBoxSide.End"" Value=""true"" />
<BitCheckbox Label=""Reversed - Disable Checkbox"" BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"" />
<BitCheckbox Label=""Reversed - Disable Checked Checkbox"" BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example3HTMLCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" @bind-IsIndeterminate=""IsIndeterminate"" />
<BitCheckbox Label=""One-way indeterminate Checkbox (Fixed)"" IsIndeterminate=""true"" />
<BitCheckbox Label=""Disabled indeterminate checkbox"" IsIndeterminate=""true"" IsEnabled=""false"" />";
    private readonly string example3CSharpCode = @"
private bool IsIndeterminate = true;
";

    private readonly string example4HTMLCode = @"
<BitCheckbox Label=""One-way Controlled Checkbox"" Value=""IsChecked_OneWay"" />
<BitButton OnClick=""() => IsChecked_OneWay = !IsChecked_OneWay"">
    @(IsChecked_OneWay ? ""Remove"" : ""Make"") Checked
</BitButton>

<BitCheckbox Label=""Two-way Controlled Checkbox"" @bind-Value=""IsChecked_TwoWay"" />
<BitButton OnClick=""() => IsChecked_TwoWay = !IsChecked_TwoWay"">
    @(IsChecked_TwoWay ? ""Remove"" : ""Make"") Checked
</BitButton>

<BitCheckbox Label=""One-way Controlled indeterminate checkbox"" IsIndeterminate=""IsIndeterminate_OneWay"" />
<BitButton OnClick=""() => IsIndeterminate_OneWay = !IsIndeterminate_OneWay"">
    @(IsIndeterminate_OneWay ? ""Remove"" : ""Make"") Indeterminate
</BitButton>

<BitCheckbox Label=""Two-way Controlled indeterminate checkbox"" @bind-IsIndeterminate=""IsIndeterminate_TwoWay"" />
<BitButton OnClick=""() => IsIndeterminate_TwoWay = !IsIndeterminate_TwoWay"">
    @(IsIndeterminate_TwoWay ? ""Remove"" : ""Make"") Indeterminate
</BitButton>";
    private readonly string example4CSharpCode = @"
private bool IsChecked_OneWay;
private bool IsChecked_TwoWay;
private bool IsIndeterminate_OneWay = true;
private bool IsIndeterminate_TwoWay = true;
";

    private readonly string example5HTMLCode = @"
<BitCheckbox @bind-Value=""IsCheckedLabelTemplate"">
    <LabelTemplate>
        <span style=""@(IsCheckedLabelTemplate ? ""color: green;"" : ""color: red;"")"">
            Label Template
        </span>
    </LabelTemplate>
</BitCheckbox>
";
    private readonly string example5CSharpCode = @"
private bool IsCheckedLabelTemplate;
";

    private readonly string example6HTMLCode = @"
<style>
    .custom-checkbox {
        gap: 0.5rem;
        display: flex;
        cursor: pointer;
        flex-wrap: wrap;
        user-select: none;
        align-items: center;
    }

    .custom-checkbox .checked-box {
        display: flex;
        width: rem(32px);
        height: rem(32px);
        border: 1px solid;
        align-items: center;
        margin-right: rem(5px);
        justify-content: center;
    }
</style>

<BitCheckbox @bind-Value=""IsCheckedCustomCheckBox"">
    <div class=""custom-checkbox"">
        <div class=""checked-box"">
            <BitIcon IconName=""@(IsCheckedCustomCheckBox ? BitIconName.Accept : null)"" />
        </div>
        <span>
            Custom Basic Checkbox
        </span>
    </div>
</BitCheckbox>

<BitCheckbox @bind-Value=""IsCheckedCustomIndeterminateCheckBox"" @bind-IsIndeterminate=""IsIndeterminateCustomCheckBox"">
    <div class=""custom-checkbox"">
        <div class=""checked-box"">
            <BitIcon IconName=""@(IsIndeterminateCustomCheckBox ? BitIconName.Fingerprint : (IsCheckedCustomIndeterminateCheckBox ? BitIconName.Accept : null))"" />
        </div>
        <span>
            Custom Indeterminate checkbox
        </span>
        <BitButton OnClick=""() => IsIndeterminateCustomCheckBox = true"">Make Indeterminate</BitButton>
    </div>
</BitCheckbox>";
    private readonly string example6CSharpCode = @"
private bool IsCheckedCustomCheckBox;
private bool IsCheckedCustomIndeterminateCheckBox;
private bool IsIndeterminateCustomCheckBox = true;
";

    private readonly string example7HTMLCode = @"
@using System.ComponentModel.DataAnnotations;

<style>
    .validation-summary {
        color: #A4262C;
        overflow: hidden;
        margin-bottom: 0.8rem;
        background-color: #FDE7E9;
        border-left: 0.25rem solid #d13438;
    }

    .validation-summary .validation-errors {
        margin: 0.25rem;
    }

    div.validation-message {
        color: #A4262C;
        font-size: 0.75rem;
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
            <BitCheckbox Label=""I agree with the terms and conditions."" @bind-Value=""ValidationForm.TermsAgreement"" />
            <ValidationMessage For=""@(() => ValidationForm.TermsAgreement)"" />
        </div>
        <br />
        <BitButton ButtonType=""BitButtonType.Submit"">
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
    private readonly string example7CSharpCode = @"
private BitCheckboxValidationModel ValidationForm = new();
private string SuccessMessage = string.Empty;

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

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
