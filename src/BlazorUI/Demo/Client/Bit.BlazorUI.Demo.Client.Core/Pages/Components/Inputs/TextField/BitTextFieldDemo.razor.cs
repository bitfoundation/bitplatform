namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TextField;

public partial class BitTextFieldDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoComplete",
            Type = "string?",
            DefaultValue = "null",
            Description = "AutoComplete is a string that maps to the autocomplete attribute of the HTML input element.",
        },
        new()
        {
            Name = "CanRevealPassword",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the reveal password button for input type 'password'.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitTextFieldClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitTextField.",
            LinkType = LinkType.Link,
            Href = "#textfield-class-styles",
        },
        new()
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "Description displayed below the text field to provide additional details about what text to enter.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom description for text field.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name for the icon shown in the far right end of the text field.",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the input element.",
            LinkType = LinkType.Link,
            Href = "#input-mode",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label displayed above the text field and read by screen readers.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom label for text field.",
        },
        new()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Specifies the maximum number of characters allowed in the input.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is a Multiline text field.",
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is borderless.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the input clicked.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves out of the input.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for When a keyboard key is released.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Input placeholder text.",
        },
        new()
        {
            Name = "Prefix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Prefix displayed before the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom prefix for text field.",
        },
        new()
        {
            Name = "Resizable",
            Type = "bool",
            DefaultValue = "false",
            Description = "For multiline text fields, whether or not the field is resizable.",
        },
        new()
        {
            Name = "RevealPasswordAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "Rows",
            Type = "int?",
            DefaultValue = "null",
            Description = "For multiline text, Number of rows.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTextFieldClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#textfield-class-styles",
            Description = "Custom CSS styles for different parts of the BitTextField.",
        },
        new()
        {
            Name = "Suffix",
            Type = "string?",
            DefaultValue = "null",
            Description = "Suffix displayed after the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Shows the custom suffix for text field.",
        },
        new()
        {
            Name = "Trim",
            Type = "bool",
            DefaultValue = "false",
            Description = "Specifies whether to remove any leading or trailing whitespace from the value.",
        },
        new()
        {
            Name = "Type",
            Type = "BitInputType",
            DefaultValue = "BitInputType.Text",
            Description = "Input type.",
            LinkType = LinkType.Link,
            Href = "#input-type-enum"
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is underlined.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "textfield-class-styles",
            Title = "BitTextFieldClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's root element."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles of the root element in focus state."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of label and input in the BitTextField."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's label."
                },
                new()
                {
                    Name = "FieldGroup",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's field group."
                },
                new()
                {
                    Name = "PrefixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's prefix container."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's prefix."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's input."
                },
                new()
                {
                    Name = "RevealPassword",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password."
                },
                new()
                {
                    Name = "RevealPasswordIconContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password icon container."
                },
                new()
                {
                    Name = "RevealPasswordIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's reveal password icon."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's icon."
                },
                new()
                {
                    Name = "SuffixContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's suffix container."
                },
                new()
                {
                    Name = "Suffix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's suffix."
                },
                new()
                {
                    Name = "DescriptionContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's description container."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitTextField's description."
                }
            }
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "input-type-enum",
            Name = "BitInputType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Text",
                    Description="The input expects text characters.",
                    Value="0",
                },
                new()
                {
                    Name= "Password",
                    Description="The input expects password characters.",
                    Value="1",
                },
                new()
                {
                    Name= "Number",
                    Description="The input expects number characters.",
                    Value="2",
                },
                new()
                {
                    Name= "Email",
                    Description="The input expects email characters.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="The input expects tel characters.",
                    Value="4",
                },
                new()
                {
                    Name= "Url",
                    Description="The input expects url characters.",
                    Value="5",
                }
            ]
        },
        new()
        {
            Id = "input-mode",
            Name = "BitInputMode",
            Description = "This allows a browser to display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="The input expects text characters.",
                    Value="0",
                },
                new()
                {
                    Name= "Text",
                    Description="Standard input keyboard for the user's current locale.",
                    Value="1",
                },
                new()
                {
                    Name= "Decimal",
                    Description="Fractional numeric input keyboard containing the digits and decimal separator for the user's locale.",
                    Value="2",
                },
                new()
                {
                    Name= "Numeric",
                    Description="Numeric input keyboard, but only requires the digits 0–9.",
                    Value="3",
                },
                new()
                {
                    Name= "Tel",
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key",
                    Value="4",
                },
                new()
                {
                    Name= "Search",
                    Description="A virtual keyboard optimized for search input.",
                    Value="5",
                },
                new()
                {
                    Name= "Email",
                    Description="A virtual keyboard optimized for entering email addresses.",
                    Value="6",
                },
                new()
                {
                    Name= "Url",
                    Description="A keypad optimized for entering URLs.",
                    Value="7",
                }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElement",
            Type = "ElementReference",
            Description = "The ElementReference to the input element of the BitTextField.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            Description = "Gives focus to the input element of the BitTextField.",
        }
    ];



    private string? oneWayValue;
    private string? twoWayValue;
    private string? onChangeValue;
    private string? immediateValue;
    private string? debounceValue;
    private string? throttleValue;

    private string? trimmedValue;
    private string? notTrimmedValue;

    private string? classesValue;

    private ValidationTextFieldModel validationTextFieldModel = new();
    public bool formIsValidSubmit;

    private async Task HandleValidSubmit()
    {
        formIsValidSubmit = true;

        await Task.Delay(2000);

        validationTextFieldModel = new();

        formIsValidSubmit = false;

        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        formIsValidSubmit = false;
    }



    private readonly string example1RazorCode = @"
<BitTextField Label=""Basic"" />
<BitTextField Label=""Placeholder"" Placeholder=""Enter a text..."" />
<BitTextField Label=""Disabled"" IsEnabled=""false"" />
<BitTextField Label=""ReadOnly"" ReadOnly DefaultValue=""This is ReadOnly"" />
<BitTextField Label=""Description"" Description=""This is Description"" />
<BitTextField Label=""Required"" Required />
<BitTextField Label=""MaxLength: 5"" MaxLength=""5"" />
<BitTextField Label=""Auto focused"" AutoFocus />";

    private readonly string example2RazorCode = @"
<BitTextField Label=""Basic"" Underlined />
<BitTextField Label=""Placeholder"" Underlined Placeholder=""Enter a text..."" />
<BitTextField Label=""Disabled"" Underlined IsEnabled=""false"" />
<BitTextField Label=""Required"" Underlined Required />";

    private readonly string example3RazorCode = @"
<BitTextField Label=""Basic"" Placeholder=""Enter a text..."" NoBorder />
<BitTextField Label=""Disabled"" Placeholder=""Enter a text..."" NoBorder IsEnabled=""false"" />
<BitTextField Label=""Required"" Placeholder=""Enter a text..."" NoBorder Required />";

    private readonly string example4RazorCode = @"
<BitTextField Label=""Multiline"" Multiline />
<BitTextField Label=""Resizable"" Multiline Resizable />
<BitTextField Label=""Rows = 10"" Multiline Rows=""10"" />";

    private readonly string example5RazorCode = @"
<BitTextField Label=""Email"" IconName=""@BitIconName.EditMail"" />
<BitTextField Label=""Calendar"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example6RazorCode = @"
<BitTextField Label=""Prefix"" Prefix=""https://"" />
<BitTextField Label=""Suffix"" Suffix="".com"" />
<BitTextField Label=""Prefix and Suffix"" Prefix=""https://"" Suffix="".com"" />
<BitTextField Label=""Disabled"" Prefix=""https://"" Suffix="".com"" IsEnabled=""false"" />";

    private readonly string example7RazorCode = @"
<BitTextField>
    <LabelTemplate>
        <BitLabel Style=""color:coral"">Custom Label</BitLabel>
    </LabelTemplate>
</BitTextField>

<BitTextField Label=""Custom Description"">
    <DescriptionTemplate>
        <BitLabel Style=""color:coral"">Description</BitLabel>
    </DescriptionTemplate>
</BitTextField>

<BitTextField Label=""Custom Prefix"">
    <PrefixTemplate>
        <BitLabel Style=""color:coral;margin:0 5px"">Prefix</BitLabel>
    </PrefixTemplate>
</BitTextField>

<BitTextField Label=""Custom Suffix"">
    <SuffixTemplate>
        <BitLabel Style=""color:coral;margin:0 5px"">Suffix</BitLabel>
    </SuffixTemplate>
</BitTextField>";

    private readonly string example8RazorCode = @"
<BitTextField Label=""Password"" Type=""BitInputType.Password"" />
<BitTextField Label=""Reveal Password"" Type=""BitInputType.Password"" CanRevealPassword />";

    private readonly string example9RazorCode = @"
<BitTextField Label=""One-way"" Value=""@oneWayValue"" />
<div>Value: [@oneWayValue]</div>
<BitOtpInput Length=""5"" Style=""margin-top: 5px;"" @bind-Value=""oneWayValue"" />

<BitTextField Label=""Two-way"" @bind-Value=""twoWayValue"" />
<div>Value: [@twoWayValue]</div>
<BitOtpInput Length=""5"" Style=""margin-top: 5px;"" @bind-Value=""twoWayValue"" Immediate />

<BitTextField Label=""OnChange"" OnChange=""(v) => onChangeValue = v"" Immediate />
<BitLabel>Value: [@onChangeValue]</BitLabel>

<BitTextField Label=""Immediate"" @bind-Value=""@immediateValue"" Immediate />
<div>Value: [@immediateValue]</div>

<BitTextField Label=""Debounce"" @bind-Value=""@debounceValue"" Immediate DebounceTime=""300"" />
<div>Value: [@debounceValue]</div>

<BitTextField Label=""Throttle"" @bind-Value=""@throttleValue"" Immediate ThrottleTime=""300"" />
<div>Value: [@throttleValue]</div>";
    private readonly string example9CsharpCode = @"
private string oneWayValue;
private string twoWayValue;
private string onChangeValue;
private string? immediateValue;
private string? debounceValue;
private string? throttleValue;";

    private readonly string example10RazorCode = @"
<BitTextField Label=""Trimmed"" Trim @bind-Value=""trimmedValue"" />
<pre>[@trimmedValue]</pre>

<BitTextField Label=""Not Trimmed"" @bind-Value=""notTrimmedValue"" />
<pre>[@notTrimmedValue]</pre>";
    private readonly string example10CsharpCode = @"
private string trimmedValue;
private string notTrimmedValue;";

    private readonly string example11RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid brown;
    }

    .custom-class *, .custom-class *::after {
        border: none;
    }

    .custom-class::after {
        content: '';
        width: 0;
        left: 50%;
        bottom: 0;
        height: 2px;
        position: absolute;
        background-color: red;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-class:focus::after {
        left: 0;
        width: 100%;
    }


    .custom-root {
        height: 3rem;
        display: flex;
        align-items: end;
        position: relative;
        margin-inline: 1rem;
    }

    .custom-label {
        top: 0;
        left: 0;
        z-index: 1;
        padding: 0;
        font-size: 1rem;
        color: darkgray;
        position: absolute;
        transform-origin: top left;
        transform: translate(0, 22px) scale(1);
        transition: color 200ms cubic-bezier(0, 0, 0.2, 1) 0ms, transform 200ms cubic-bezier(0, 0, 0.2, 1) 0ms;
    }

    .custom-label-top {
        transform: translate(0, 1.5px) scale(0.75);
    }

    .custom-input {
        padding: 0;
        font-size: 1rem;
        font-weight: 900;
    }

    .custom-field {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-field::after {
        content: '';
        width: 0;
        height: 2px;
        border: none;
        position: absolute;
        inset: 100% 0 0 50%;
        background-color: blueviolet;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-focus .custom-field::after {
        left: 0;
        width: 100%;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }
</style>


<BitTextField Style=""box-shadow: aqua 0 0 1rem; margin-inline: 1rem;"" />

<BitTextField Class=""custom-class"" />


<BitTextField Label=""Styles""
              Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                Focused = ""--focused-background: #b2b2b25a;"",
                                FieldGroup = ""background: var(--focused-background);"",
                                Label = ""text-shadow: aqua 0 0 1rem; font-weight: 900; font-size: 1.25rem;"",
                                Input = ""padding: 0.5rem;"" })"" />

<BitTextField @bind-Value=""classesValue""
              Label=""Classes""
              Classes=""@(new() { Root = ""custom-root"",
                                 FieldGroup = ""custom-field"",
                                 Focused = ""custom-focus"",
                                 Input = ""custom-input"",
                                 Label = $""custom-label{(string.IsNullOrEmpty(classesValue) ? string.Empty : "" custom-label-top"")}"" })"" />";
    private readonly string example11CsharpCode = @"
private string? classesValue;";

    private readonly string example12RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>


<EditForm Model=""validationTextFieldModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
    <DataAnnotationsValidator />

    <BitTextField Label=""Required"" Required @bind-Value=""validationTextFieldModel.Text"" />
    <ValidationMessage For=""() => validationTextFieldModel.Text"" />

    <BitTextField Label=""Numeric"" @bind-Value=""validationTextFieldModel.NumericText"" />
    <ValidationMessage For=""() => validationTextFieldModel.NumericText"" />

    <BitTextField Label=""Only chars"" @bind-Value=""validationTextFieldModel.CharacterText"" />
    <ValidationMessage For=""() => validationTextFieldModel.CharacterText"" />

    <BitTextField Label=""Email"" @bind-Value=""validationTextFieldModel.EmailText"" />
    <ValidationMessage For=""() => validationTextFieldModel.EmailText"" />

    <BitTextField Label=""3 < Length < 5"" @bind-Value=""validationTextFieldModel.RangeText"" />
    <ValidationMessage For=""() => validationTextFieldModel.RangeText"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example12CsharpCode = @"
public class ValidationTextFieldModel
{
    [Required(ErrorMessage = ""This field is required."")]
    public string Text { get; set; }

    [RegularExpression(""0*[1-9][0-9]*"", ErrorMessage = ""Only numeric values are allowed."")]
    public string NumericText { get; set; }

    [RegularExpression(""^[a-zA-Z0-9.]*$"", ErrorMessage = ""Only letters(a-z), numbers(0-9), and period(.) are allowed."")]
    public string CharacterText { get; set; }

    [EmailAddress(ErrorMessage = ""Invalid e-mail address."")]
    public string EmailText { get; set; }

    [StringLength(5, MinimumLength = 3, ErrorMessage = ""The text length must be between 3 and 5 chars."")]
    public string RangeText { get; set; }
}

private ValidationTextFieldModel validationTextFieldModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example13RazorCode = @"
<BitTextField Dir=""BitDir.Rtl""
              Placeholder=""پست الکترونیکی""
              IconName=""@BitIconName.EditMail"" />

<BitTextField Underlined 
              Label=""تقویم"" 
              Dir=""BitDir.Rtl""
              IconName=""@BitIconName.Calendar"" />";
}
