using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public partial class BitOtpInputDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Accent",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The accent color of the inputs, applied to the border and the focus ring of the focused input. The error state of the validation still wins over it.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the first input is auto focused on the first render. A component that starts out disabled cannot take the focus, so it is focused on the first render that finds it enabled instead of losing the auto focus altogether.",
        },
        new()
        {
            Name = "AutoShift",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables auto shifting the indexes while clearing the inputs using Delete or Backspace, so the remaining characters move one input to the left instead of leaving a hole in the middle of the code.",
        },
        new()
        {
            Name = "BlurOnFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the focus from the inputs as soon as the code is complete, which is what dismisses the virtual keyboard of a phone once there is nothing left to type.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "InputAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format of the aria-label rendered on each input, where {0} is the one based index of the input and {1} is the Length. Set it to localize the position that screen readers announce for each input. The default is \"{0} of {1}\".",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "Sets the inputmode html attribute of the inputs, which is what decides the virtual keyboard that a phone brings up without changing the element that is rendered or the characters that are accepted. It defaults to the keyboard that matches the Type, so it is only needed to ask for a keyboard the type does not imply, like the telephone keypad (whose keys are larger than the numeric ones on most Android keyboards) for a code of digits.",
            LinkType = LinkType.Link,
            Href = "#input-mode-enum",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label displayed above the inputs. It is rendered as a real label element bound to the first input and it also names the group of the inputs for assistive technologies.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the label displayed above the inputs, taking precedence over Label.",
        },
        new()
        {
            Name = "Length",
            Type = "int",
            DefaultValue = "5",
            Description = "Length of the OTP or number of the inputs. Values below 1 are clamped to 1, changing it at runtime keeps the characters of the inputs that survive the resize, and a value longer than the inputs can hold loses its extra characters instead of being reported as a value that is not shown.",
        },
        new()
        {
            Name = "Mask",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered in place of every filled input, which hides the code without turning the inputs into password inputs, so a masking character of its own (a bullet, an asterisk, an emoji) can be used. The value of the component stays the code that was typed.",
        },
        new()
        {
            Name = "NoSmsAutoFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables both the SMS auto fill of the OTP through the WebOTP API of the browser and the one-time-code autofill of the inputs themselves.",
        },
        new()
        {
            Name = "OnFill",
            Type = "EventCallback<string?>",
            Description = "Callback for when all of the inputs are filled. It is raised once per completed code, so an edit that keeps the very same code does not raise it again.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<(FocusEventArgs Event, int Index)>",
            Description = "onfocusin event callback for each input, receiving the event and the index of the input that raised it.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<(FocusEventArgs Event, int Index)>",
            Description = "onfocusout event callback for each input, receiving the event and the index of the input that raised it.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<(ChangeEventArgs Event, int Index)>",
            Description = "oninput event callback for each input, receiving the event and the index of the input that raised it.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<(KeyboardEventArgs Event, int Index)>",
            Description = "onkeydown event callback for each input, receiving the event and the index of the input that raised it.",
        },
        new()
        {
            Name = "OnPaste",
            Type = "EventCallback<(ClipboardEventArgs Event, int Index)>",
            Description = "onpaste event callback for each input, receiving the event and the index of the input that raised it.",
        },
        new()
        {
            Name = "Pattern",
            Type = "string?",
            DefaultValue = "null",
            Description = "A regular expression that every single character of the code has to match, which is what narrows the code down to a set of characters that no input type covers on its own, like upper case letters or hexadecimal digits. Characters that do not match are rejected while typing and dropped while pasting. An unusable expression is ignored rather than breaking the input.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "The hint text rendered in the empty inputs. A string as long as the Length is spread over the inputs one character each, any other value is rendered in every input as is.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render inputs in the opposite direction. The arrow key navigation flips along with it.",
        },
        new()
        {
            Name = "Separator",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text rendered between the inputs, like a dash or a dot, to make a long code easier to read. It is hidden from assistive technologies and never becomes part of the value.",
        },
        new()
        {
            Name = "SeparatorInterval",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of inputs of each group that the Separator is rendered between, which is how a long code is split into the chunks it is usually printed in, like 123-456. The default is 1, meaning a separator between every pair of inputs. Values below 1 are treated as 1.",
        },
        new()
        {
            Name = "SeparatorTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template rendered between the inputs in place of the Separator text, which is what puts an icon or any other markup between the groups of a code. The context is the zero based index of the input the separator is rendered before, so a template can tell one separator of the row from another. It takes precedence over the Separator.",
        },
        new()
        {
            Name = "SingleTabStop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the whole component into a single stop of the tab order: only the input holding the first character of the code is reachable with the Tab key and the rest are left to the auto advancing focus, the arrow keys and the mouse. Tabbing out of the code then lands on the element after it rather than on its next character.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the inputs.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitOtpInputClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitOtpInput.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Type",
            Type = "BitInputType?",
            DefaultValue = "null",
            Description = "Type of the inputs, which also decides the virtual keyboard of the mobile browsers. The Number type asks for the numeric keypad and rejects every character that is not a digit, whether it is typed or pasted, without rendering a native number input (which would carry spin buttons and report an empty value for characters like e or -).",
            LinkType = LinkType.Link,
            Href = "#input-type-enum",
        },
        new()
        {
            Name = "Uppercase",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns every character of the code into its upper case form as it is typed or pasted, which is what lets a code that is printed in upper case be typed in either case. The conversion happens before the Pattern is applied, so an expression restricted to upper case letters accepts a lower case keystroke instead of rejecting it.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the inputs, which decides how much of the frame around each input is painted: a full fill, only an outline, or just an underline.",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defines whether to render inputs vertically. The arrow key navigation follows the layout.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitOtpInputClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the otp input.",
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the otp input.",
                },
                new()
                {
                    Name = "InputsWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper element of the inputs.",
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each input in otp input.",
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused input in otp input.",
                },
                new()
                {
                    Name = "Filled",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each input that already holds a character in otp input.",
                },
                new()
                {
                    Name = "Separator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the separator rendered between the inputs of the otp input.",
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [

        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "input-type-enum",
            Name = "BitInputType",
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
            Id = "input-mode-enum",
            Name = "BitInputMode",
            Description = "Defines the inputmode html attribute, which is what lets a browser display an appropriate virtual keyboard.",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="No virtual keyboard. For when the page implements its own keyboard input control.",
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
                    Description="A telephone keypad input, including the digits 0–9, the asterisk (*), and the pound (#) key.",
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
        },
        new()
        {
            Id = "variant-enum",
            Name = "BitVariant",
            Description = "Determines the variant of the content that controls the rendered style of the corresponding element(s).",
            Items =
            [
                new() { Name = "Fill", Description = "Fill styled variant.", Value = "0" },
                new() { Name = "Outline", Description = "Outline styled variant.", Value = "1" },
                new() { Name = "Text", Description = "Text styled variant.", Value = "2" }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElements",
            Type = "ElementReference[]",
            Description = "The ElementReferences to the input elements of the BitOtpInput.",
        },
        new()
        {
            Name = "Clear",
            Type = "() => Task",
            Description = "Clears the value of all of the inputs of the BitOtpInput. It does nothing while the component is disabled or read-only.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "(int index = 0) => ValueTask",
            Description = "Gives focus to a specific input element of the BitOtpInput. The index is clamped into the range of the rendered inputs, and calling it before the component has rendered does nothing rather than asking the browser for an element that is not there yet.",
        }
    ];



    private string? maskValue;

    private string? pasteValue;

    private string? oneWayValue;
    private string? twoWayValue;

    private string? onChangeValue;
    private string? onFillValue;
    private (FocusEventArgs Event, int Index)? onFocusInArgs;
    private (FocusEventArgs Event, int Index)? onFocusOutArgs;
    private (ChangeEventArgs Event, int Index)? onInputArgs;
    private (KeyboardEventArgs Event, int Index)? onKeyDownArgs;
    private (ClipboardEventArgs Event, int Index)? onPasteArgs;

    private BitOtpInput? apiOtpInput;
    private async Task HandleClearClick()
    {
        if (apiOtpInput is null) return;

        await apiOtpInput.Clear();
        await apiOtpInput.FocusAsync();
    }

    private ValidationOtpInputModel validationOtpInputModel = new();
    public bool formIsValidSubmit;
    private async Task HandleValidSubmit()
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



    private readonly string example1RazorCode = @"
<BitOtpInput />

<BitOtpInput Length=""4"" />

<BitOtpInput IsEnabled=""false"" DefaultValue=""12345"" />

<BitOtpInput ReadOnly DefaultValue=""12345"" />

<BitOtpInput AutoFocus />

<BitOtpInput AutoShift DefaultValue=""12345"" />

<BitOtpInput BlurOnFill Length=""4"" />";

    private readonly string example2RazorCode = @"
<BitOtpInput Label=""OTP"" />

<BitOtpInput Label=""OTP"" Required />

<BitOtpInput>
    <LabelTemplate>
        <BitStack Horizontal>
            <BitText Gutter><i>Custom label</i></BitText>
            <BitSpacer />
            <BitIcon IconName=""@BitIconName.TemporaryAccessPass"" />
        </BitStack>
    </LabelTemplate>
</BitOtpInput>";

    private readonly string example3RazorCode = @"
<BitOtpInput Label=""Text"" Type=""BitInputType.Text"" />
<BitOtpInput Label=""Number"" Type=""BitInputType.Number"" />
<BitOtpInput Label=""Password"" Type=""BitInputType.Password"" />
<BitOtpInput Label=""Number, with the telephone keypad"" Type=""BitInputType.Number"" InputMode=""BitInputMode.Tel"" />";

    private readonly string example4RazorCode = @"
<BitOtpInput Label=""Bullet"" Mask=""●"" DefaultValue=""12345"" />

<BitOtpInput Label=""Asterisk"" Mask=""*"" Type=""BitInputType.Number"" />

<BitOtpInput Label=""Emoji"" Mask=""🔒"" DefaultValue=""12345"" />

<BitOtpInput Label=""Masked, with the real value below"" Mask=""●"" @bind-Value=""maskValue"" />
<div>Value: @maskValue</div>";
    private readonly string example4CsharpCode = @"
private string? maskValue;";

    private readonly string example5RazorCode = @"
<BitOtpInput Label=""Hexadecimal"" Length=""6"" Pattern=""^[a-fA-F0-9]$"" Placeholder=""0"" />

<BitOtpInput Label=""Upper case letters"" Length=""4"" Pattern=""^[A-Z]$"" Placeholder=""A"" />

<BitOtpInput Label=""Upper case letters, typed in any case"" Length=""4"" Pattern=""^[A-Z]$"" Placeholder=""A"" Uppercase />";

    private readonly string example6RazorCode = @"
<BitOtpInput Label=""Single character"" Placeholder=""•"" />

<BitOtpInput Label=""One character per input"" Length=""6"" Placeholder=""123456"" />";

    private readonly string example7RazorCode = @"
<BitOtpInput Label=""Dash"" Length=""6"" Separator=""-"" />

<BitOtpInput Label=""Dot"" Length=""6"" Separator=""•"" Type=""BitInputType.Number"" />

<BitOtpInput Label=""Grouped by 3"" Length=""6"" Separator=""-"" SeparatorInterval=""3"" />

<BitOtpInput Label=""Grouped by 4"" Length=""8"" Separator=""—"" SeparatorInterval=""4"" Type=""BitInputType.Number"" />

<BitOtpInput Label=""Icon separator"" Length=""6"" SeparatorInterval=""3"" Type=""BitInputType.Number"">
    <SeparatorTemplate>
        <BitIcon IconName=""@BitIconName.Remove"" />
    </SeparatorTemplate>
</BitOtpInput>";

    private readonly string example8RazorCode = @"
<BitOtpInput Label=""Default"" />
<BitOtpInput Label=""Reversed"" Reversed />
<BitOtpInput Label=""Vertical"" Vertical />
<BitOtpInput Label=""Reversed Vertical"" Vertical Reversed />";

    private readonly string example9RazorCode = @"
<BitOtpInput Label=""Verification code"" Length=""6"" />

<BitOtpInput AriaLabel=""Enter the 6 digit code sent to your phone"" Length=""6"" />

<BitOtpInput Label=""Localized announcement"" Length=""6"" InputAriaLabelFormat=""رقم {0} از {1}"" />

<BitOtpInput Label=""Single tab stop"" Length=""6"" SingleTabStop />";

    private readonly string example10RazorCode = @"
<BitOtpInput Label=""Paste a code"" Length=""6"" Type=""BitInputType.Number"" @bind-Value=""pasteValue"" />
<div>Value: @pasteValue</div>

<BitOtpInput Label=""Without the SMS auto fill"" Length=""6"" NoSmsAutoFill />";
    private readonly string example10CsharpCode = @"
private string? pasteValue;";

    private readonly string example11RazorCode = @"
<BitOtpInput Label=""One-way"" Value=""@oneWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""oneWayValue"" />

<BitOtpInput Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""twoWayValue"" />";
    private readonly string example11CsharpCode = @"
private string? oneWayValue;
private string? twoWayValue;";

    private readonly string example12RazorCode = @"
<BitOtpInput Label=""OnChange"" OnChange=""v => onChangeValue = v"" />
<div>OnChange value: @onChangeValue</div>

<BitOtpInput Label=""OnFill"" OnFill=""v => onFillValue = v"" />
<div>OnFill value: @onFillValue</div>

<BitOtpInput Label=""OnFocusIn"" OnFocusIn=""args => onFocusInArgs = args"" />
<div>Focus type: @onFocusInArgs?.Event.Type</div>
<div>Input index: @onFocusInArgs?.Index</div>

<BitOtpInput Label=""OnFocusOut"" OnFocusOut=""args => onFocusOutArgs = args"" />
<div>Focus type: @onFocusOutArgs?.Event.Type</div>
<div>Input index: @onFocusOutArgs?.Index</div>

<BitOtpInput Label=""OnInput"" OnInput=""args => onInputArgs = args"" />
<div>Value: @onInputArgs?.Event.Value</div>
<div>Input index: @onInputArgs?.Index</div>

<BitOtpInput Label=""OnKeyDown"" OnKeyDown=""args => onKeyDownArgs = args"" />
<div>Key & Code: [@onKeyDownArgs?.Event.Key] [@onKeyDownArgs?.Event.Code]</div>
<div>Input index: @onKeyDownArgs?.Index</div>

<BitOtpInput Label=""OnPaste"" OnPaste=""args => onPasteArgs = args"" />
<div>Focus type: @onPasteArgs?.Event.Type</div>
<div>Input index: @onPasteArgs?.Index</div>";
    private readonly string example12CsharpCode = @"
private string? onChangeValue;
private string? onFillValue;
private (FocusEventArgs Event, int Index)? onFocusInArgs;
private (FocusEventArgs Event, int Index)? onFocusOutArgs;
private (ChangeEventArgs Event, int Index)? onInputArgs;
private (KeyboardEventArgs Event, int Index)? onKeyDownArgs;
private (ClipboardEventArgs Event, int Index)? onPasteArgs;";

    private readonly string example13RazorCode = @"
<BitOtpInput @ref=""apiOtpInput"" Label=""OTP"" Length=""6"" DefaultValue=""123456"" />

<BitStack Horizontal FitWidth Gap=""0.5rem"">
    <BitButton OnClick=""() => apiOtpInput?.FocusAsync(0)"">Focus first</BitButton>
    <BitButton OnClick=""() => apiOtpInput?.FocusAsync(5)"">Focus last</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""HandleClearClick"">Clear</BitButton>
</BitStack>";
    private readonly string example13CsharpCode = @"
private BitOtpInput? apiOtpInput;

private async Task HandleClearClick()
{
    if (apiOtpInput is null) return;

    await apiOtpInput.Clear();
    await apiOtpInput.FocusAsync();
}";

    private readonly string example14RazorCode = @"
<style>
    .validation-message {
        color: red;
        font-size: 0.75rem;
    }
</style>

<EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitOtpInput Length=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
    <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />

    <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example14CsharpCode = @"
public class ValidationOtpInputModel
{
    [Required(ErrorMessage = ""The OTP value is required."")]
    [MinLength(6, ErrorMessage = ""Minimum length is 6."")]
    public string OtpValue { get; set; }
}

private ValidationOtpInputModel validationOtpInputModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example15RazorCode = @"
<BitOtpInput Label=""Fill"" Variant=""BitVariant.Fill"" DefaultValue=""12345"" />
<BitOtpInput Label=""Outline"" Variant=""BitVariant.Outline"" DefaultValue=""12345"" />
<BitOtpInput Label=""Text"" Variant=""BitVariant.Text"" DefaultValue=""12345"" />";

    private readonly string example16RazorCode = @"
<BitOtpInput Label=""Primary"" Accent=""BitColor.Primary"" />
<BitOtpInput Label=""Secondary"" Accent=""BitColor.Secondary"" />
<BitOtpInput Label=""Tertiary"" Accent=""BitColor.Tertiary"" />
<BitOtpInput Label=""Info"" Accent=""BitColor.Info"" />
<BitOtpInput Label=""Success"" Accent=""BitColor.Success"" />
<BitOtpInput Label=""Warning"" Accent=""BitColor.Warning"" />
<BitOtpInput Label=""SevereWarning"" Accent=""BitColor.SevereWarning"" />
<BitOtpInput Label=""Error"" Accent=""BitColor.Error"" />";

    private readonly string example17RazorCode = @"
<BitOtpInput Label=""Small"" Size=""BitSize.Small"" />
<BitOtpInput Label=""Medium"" Size=""BitSize.Medium"" />
<BitOtpInput Label=""Large"" Size=""BitSize.Large"" />";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        gap: 1rem;
        margin-inline: 1rem;
    }

    .custom-class input {
        border-radius: 0;
        border-width: 0 0 1px 0;
        border-color: lightseagreen;
    }


    .custom-root {
        margin-inline: 1rem;
    }

    .custom-input {
        border-radius: 50%;
        border: 1px solid tomato;
    }

    .custom-filled {
        background-color: #fff1ed;
    }

    .custom-focused {
        border-color: red;
        box-shadow: tomato 0 0 1rem;
    }

    .custom-separator {
        color: tomato;
        font-weight: 700;
    }
</style>


<BitOtpInput Style=""margin-inline: 1rem; box-shadow: aqua 0 0 0.5rem;"" />

<BitOtpInput Class=""custom-class"" />


<BitOtpInput Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                               Input = ""border-color: blueviolet;"",
                               Filled = ""background-color: #f3e8ff;"",
                               Focused = ""box-shadow: blueviolet 0 0 1rem;"" })"" />

<BitOtpInput Length=""6"" Separator=""-""
             Classes=""@(new() { Root = ""custom-root"",
                                Input = ""custom-input"",
                                Filled = ""custom-filled"",
                                Focused = ""custom-focused"",
                                Separator = ""custom-separator"" })"" />";

    private readonly string example19RazorCode = @"
<BitOtpInput Label=""پیش‌فرض"" Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""معکوس"" Reversed Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""جداکننده"" Length=""6"" Separator=""-"" Dir=""BitDir.Rtl"" />";
}
