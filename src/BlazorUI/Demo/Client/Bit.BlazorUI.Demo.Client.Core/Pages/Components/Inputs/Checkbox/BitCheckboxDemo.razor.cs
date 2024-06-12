namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

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
            Name = "Classes",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
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
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the checkbox clicked.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCheckboxClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCheckbox.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title text applied to the root element and the hidden checkbox input.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "class-styles",
            Title = "BitCheckboxClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCheckBox.",
                },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitCheckbox."
               },
               new()
               {
                   Name = "Box",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the box element of the BitCheckbox."
               },
               new()
               {
                   Name = "Icon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the icon of the BitCheckbox."
               },
               new()
               {
                   Name = "Label",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the label of the BitCheckbox."
               }
            }
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


    private readonly string example1RazorCode = @"
<BitCheckbox Label=""Basic checkbox"" />
<BitCheckbox Label=""Disable checkbox"" IsEnabled=""false"" />
<BitCheckbox Label=""Disable checked checkbox"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example2RazorCode = @"
<BitCheckbox Label=""Custom checkmark checkbox"" CheckmarkIconName=""@BitIconName.Heart"" />
<BitCheckbox Label=""Disabled custom checkmark checkbox"" CheckmarkIconName=""@BitIconName.WavingHand"" Value=""true"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitCheckbox Label=""BitCheckboxSide.End - Basic Checkbox"" BoxSide=""@BitCheckboxSide.End"" />
<BitCheckbox Label=""BitCheckboxSide.End - Disable Checkbox"" BoxSide=""@BitCheckboxSide.End"" IsEnabled=""false"" />
<BitCheckbox Label=""BitCheckboxSide.End - Disable Checked Checkbox"" BoxSide=""@BitCheckboxSide.End"" IsEnabled=""false"" Value=""true"" />";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        color: brown;
        padding: 1rem;
        background-color: aquamarine;
    }

    .custom-label {
        color: darkblue;
        font-size: 18px;
        font-weight: bold;
    }

    .custom-box {
        border-width: 3px;
        border-color: crimson;
    }
</style>


<BitCheckbox Label=""Styled checkbox"" Style=""background-color:aqua;color:red"" />

<BitCheckbox Label=""Classed checkbox"" Class=""custom-class"" />


<BitCheckbox Label=""Styles"" Styles=""@(new() { Label=""color:darkgoldenrod"", Box=""border-color:brown"", Icon=""color:red"" })"" />

<BitCheckbox Label=""Classes"" Classes=""@(new() { Label=""custom-label"", Box=""custom-box"" })"" />";

    private readonly string example5RazorCode = @"
Visible: [ <BitCheckbox Visibility=""BitVisibility.Visible"" Label=""Visible Checkbox"" /> ]
Hidden: [ <BitCheckbox Visibility=""BitVisibility.Hidden"" Label=""Hidden Checkbox"" /> ]
Collapsed: [ <BitCheckbox Visibility=""BitVisibility.Collapsed"" Label=""Collapsed Checkbox"" /> ]";

    private readonly string example6RazorCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" @bind-IsIndeterminate=""isIndeterminate"" />
<BitCheckbox Label=""Disabled indeterminate checkbox"" IsIndeterminate=""true"" IsEnabled=""false"" />";
    private readonly string example6CsharpCode = @"
private bool isIndeterminate = true;
";

    private readonly string example7RazorCode = @"
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
    private readonly string example7CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;
private bool oneWayIsIndeterminate = true;
private bool twoWayIsIndeterminate = true;
";

    private readonly string example8RazorCode = @"
<BitCheckbox @bind-Value=""labelTemplateValue"">
    <LabelTemplate>
        <span style=""@(labelTemplateValue ? ""color: green;"" : ""color: red;"")"">Label Template</span>
    </LabelTemplate>
</BitCheckbox>";
    private readonly string example8CsharpCode = @"
private bool labelTemplateValue;
";

    private readonly string example9RazorCode = @"
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
    private readonly string example9CsharpCode = @"
private bool customCheckboxValue;
private bool customContentValue;
private bool customContentIsIndeterminate = true;
";

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
private BitCheckboxValidationModel validationModel = new();

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";

    private readonly string example11RazorCode = @"
<BitCheckbox Dir=""BitDir.Rtl"" Label=""کادر بررسی"" />";
}
