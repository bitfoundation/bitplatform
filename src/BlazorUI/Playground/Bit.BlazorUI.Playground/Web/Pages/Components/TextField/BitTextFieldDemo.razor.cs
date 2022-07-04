using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.TextField;

public partial class BitTextFieldDemo
{
    private BitTextFieldType InputType = BitTextFieldType.Password;
    private string TextValue;
    private string NonTrimmedTextValue;
    private string TrimmedTextValue;
    private ValidationTextFieldModel validationTextFieldModel = new();
    public bool formIsValidSubmit;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "CanRevealPassword",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to show the reveal password button for input type 'password'.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "string",
            DefaultValue = "",
            Description = "Default value of the text field. Only provide this if the text field is an uncontrolled component; otherwise, use the value property.",
        },
        new ComponentParameter()
        {
            Name = "Description",
            Type = "string",
            DefaultValue = "",
            Description = "Description displayed below the text field to provide additional details about what text to enter.",
        },
        new ComponentParameter()
        {
            Name = "DescriptionFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Shows the custom description for text field.",
        },
        new ComponentParameter()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is borderless.",
        },
        new ComponentParameter()
        {
            Name = "IconName",
            Type = "BitIcon",
            DefaultValue = "",
            Description = "The icon name for the icon shown in the far right end of the text field.",
        },
        new ComponentParameter()
        {
            Name = "IsMultiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is a Multiline text field.",
        },
        new ComponentParameter()
        {
            Name = "IsReadonly",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the text field is readonly.",
        },
        new ComponentParameter()
        {
            Name = "IsResizable",
            Type = "bool",
            DefaultValue = "false",
            Description = "For multiline text fields, whether or not the field is resizable.",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated input is required or not, add an asterisk '*' to its label.",
        },
        new ComponentParameter()
        {
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the text field is underlined.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Label displayed above the text field and read by screen readers.",
        },
        new ComponentParameter()
        {
            Name = "LabelFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Shows the custom label for text field.",
        },
        new ComponentParameter()
        {
            Name = "MaxLength",
            Type = "int",
            DefaultValue = "-1",
            Description = "Specifies the maximum number of characters allowed in the input.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Callback for when the input value changes. This is called on both input and change events.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the input clicked.",
        },
        new ComponentParameter()
        {
            Name = "OnFocus",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<KeyboardEventArgs>",
            DefaultValue = "",
            Description = "Callback for when a keyboard key is pressed.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves out of the input.",
        },
        new ComponentParameter()
        {
            Name = "OnKeyUp",
            Type = "EventCallback<KeyboardEventArgs>",
            DefaultValue = "",
            Description = "Callback for When a keyboard key is released.",
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "",
            Description = "Input placeholder text.",
        },
        new ComponentParameter()
        {
            Name = "Prefix",
            Type = "string",
            DefaultValue = "",
            Description = "Prefix displayed before the text field contents. This is not included in the value. Ensure a descriptive label is present to assist screen readers, as the value does not include the prefix.",
        },
        new ComponentParameter()
        {
            Name = "PrefixFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Shows the custom prefix for text field.",
        },
        new ComponentParameter()
        {
            Name = "Rows",
            Type = "int",
            DefaultValue = "3",
            Description = "For multiline text, Number of rows.",
        },
        new ComponentParameter()
        {
            Name = "RvealPasswordAriaLabel",
            Type = "string",
            DefaultValue = "",
            Description = "Aria label for the reveal password button.",
        },
        new ComponentParameter()
        {
            Name = "Suffix",
            Type = "string",
            DefaultValue = "",
            Description = "Suffix displayed after the text field contents. Ensure a descriptive label is present to assist screen readers, as the value does not include the suffix.",
        },
        new ComponentParameter()
        {
            Name = "SuffixFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Shows the custom suffix for text field.",
        },
        new ComponentParameter()
        {
            Name = "Type",
            Type = "BitTextFieldType",
            LinkType = LinkType.Link,
            Href = "#text-field-type-enum",
            DefaultValue = "BitTextFieldType.text",
            Description = "Input type.",
        },
        new ComponentParameter()
        {
            Name = "Trim",
            Type = "bool",
            DefaultValue = "false",
            Description = "Specifies whether to remove any leading or trailing whitespace from the value.",
        },
        new ComponentParameter()
        {
            Name = "CurrentValue",
            Type = "string",
            DefaultValue = "",
            Description = "Current value of the text field.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Callback for when the input value changes.",
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

    private readonly string example1HTMLCode = @"<div>
    <BitTextField Label=""Standard""></BitTextField>
</div>
<div>
    <BitTextField Label=""Standard With Two-Way Binding"" @bind-Value=""@TextValue""></BitTextField>
    <BitLabel>You are typing : @TextValue </BitLabel>
</div>
<div>";

    private readonly string example1CSharpCode = @"
private string TextValue;
";

    private readonly string example2HTMLCode = @"<div>
    <BitTextField Label=""Read-only"" IsReadonly=""true"" DefaultValue=""I am read-only""></BitTextField>
</div>";

    private readonly string example3HTMLCode = @"<div>
    <BitTextField Label=""With Placeholder"" Placeholder=""Please enter text here""></BitTextField>
</div>";

    private readonly string example4HTMLCode = @"<div>
    <BitTextField Label=""Disabled"" IsEnabled=""false"" DefaultValue=""I am disabled""></BitTextField>
</div>
<div>
    <BitTextField Label=""Disabled With Placeholder"" IsEnabled=""false"" Placeholder=""I am disabled""></BitTextField>
</div>";

    private readonly string example5HTMLCode = @"<div>
    <BitTextField Label=""Controlled TextField Limiting Length Of Value To 10"" MaxLength=""10""></BitTextField>
</div>";

    private readonly string example6HTMLCode = @"<div>
    <BitTextField Label=""With An Icon"" IconName=""BitIconName.CalendarMirrored""></BitTextField>
</div>";

    private readonly string example7HTMLCode = @"<div>
    <BitTextField Label=""Password With Reveal Button""
                  Type=""@InputType""
                  CanRevealPassword=""true""></BitTextField>
</div>";

    private readonly string example7CSharpCode = @"

private BitTextFieldType InputType = BitTextFieldType.Password;
";

    private readonly string example8HTMLCode = @"<div>
    <BitTextField Label=""Required"" IsRequired=""true""></BitTextField>
</div>
<div>
    <BitTextField AriaLabel=""Required Without Visible Label"" IsRequired=""true""></BitTextField>
</div>";

    private readonly string example9HTMLCode = @"<div>
    <BitTextField Label=""Standard"" IsMultiline=""true"" Rows=""3""></BitTextField>
</div>
<div>
    <BitTextField Label=""Limited multi-line text field - 10 chars"" IsMultiline=""true"" MaxLength=""10""></BitTextField>
</div>
<div>
    <BitTextField Label=""Disabled"" IsMultiline=""true"" IsEnabled=""false"" Value=""Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat""></BitTextField>
</div>
<div>
    <BitTextField Label=""Non-resizable"" IsMultiline=""true"" IsResizable=""false""></BitTextField>
</div>";

    private readonly string example10HTMLCode = @"<div>
    <BitTextField Label=""Standard:"" IsUnderlined=""true""></BitTextField>
</div>
<div>
    <BitTextField Label=""Disabled:"" IsUnderlined=""true"" IsEnabled=""false"" DefaultValue=""I am disabled""></BitTextField>
</div>
<div>
    <BitTextField Label=""Required:"" IsUnderlined=""true"" IsRequired=""true""></BitTextField>
</div>
<div>
    <BitTextField Label=""Borderless single-line TextField"" HasBorder=""false""></BitTextField>
</div>
<div>
    <BitTextField Label=""Borderless multi-line TextField"" IsMultiline=""true"" HasBorder=""false""></BitTextField>
</div>";

    private readonly string example11HTMLCode = @"<div>
    <BitTextField Label=""With Prefix"" Prefix=""https://""></BitTextField>
</div>
<div>
    <BitTextField Label=""With Suffix"" Suffix="".com""></BitTextField>
</div>
<div>
    <BitTextField Label=""Disabled With Prefix"" Prefix=""https://"" IsEnabled=""false""></BitTextField>
</div>
<div>
    <BitTextField Label=""With Prefix And Suffix"" Prefix=""https://"" Suffix="".com""></BitTextField>
</div>";

    private readonly string example12HTMLCode = @"<div>
    <BitTextField Description=""Click the (i) icon!"">
        <LabelFragment>
            <BitLabel Style=""display:inline-block;padding-bottom:10px;"">With PrefixCustom Label Rendering</BitLabel>
            <BitIconButton IconName=""BitIconName.Info""></BitIconButton>
        </LabelFragment>
    </BitTextField>
</div>
<div class=""m-t-15"">
    <BitTextField>
        <DescriptionFragment>
            <BitLabel Style=""color:green;"">With PrefixCustom Label Rendering</BitLabel>
        </DescriptionFragment>
    </BitTextField>
</div>";

    private readonly string example13HTMLCode = @"@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationTextFieldModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""form-item"">
            <BitTextField Label=""Required"" @bind-Value=""validationTextFieldModel.Text"" />
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
            <BitTextField Label=""Email validation"" @bind-Value=""validationTextFieldModel.EmailText"" />
            <ValidationMessage For=""() => validationTextFieldModel.EmailText"" />
        </div>
        <div class=""form-item"">
            <BitTextField Label=""Length character validation"" @bind-Value=""validationTextFieldModel.RangeText"" />
            <ValidationMessage For=""() => validationTextFieldModel.RangeText"" />
        </div>
        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form is valid to submit successfully.
    </BitMessageBar>
}";

    private readonly string example13CSharpCode = @"private ValidationTextFieldModel validationTextFieldModel = new();
public bool formIsValidSubmit;
	
private async void HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(3000);

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}

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

    [StringLength(5, MinimumLength = 3, ErrorMessage = ""The text length much be between 3 and 5 characters in length."")]
    public string RangeText { get; set; }
}";

    private readonly string example14HTMLCode = @"<div class=""example-desc"">The trim property removes whitespaces from both sides of a string.</div>
<BitTextField Trim=""true""
              Label=""With Trim""
              @bind-Value=""TrimmedTextValue""></BitTextField>
    <BitTextField @bind-Value=""NonTrimmedTextValue""></BitTextField>
    <pre>type with trim: [@TrimmedTextValue]</pre>
    <pre>type without trim: [@NonTrimmedTextValue]</pre>";

    private readonly string example14CSharpCode = @"private string NonTrimmedTextValue;
private string TrimmedTextValue;";

    private async void HandleValidSubmit()
    {
        formIsValidSubmit = true;

        await Task.Delay(3000);

        formIsValidSubmit = false;

        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        formIsValidSubmit = false;
    }
}
