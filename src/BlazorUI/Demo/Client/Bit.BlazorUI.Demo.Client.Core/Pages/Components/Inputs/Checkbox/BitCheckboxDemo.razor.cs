namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Checkbox;

public partial class BitCheckboxDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "CheckIconName",
            Type = "string",
            DefaultValue = "Accept",
            Description = "Custom icon for the check mark rendered by the checkbox instead of default check mark icon.",
        },
        new()
        {
            Name = "CheckIconAriaLabel",
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
            Name = "DefaultIndeterminate",
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
            Name = "Indeterminate",
            Type = "bool",
            DefaultValue = "false",
            Description = "An indeterminate visual state for checkbox. Setting indeterminate state takes visual precedence over checked given but does not affect on Value state.",
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
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the label and checkbox location."
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
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitCheckboxClassStyles",
            Parameters =
            [
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
                   Name = "Checked",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the checked state of the BitCheckbox."
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
            ]
        }
    ];



    private bool oneWayValue;
    private bool twoWayValue;
    private bool oneWayIndeterminate = true;
    private bool twoWayIndeterminate = true;

    private bool customCheckboxValue;

    private bool customContentValue;
    private bool customContentIndeterminate = true;

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
<BitCheckbox Label=""Custom check icon"" CheckIconName=""@BitIconName.Heart"" />
<BitCheckbox Label=""Disabled custom check icon"" CheckIconName=""@BitIconName.WavingHand"" Value=""true"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitCheckbox Label=""Reversed"" Reversed />
<BitCheckbox Label=""Reversed - Disabled"" Reversed IsEnabled=""false"" />
<BitCheckbox Label=""Reversed - Disable Checked"" Reversed IsEnabled=""false"" Value=""true"" />";

    private readonly string example4RazorCode = @"
<BitCheckbox>
    <LabelTemplate>
        <BitTag Color=""BitColor.Success"">Label Template</BitTag>
    </LabelTemplate>
</BitCheckbox>";

    private readonly string example5RazorCode = @"
<BitCheckbox Label=""Indeterminate checkbox"" Indeterminate />
<BitCheckbox Label=""Disabled indeterminate checkbox"" Indeterminate IsEnabled=""false"" />";

    private readonly string example6RazorCode = @"
<BitCheckbox Label=""One-way checked (Fixed)"" Value=""true"" />

<BitCheckbox Label=""One-way"" Value=""oneWayValue"" />
<BitToggleButton @bind-IsChecked=""oneWayValue"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way controlled checkbox"" @bind-Value=""twoWayValue"" />
<BitToggleButton @bind-IsChecked=""twoWayValue"" Text=""Toggle"" />


<BitCheckbox Label=""One-way indeterminate (Fixed)"" Indeterminate />

<BitCheckbox Label=""One-way indeterminate"" Indeterminate=""oneWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""oneWayIndeterminate"" Text=""Toggle"" />

<BitCheckbox Label=""Two-way indeterminate"" @bind-Indeterminate=""twoWayIndeterminate"" />
<BitToggleButton @bind-IsChecked=""twoWayIndeterminate"" Text=""Toggle"" />";
    private readonly string example6CsharpCode = @"
private bool oneWayValue;
private bool twoWayValue;
private bool oneWayIndeterminate = true;
private bool twoWayIndeterminate = true;";

    private readonly string example7RazorCode = @"
<BitCheckbox @bind-Value=""customCheckboxValue"">
    <BitIcon Style=""border:1px solid gray;width:22px;height:22px""
             IconName=""@(customCheckboxValue ? BitIconName.Accept : null)"" />
    <span>Custom basic checkbox</span>
</BitCheckbox>


<BitCheckbox @bind-Value=""customContentValue"" @bind-Indeterminate=""customContentIndeterminate"">
    <BitIcon Style=""border:1px solid gray;width:22px;height:22px""
             IconName=""@(customContentIndeterminate ? BitIconName.Fingerprint : (customContentValue ? BitIconName.Accept : null))"" />
    <span>Custom indeterminate checkbox</span>
</BitCheckbox>
<BitButton OnClick=""() => customContentIndeterminate = true"">Make Indeterminate</BitButton>";
    private readonly string example7CsharpCode = @"
private bool customCheckboxValue;
private bool customContentValue;
private bool customContentIndeterminate = true;";

    private readonly string example8RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border-radius: 0.125rem;
        background-color: #d3d3d347;
        border: 1px solid dodgerblue;
    }


    .custom-label {
        font-weight: bold;
        color: lightseagreen;
    }

    .custom-icon {
        color: lightseagreen
    }

    .custom-box {
        border-radius: 0.2rem;
        border-color: lightseagreen;
    }

    .custom-checked .custom-icon {
        color: white
    }

    .custom-checked:hover .custom-icon {
        color: whitesmoke;
    }

    .custom-checked .custom-box {
        background-color: lightseagreen;
    }

    .custom-checked:hover .custom-box {
        border-color: mediumseagreen;
    }
</style>


<BitCheckbox Label=""Styled checkbox"" Style=""color: dodgerblue; text-shadow: lightskyblue 0 0 1rem;"" />

<BitCheckbox Label=""Classed checkbox"" Class=""custom-class"" />


<BitCheckbox Label=""Styles"" 
             Styles=""@(new() { Checked = ""--check-color: deeppink; --icon-color: white;"", 
                               Label = ""color: var(--check-color);"",
                               Box = ""border-radius: 50%; border-color: var(--check-color); background-color: var(--check-color);"",
                               Icon = ""color: var(--icon-color);"" })"" />

<BitCheckbox Label=""Classes"" 
             Classes=""@(new() { Checked = ""custom-checked"",
                                Icon = ""custom-icon"",
                                Label=""custom-label"",
                                Box=""custom-box"" })"" />";

    private readonly string example9RazorCode = @"
<style>
    .validation-message {
        color: red;
        font-size: 0.75rem;
    }
</style>

<EditForm Model=""validationModel""
          OnValidSubmit=""HandleValidSubmit""
          OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    <BitCheckbox Label=""I agree with the terms and conditions.""
                 @bind-Value=""validationModel.TermsAgreement"" />
    <ValidationMessage For=""@(() => validationModel.TermsAgreement)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example9CsharpCode = @"
private BitCheckboxValidationModel validationModel = new();

public class BitCheckboxValidationModel
{
    [Range(typeof(bool), ""true"", ""true"", ErrorMessage = ""You must agree to the terms and conditions."")]
    public bool TermsAgreement { get; set; }
}

private async Task HandleValidSubmit() { }

private void HandleInvalidSubmit() { }";

    private readonly string example10RazorCode = @"
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس راست به چپ"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال"" IsEnabled=""false"" />
<BitCheckbox Dir=""BitDir.Rtl"" Label=""چکباکس غیرفعال چک شده"" IsEnabled=""false"" Value=""true"" />";

}
