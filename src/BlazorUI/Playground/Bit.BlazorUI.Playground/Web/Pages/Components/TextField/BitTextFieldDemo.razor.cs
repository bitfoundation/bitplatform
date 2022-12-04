using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.TextField;

public partial class BitTextFieldDemo
{
    private string OneWayValue;
    private string TwoWayValue;
    private string OnChangeValue;
    private string ReadOnlyValue = "this is readonly value";

    private string TrimedValue;
    private string NotTrimedValue;

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

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AutoComplete",
            Type = "string?",
            Description = "AutoComplete is a string that maps to the autocomplete attribute of the HTML input element.",
        },
        new ComponentParameter()
        {
            Name = "CanRevealPassword",
            Type = "bool",
            Description = "Whether to show the reveal password button for input type 'password'.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "string?",
            Description = "Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.",
        },
        new ComponentParameter()
        {
            Name = "Description",
            Type = "string?",
            Description = "Description displayed below the text field to provide additional details about what text to enter.",
        },
        new ComponentParameter()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom description for text field.",
        },
        new ComponentParameter()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether or not the text field is borderless.",
        },
        new ComponentParameter()
        {
            Name = "IsMultiline",
            Type = "bool",
            Description = "Whether or not the text field is a Multiline text field.",
        },
        new ComponentParameter()
        {
            Name = "IsReadonly",
            Type = "bool",
            Description = "If true, the text field is readonly.",
        },
        new ComponentParameter()
        {
            Name = "",
            Type = "",
            Description = ".",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            Description = "Whether the associated input is required or not, add an asterisk \"*\" to its label.",
        },
        new ComponentParameter()
        {
            Name = "IsUnderlined",
            Type = "bool",
            Description = "Whether or not the text field is underlined.",
        },
        new ComponentParameter()
        {
            Name = "IsResizable",
            DefaultValue = "true",
            Type = "bool",
            Description = "For multiline text fields, whether or not the field is resizable.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIconName?",
            Description = "The icon name for the icon shown in the far right end of the text field.",
        },
        new ComponentParameter()
        {
            Name = "IsTrim",
            Type = "bool",
            Description = "Specifies whether to remove any leading or trailing whitespace from the value.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string?",
            Description = "Label displayed above the text field and read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom label for text field.",
        },
        new ComponentParameter()
        {
            Name = "MaxLength",
            Type = "int",
            Description = "Specifies the maximum number of characters allowed in the input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            Description = "Callback for when focus moves out of the input.",
        },
        new ComponentParameter()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            Description = "Callback for When a keyboard key is released.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            Description = "Callback for when the input value changes. This is called on both input and change events.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the input clicked.",
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string?",
            Description = "Input placeholder text.",
        },
        new ComponentParameter()
        {
            Name = "Prefix",
            Type = "string?",
            Description = "Prefix displayed before the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new ComponentParameter()
        {
            Name = "PrefixTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom prefix for text field.",
        },
        new ComponentParameter()
        {
            Name = "Rows",
            Type = "int",
            Description = "For multiline text, Number of rows.",
        },
        new ComponentParameter()
        {
            Name = "RevealPasswordAriaLabel",
            Type = "string?",
            Description = "Suffix displayed after the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new ComponentParameter()
        {
            Name = "Suffix",
            Type = "string?",
            Description = "Suffix displayed after the text field contents. This is not included in the value. \r\n Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new ComponentParameter()
        {
            Name = "SuffixTemplate",
            Type = "RenderFragment?",
            Description = "Shows the custom suffix for text field.",
        },
        new ComponentParameter()
        {
            Name = "Type",
            Type = "BitTextFieldType",
            DefaultValue = "BitTextFieldType.Text",
            Description = "Input type.",
            LinkType = LinkType.Link,
            Href = "#text-field-type-enum"
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "text-field-type-enum",
            Title = "BitTextFieldType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Text",
                    Description="The TextField characters are shown as text.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Password",
                    Description="The TextField characters are masked.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Number",
                    Description="The TextField characters are number.",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "Email",
                    Description="The TextField act as an email input.",
                    Value="3",
                },
                new EnumItem()
                {
                    Name= "Tel",
                    Description="The TextField act as a tel input.",
                    Value="4",
                },
                new EnumItem()
                {
                    Name= "Url",
                    Description="The TextField act as a url input.",
                    Value="5",
                }
            }
        }
    };

    #region Sample Code 1

    private readonly string example1HTMLCode = @"
<BitTextField Placeholder=""Enter a text..."" Label=""Basic"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Disabled"" IsEnabled=""false"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Description"" Description=""This is Description"" />
<BitTextField Placeholder=""Enter a text..."" Label=""IsRequired"" IsRequired=""true"" />
<BitTextField Placeholder=""Enter a text..."" Label=""MaxLength: 5"" MaxLength=""5"" />
";

    #endregion

    #region Sample Code 2

    private readonly string example2HTMLCode = @"
<BitTextField Placeholder=""Enter a text..."" Label=""Basic"" IsUnderlined=""true"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Required"" IsUnderlined=""true"" IsRequired=""true"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Disabled"" IsUnderlined=""true"" IsEnabled=""false"" />
";

    #endregion

    #region Sample Code 3

    private readonly string example3HTMLCode = @"
<BitTextField Placeholder=""Enter a text..."" Label=""Has No Border"" HasBorder=""false"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Has No Border and Required"" HasBorder=""false"" IsRequired=""true"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Has No Border and Disabled"" HasBorder=""false"" IsEnabled=""false"" />
";

    #endregion

    #region Sample Code 4

    private readonly string example4HTMLCode = @"
<BitTextField Placeholder=""Enter a text..."" Label=""Multiline and Resizable(by default)"" IsMultiline=""true"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Multiline and is not Resizable"" IsMultiline=""true"" IsResizable=""false"" />
<BitTextField Placeholder=""Enter a text..."" Label=""Multiline and Row count: 10"" IsMultiline=""true"" Rows=""10"" />
";

    #endregion

    #region Sample Code 5

    private readonly string example5HTMLCode = @"
<BitTextField Placeholder=""Enter an email..."" Label=""Email Icon"" IconName=""BitIconName.EditMail"" />
<BitTextField Placeholder=""Enter a date..."" Label=""Calendar Icon"" IconName=""BitIconName.Calendar"" />
";

    #endregion

    #region Sample Code 6

    private readonly string example6HTMLCode = @"
<BitTextField Label=""With Prefix"" Prefix=""https://"" />
<BitTextField Label=""With Suffix"" Suffix="".com"" />
<BitTextField Label=""With Prefix and Suffix"" Prefix=""https://"" Suffix="".com"" />
<BitTextField Label=""Disabled"" Prefix=""https://"" Suffix="".com"" IsEnabled=""false"" />
";

    #endregion

    #region Sample Code 7

    private readonly string example7HTMLCode = @"
<BitTextField Placeholder=""Enter a text..."">
    <LabelTemplate>
        <label style=""color: red;"">This is custom Label</label>
    </LabelTemplate>
</BitTextField>

<BitTextField Placeholder=""Enter a text..."" Label=""This is custom Description"">
    <DescriptionTemplate>
        <span style=""color: red;"">Description</span>
    </DescriptionTemplate>
</BitTextField>

<BitTextField Placeholder=""Enter a text..."" Label=""This is custom Prefix"">
    <PrefixTemplate>
        <span style=""color: red; margin: 0 5px;"">Prefix</span>
    </PrefixTemplate>
</BitTextField>

<BitTextField Placeholder=""Enter a text..."" Label=""This is custom Suffix"">
    <SuffixTemplate>
        <span style=""color: red; margin: 0 5px;"">Suffix</span>
    </SuffixTemplate>
</BitTextField>
";

    #endregion

    #region Sample Code 8

    private readonly string example8HTMLCode = @"
<BitTextField Placeholder=""Enter a password..."" Label=""Password"" Type=""BitTextFieldType.Password"" />
<BitTextField Placeholder=""Enter a password..."" Label=""Can Reveal Password"" Type=""BitTextFieldType.Password"" CanRevealPassword=""true"" />
";

    #endregion

    #region Sample Code 9

    private readonly string example9HTMLCode = @"
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""One-way"" Value=""@OneWayValue"" />
    <BitOtpInput Length=""4"" Style=""margin-top: 5px;"" @bind-Value=""OneWayValue"" />
</div>
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""Two-way"" @bind-Value=""TwoWayValue"" />
    <BitOtpInput Length=""4"" Style=""margin-top: 5px;"" @bind-Value=""TwoWayValue"" />
</div>
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""OnChange"" OnChange=""(v) => OnChangeValue = v"" />
    <span>Value: @OnChangeValue</span>
</div>
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""Readonly"" @bind-Value=""ReadOnlyValue"" IsReadonly=""true"" />
</div>
";

    private readonly string example9CSharpCode = @"
private string OneWayValue;
private string TwoWayValue;
private string OnChangeValue;
private string ReadOnlyValue = ""this is readonly value"";
";

    #endregion

    #region Sample Code 10

    private readonly string example10HTMLCode = @"
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""Trimed"" IsTrim=""true"" @bind-Value=""TrimedValue"" />
    <pre>[@TrimedValue]</pre>
</div>
<div>
    <BitTextField Placeholder=""Enter a text..."" Label=""Not Trimed"" @bind-Value=""NotTrimedValue"" />
    <pre>[@NotTrimedValue]</pre>
</div>
";

    private readonly string example10CSharpCode = @"
private string TrimedValue;
private string NotTrimedValue;
";

    #endregion

    #region Sample Code 11

    private readonly string example11HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationTextFieldModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""form-item"">
            <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""validationTextFieldModel.Text"" />
            <ValidationMessage For=""() => validationTextFieldModel.Text"" />
        </div>
        <div class=""form-item"">
            <BitTextField Label=""Numeric validation"" @bind-Value=""validationTextFieldModel.NumericText"" />
            <ValidationMessage For=""() => validationTextFieldModel.NumericText"" />
        </div>
        <div class=""form-item"">
            <BitTextField Label=""Character validation"" @bind-Value=""validationTextFieldModel.CharacterText"" />
            <ValidationMessage For=""() => validationTextFieldModel.CharacterText"" />
        </div>
        <div class=""form-item"">
            <BitTextField Label=""Email format validation"" @bind-Value=""validationTextFieldModel.EmailText"" />
            <ValidationMessage For=""() => validationTextFieldModel.EmailText"" />
        </div>
        <div class=""form-item"">
            <BitTextField Label=""Length character validation (Min: 3, Max: 5)"" MaxLength=""5"" @bind-Value=""validationTextFieldModel.RangeText"" />
            <ValidationMessage For=""() => validationTextFieldModel.RangeText"" />
        </div>

        <BitButton ButtonType=""BitButtonType.Submit"" Style=""margin-top: 10px;"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form is valid to submit successfully.
    </BitMessageBar>
}
";

    private readonly string example11CSharpCode = @"
public class ValidationTextFieldModel
{
    [Required]
    public string Text { get; set; }

    [RegularExpression(""0*[1-9][0-9]*"", ErrorMessage = ""Only numeric values are allow in field."")]
    public string NumericText { get; set; }

    [RegularExpression(""^[a-zA-Z0-9.]*$"", ErrorMessage = ""Sorry, only letters(a-z), numbers(0-9), and periods(.) are allowed."")]
    public string CharacterText { get; set; }

    [EmailAddress(ErrorMessage = ""Invalid e-mail address."")]
    public string EmailText { get; set; }

    [StringLength(maximumLength: 5, MinimumLength = 3, ErrorMessage = ""The text length much be between 3 and 5 characters in length."")]
    public string RangeText { get; set; }
}

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
";

    #endregion
}
