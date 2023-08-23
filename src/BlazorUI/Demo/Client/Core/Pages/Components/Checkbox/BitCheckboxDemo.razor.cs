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
            DefaultValue = "BitCheckBoxSide.Start",
            Description = "Determines whether the checkbox should be shown before the label (start) or after (end).",
            Href = "#box-side-enum",
            LinkType = LinkType.Link,
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



    private bool isIndeterminate = true;
    
    private bool oneWayValue;
    private bool twoWayValue;
    private bool oneWayIsIndeterminate = true;
    private bool twoWayIsIndeterminate = true;

    private bool labelTemplateValue;
    private bool customCheckboxValue;

    private bool customContentValue;
    private bool customContentIsIndeterminate = true;

    private string SuccessMessage = string.Empty;
    private BitCheckboxValidationModel validationModel = new();

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
<BitCheckbox Label=""Basic checkbox"" />
<BitCheckbox Label=""Disable checkbox"" IsEnabled=""false"" />
<BitCheckbox Label=""Disable checked checkbox"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example2HTMLCode = @"
<BitCheckbox Label=""Custom checkmark checkbox"" CheckmarkIconName=""@BitIconName.Heart"" />
<BitCheckbox Label=""Disabled custom checkmark checkbox"" CheckmarkIconName=""@BitIconName.WavingHand"" Value=""true"" IsEnabled=""false"" />";

    private readonly string example3HTMLCode = @"
<BitCheckbox Label=""BitCheckboxSide.End - Basic Checkbox"" BoxSide=""@BitCheckboxSide.End"" />
<BitCheckbox Label=""BitCheckboxSide.End - Disable Checkbox"" BoxSide=""@BitCheckboxSide.End"" IsEnabled=""false"" />
<BitCheckbox Label=""BitCheckboxSide.End - Disable Checked Checkbox"" BoxSide=""@BitCheckboxSide.End"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example4HTMLCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" @bind-IsIndeterminate=""isIndeterminate"" />
<BitCheckbox Label=""Disabled indeterminate checkbox"" IsIndeterminate=""true"" IsEnabled=""false"" />";
    private readonly string example4CSharpCode = @"
private bool isIndeterminate = true;
";

    private readonly string example5HTMLCode = @"
<BitCheckbox Label=""One-way checked checkbox (Fixed)"" Value=""true"" />

<BitCheckbox Label=""One-way controlled checkbox"" Value=""oneWayValue"" />
<BitButton OnClick=""() => oneWayValue = !oneWayValue"">@(oneWayValue ? ""Remove"" : ""Make"") Checked</BitButton>

<BitCheckbox Label=""Two-way controlled checkbox"" @bind-Value=""twoWayValue"" />
<BitButton OnClick=""() => twoWayValue = !twoWayValue"">@(twoWayValue ? ""Remove"" : ""Make"") Checked</BitButton>


<BitCheckbox Label=""One-way indeterminate checkbox (Fixed)"" IsIndeterminate=""true"" />

<BitCheckbox Label=""One-way Controlled indeterminate checkbox"" IsIndeterminate=""oneWayIsIndeterminate"" />
<BitButton OnClick=""() => oneWayIsIndeterminate = !oneWayIsIndeterminate"">@(oneWayIsIndeterminate ? ""Remove"" : ""Make"") Indeterminate</BitButton>

<BitCheckbox Label=""Two-way Controlled indeterminate checkbox"" @bind-IsIndeterminate=""twoWayIsIndeterminate"" />
<BitButton OnClick=""() => twoWayIsIndeterminate = !twoWayIsIndeterminate"">@(twoWayIsIndeterminate ? ""Remove"" : ""Make"") Indeterminate</BitButton>";
    private readonly string example5CSharpCode = @"
private bool oneWayValue;
private bool twoWayValue;
private bool oneWayIsIndeterminate = true;
private bool twoWayIsIndeterminate = true;
";

    private readonly string example6HTMLCode = @"
<BitCheckbox @bind-Value=""labelTemplateValue"">
    <LabelTemplate>
        <span style=""@(labelTemplateValue ? ""color: green;"" : ""color: red;"")"">Label Template</span>
    </LabelTemplate>
</BitCheckbox>";
    private readonly string example6CSharpCode = @"
private bool labelTemplateValue;
";

    private readonly string example7HTMLCode = @"
<style>
    .custom-checkbox {
        gap: 0.5rem;
        display: flex;
        cursor: pointer;
        align-items: center;
    }

    .custom-box {
        display: flex;
        width: rem2(32px);
        height: rem2(32px);
        border: 1px solid;
        align-items: center;
        margin-right: rem2(5px);
        justify-content: center;
    }
</style>

<BitCheckbox @bind-Value=""customCheckboxValue"">
    <div class=""custom-checkbox"">
        <div class=""custom-box"">
            <BitIcon IconName=""@(customCheckboxValue ? BitIconName.Accept : null)"" />
        </div>
        <span>Custom basic checkbox</span>
    </div>
</BitCheckbox>

<BitCheckbox @bind-Value=""customContentValue"" @bind-IsIndeterminate=""customContentIsIndeterminate"">
    <div class=""custom-checkbox"">
        <div class=""custom-box"">
            <BitIcon IconName=""@(customContentIsIndeterminate ? BitIconName.Fingerprint : (customContentValue ? BitIconName.Accept : null))"" />
        </div>
        <span>Custom indeterminate checkbox</span>
    </div>
</BitCheckbox>
<BitButton OnClick=""() => customContentIsIndeterminate = true"">Make Indeterminate</BitButton>";
    private readonly string example7CSharpCode = @"
private bool customCheckboxValue;
private bool customContentValue;
private bool customContentIsIndeterminate = true;
";

    private readonly string example8HTMLCode = @"
@using System.ComponentModel.DataAnnotations;

<style>
    .validation-message {
        color: #A4262C;
        font-size: 0.75rem;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    <div>
        <BitCheckbox Label=""I agree with the terms and conditions."" @bind-Value=""validationModel.TermsAgreement"" />
        <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example8CSharpCode = @"
private BitCheckboxValidationModel validationModel = new();

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";
}
