using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Checkbox;

public partial class BitCheckboxDemo
{
    private bool IsIndeterminated = true;
    private bool IsChecked_OneWay;
    private bool IsChecked_TwoWay;
    private bool IsIndeterminated_OneWay = true;
    private bool IsIndeterminated_TwoWay = true;
    private bool IsCheckedLabelTemplate;
    private bool IsCheckedCustomCheckBox;
    private bool IsCheckedCustomIndeterminateCheckBox;
    private bool IsIndeterminatedCustomCheckBox = true;
    private BitCheckboxValidationModel ValidationForm = new();
    private string SuccessMessage = string.Empty;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaDescription",
            Type = "string",
            Description = "Detailed description of the checkbox for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "AriaLabelledby",
            Type = "string",
            Description = "ID for element that contains label information for the checkbox.",
        },
        new ComponentParameter()
        {
            Name = "AriaPositionInSet",
            Type = "string",
            Description = "The position in the parent set (if in a set) for aria-posinset.",
        },
        new ComponentParameter()
        {
            Name = "AriaSetSize",
            Type = "string",
            Description = "The total size of the parent set (if in a set) for aria-setsize.",
        },
        new ComponentParameter()
        {
            Name = "BoxSide",
            Type = "BitBoxSide",
            LinkType = LinkType.Link,
            Href = "#box-side-enum",
            DefaultValue = "BitBoxSide.Start",
            Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
        },
        new ComponentParameter()
        {
            Name = "CheckmarkIconName",
            Type = "BitIcon",
            Description = "Custom icon for the check mark rendered by the checkbox instade of default check mark icon.",
        },
        new ComponentParameter()
        {
            Name = "CheckmarkIconAriaLabel",
            Type = "string",
            Description = "The aria label of the icon for the benefit of screen readers.",
        },
        new ComponentParameter()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "Used to customize the content of checkbox(Label and Box).",
        },
        new ComponentParameter()
        {
            Name = "DefaultIsIndeterminate",
            Type = "bool",
            Description = "Default indeterminate visual state for checkbox.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "bool",
            Description = "Use this if you want an uncontrolled component, meaning the Checkbox instance maintains its own state.",
        },
        new ComponentParameter()
        {
            Name = "IsIndeterminate",
            Type = "bool",
            Description = "Callback that is called when the IsIndeterminate parameter changed.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string?",
            Description = "Descriptive label for the checkbox.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the label for the checkbox.",
        },
        new ComponentParameter()
        {
            Name = "Name",
            Type = "string",
            Description = "Name for the checkbox input. This is intended for use with forms and NOT displayed in the UI.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the checked value has changed.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the checkbox clicked.",
        },
        new ComponentParameter()
        {
            Name = "Title",
            Type = "string",
            Description = "Title text applied to the root element and the hidden checkbox input.",
        },

        new ComponentParameter()
        {
            Name = "Value",
            Type = "bool",
            Description = "Checkbox state, control the checked state at a higher level.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the Value parameter changed.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "box-side-enum",
            Title = "BitBoxSide Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Start",
                    Description="The checkbox shows before the label.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "End",
                    Description="The checkbox shows after the label.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: column wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: flex-start;
    }
</style>

<div class=""example-content"">
    <BitCheckbox Label=""Basic Checkbox"" />
    <BitCheckbox Label=""One-way Checked Checkbox (Fixed)"" Value=""true"" />
    <BitCheckbox Label=""Disable Checkbox"" IsEnabled=""false"" />
    <BitCheckbox Label=""Disable Checked Checkbox"" IsEnabled=""false"" Value=""true"" />
    <BitCheckbox Label=""Custom checkmark Checkbox"" CheckmarkIconName=""BitIconName.Heart"" />
</div>";

    private readonly string example2TMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: column wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: flex-start;
    }
</style>

<div class=""example-content"">
    <BitCheckbox Label=""Reversed - Basic Checkbox"" BoxSide=""@BitCheckBoxSide.End"" />
    <BitCheckbox Label=""Reversed - One-way Checked Checkbox (Fixed)"" BoxSide=""@BitCheckBoxSide.End"" Value=""true"" />
    <BitCheckbox Label=""Reversed - Disable Checkbox"" BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"" />
    <BitCheckbox Label=""Reversed - Disable Checked Checkbox"" BoxSide=""@BitCheckBoxSide.End"" IsEnabled=""false"" Value=""true"" />
</div>";

    private readonly string example3HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: column wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: flex-start;
    }
</style>

<div class=""example-content"">
    <BitCheckbox Label=""Indeterminate checkbox"" @bind-IsIndeterminate=""IsIndeterminated"" />
    <BitCheckbox Label=""One-way indeterminate Checkbox (Fixed)"" IsIndeterminate=""true"" />
    <BitCheckbox Label=""Disabled indeterminate checkbox"" IsIndeterminate=""true"" IsEnabled=""false"" />
</div>";

    private readonly string example3CSharpCode = @"
private bool IsIndeterminated = true;
";

    private readonly string example4HTMLCode = @"
<style>
    .example-content {
        display: flex;
        flex-flow: column wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: flex-start;
    }

    .controlled-box {
        display: flex;
        flex-direction: row;
        gap: 1rem;
        width: fit-content;
    }

    @media screen and (max-width: 37.5em) {
        .controlled-box {
            flex-direction: column;
        }
    }
</style>

<div class=""example-content"">
    <div class=""controlled-box"">
        <BitCheckbox Label=""One-way Controlled Checkbox"" Value=""IsChecked_OneWay"" />
        <BitButton OnClick=""() => IsChecked_OneWay = !IsChecked_OneWay"" ButtonSize=""BitButtonSize.Small"">
            @(IsChecked_OneWay ? ""Remove"" : ""Make"") Checked
        </BitButton>
    </div>
    <div class=""controlled-box"">
        <BitCheckbox Label=""Two-way Controlled Checkbox"" @bind-Value=""IsChecked_TwoWay"" />
        <BitButton OnClick=""() => IsChecked_TwoWay = !IsChecked_TwoWay"" ButtonSize=""BitButtonSize.Small"">
            @(IsChecked_TwoWay ? ""Remove"" : ""Make"") Checked
        </BitButton>
    </div>
    <div class=""controlled-box"">
        <BitCheckbox Label=""One-way Controlled indeterminate checkbox"" IsIndeterminate=""IsIndeterminated_OneWay"" />
        <BitButton OnClick=""() => IsIndeterminated_OneWay = !IsIndeterminated_OneWay"" ButtonSize=""BitButtonSize.Small"">
            @(IsIndeterminated_OneWay ? ""Remove"" : ""Make"") Indeterminate
        </BitButton>
    </div>
    <div class=""controlled-box"">
        <BitCheckbox Label=""Two-way Controlled indeterminate checkbox"" @bind-IsIndeterminate=""IsIndeterminated_TwoWay"" />
        <BitButton OnClick=""() => IsIndeterminated_TwoWay = !IsIndeterminated_TwoWay"" ButtonSize=""BitButtonSize.Small"">
            @(IsIndeterminated_TwoWay ? ""Remove"" : ""Make"") Indeterminate
        </BitButton>
    </div>
</div>";

    private readonly string example4CSharpCode = @"
private bool IsChecked_OneWay;
private bool IsChecked_TwoWay;
private bool IsIndeterminated_OneWay = true;
private bool IsIndeterminated_TwoWay = true;
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
    .example-content {
        display: flex;
        flex-flow: column wrap;
        gap: 0.5rem;
        width: fit-content;
        align-items: flex-start;
    }

    .custom-checkbox {
        display: flex;
        justify-content: center;
        cursor: pointer;
        user-select: none;
        align-items: center;
    }

        .custom-checkbox .checked-box {
            border: 1px solid;
            margin-right: 0.3125rem;
        }
</style>

<div class=""example-content"">
    <BitCheckbox @bind-Value=""IsCheckedCustomCheckBox"">
        <div class=""custom-checkbox"">
            <BitIconButton Class=""checked-box"" IconName=""@(IsCheckedCustomCheckBox ? BitIconName.Accept : BitIconName.NotSet)"" />
            <span>
                Customized Basic Checkbox
            </span>
        </div>
    </BitCheckbox>

    <BitCheckbox @bind-Value=""IsCheckedCustomIndeterminateCheckBox"" @bind-IsIndeterminate=""IsIndeterminatedCustomCheckBox"">
        <div class=""custom-checkbox"">
            @if (IsIndeterminatedCustomCheckBox)
            {
                <BitIconButton Class=""checked-box"" IconName=""BitIconName.Fingerprint"" />
            }
            else
            {
                <BitIconButton Class=""checked-box"" IconName=""@(IsCheckedCustomIndeterminateCheckBox ? BitIconName.Accept : BitIconName.NotSet)"" />
            }
            <span>
                Customized Indeterminate checkbox
            </span>
        </div>
    </BitCheckbox>
</div>";

    private readonly string example6CSharpCode = @"
private bool IsCheckedCustomCheckBox;
private bool IsCheckedCustomIndeterminateCheckBox;
private bool IsIndeterminatedCustomCheckBox = true;
";

    private readonly string example7HTMLCode = @"
@using System.ComponentModel.DataAnnotations;

<style>
    .validation-summary {
        border-left: 0.25rem solid #d13438;
        background-color: #FDE7E9;
        overflow: hidden;
        margin-bottom: 0.8rem;
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
}
";

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
}
";

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
}
