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
            Description = "If true, the first input left to fill is auto focused on the first render, so a component seeded with a partial code carries on where the typing stopped. A component that starts out disabled cannot take the focus, so it is focused on the first render that finds it enabled instead of losing the auto focus altogether.",
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
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The description (helper text) rendered under the inputs, which the group of the inputs references through its aria-describedby so that screen readers announce it along with the name of the group. It is where the sentence that turns a row of empty boxes into a question the user can answer belongs: where the code was sent, how long it is good for, or what a server that rejected it said.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the description (helper text) rendered under the inputs, which takes precedence over the Description. It is referenced the very same way, so a \"resend the code\" button or a countdown put in here is announced with the group as well.",
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
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Paints the inputs with the error state without an EditContext taking part in it, which is what reports a code that the server has rejected (\"that code is not correct, try again\"): the failure only becomes known once the code has been submitted, so there is nothing for a validator to see. It also marks the inputs with aria-invalid, and a failing validation of an EditContext still shows the very same state on its own.",
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
            Name = "Lowercase",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns every character of the code into its lower case form as it is typed or pasted, the mirror of the Uppercase and applied under the very same rules: before the Pattern is applied, so an expression restricted to lower case letters accepts an upper case keystroke, and to a code that is assigned to the component as much as to one that is typed into it. The Uppercase wins when both are set.",
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
            Name = "NormalizeDigits",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns the digits of the other numbering systems (the Persian ۰۱۲۳, the Arabic-Indic ٠١٢٣, the full width ０１２３ and the rest) into their ASCII form as they are typed or pasted, which is what lets a code that arrives in a message written in the language of the user be typed on the keyboard of that language rather than being rejected as if it were not a number at all. The conversion happens before the Pattern is applied and before the Type rejects what is not a digit, and the value of the component is the ASCII form that a server expects.",
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
            Name = "OnInvalid",
            Type = "EventCallback<(string Value, int Index)>",
            Description = "Callback for when what was typed, pasted or auto filled is rejected in full by the Type or the Pattern, so that nothing of it reaches the inputs. It receives the rejected text along with the index of the input that received it, and it is what turns a silent rejection into a visible one. A paste that only loses some of its characters, like a code copied with the dashes in it, is not a rejection and does not raise it.",
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
            Name = "PasteTransformer",
            Type = "Func<string, string>?",
            DefaultValue = "null",
            Description = "A function applied to a chunk of characters that reaches the component in one go (a paste, an SMS auto fill, or a multi character input event) before anything else is done with it, which is what pulls the code out of the text it was copied inside of. The per character filtering of the Type and the Pattern cannot do that on its own for a code of letters, since the letters of the words around it match just as well as the ones of the code: \"your code is A1B2C3\" would fill the inputs with \"YOURCODEI\". Returning an empty string rejects the chunk, which raises OnInvalid. It is not applied to a single typed character, and an exception thrown out of it leaves the chunk untouched rather than breaking the input.",
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
            Name = "Sequential",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the code free of holes: giving the focus to an input that sits after the first empty one, by clicking it or with an arrow key, moves the focus to that first empty input instead, and a chunk of characters that arrives at once (a paste or an auto fill) cannot land past it either, so the code is always filled from its start onwards. Without it a character typed into the middle of an empty row is reported as if it were the first one of the code, since the value is the characters of the inputs joined together and an empty input contributes nothing to it. A complete code is left alone, so any of its characters can still be clicked and corrected.",
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
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description (helper text) of the otp input.",
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
            Name = "BlurAsync",
            Type = "() => ValueTask",
            Description = "Removes the focus from the input of the BitOtpInput that currently holds it, which is what dismisses the virtual keyboard of a phone. Nothing happens when the focus is somewhere else on the page, so a component that filled itself in the background never takes it away from what the user is doing.",
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



    private string? normalizeDigitsValue;

    private string? maskValue;

    private string? pasteValue;
    private string? transformedPasteValue;

    private string? oneWayValue;
    private string? twoWayValue;

    private string? onChangeValue;
    private string? onFillValue;
    private (string Value, int Index)? onInvalidArgs;
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

    private bool invalidState;
    private BitOtpInput? invalidOtpInput;
    private string invalidDescription = "Enter the 6 digit code we sent you. Try 123456.";
    private void HandleInvalidDemoFill(string? value)
    {
        // Only the server that issued the code knows whether it is the right one, so the error state is
        // set from the answer it gives rather than from a validator, and the answer itself goes into the
        // description, which the group of the inputs is described by.
        invalidState = value != "123456";

        invalidDescription = invalidState
            ? "That code is not correct or has expired. Try 123456."
            : "That code is correct.";
    }

    private async Task HandleInvalidDemoRetry()
    {
        invalidState = false;
        invalidDescription = "Enter the 6 digit code we sent you. Try 123456.";

        if (invalidOtpInput is null) return;

        await invalidOtpInput.Clear();
        await invalidOtpInput.FocusAsync();
    }



    private readonly string example1RazorCode = @"
<BitOtpInput />

<BitOtpInput Length=""4"" />

<BitOtpInput IsEnabled=""false"" DefaultValue=""12345"" />

<BitOtpInput ReadOnly DefaultValue=""12345"" />

<BitOtpInput AutoFocus />

<BitOtpInput AutoShift DefaultValue=""12345"" />

<BitOtpInput BlurOnFill Length=""4"" />

<BitOtpInput Sequential Length=""6"" Type=""BitInputType.Number"" />";

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
</BitOtpInput>

<BitOtpInput Label=""Verification code"" Length=""6"" Type=""BitInputType.Number""
             Description=""We sent a 6 digit code to +1 555 0100. It stays valid for 10 minutes."" />

<BitOtpInput Label=""Verification code"" Length=""6"" Type=""BitInputType.Number"">
    <DescriptionTemplate>
        <BitStack Horizontal FitWidth Gap=""0.25rem"" VerticalAlign=""BitAlignment.Center"">
            <BitText Typography=""BitTypography.Caption1"">Didn't get it?</BitText>
            <BitLink Href=""#example2"">Send it again</BitLink>
        </BitStack>
    </DescriptionTemplate>
</BitOtpInput>";

    private readonly string example3RazorCode = @"
<BitOtpInput Label=""Text"" Type=""BitInputType.Text"" />
<BitOtpInput Label=""Number"" Type=""BitInputType.Number"" />
<BitOtpInput Label=""Password"" Type=""BitInputType.Password"" />
<BitOtpInput Label=""Number, with the telephone keypad"" Type=""BitInputType.Number"" InputMode=""BitInputMode.Tel"" />

<BitOtpInput Label=""Number, typed or pasted in any numbering system"" Length=""6""
             Type=""BitInputType.Number"" NormalizeDigits @bind-Value=""normalizeDigitsValue"" />
<div>Value: @normalizeDigitsValue</div>";
    private readonly string example3CsharpCode = @"
private string? normalizeDigitsValue;";

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

<BitOtpInput Label=""Upper case letters, typed in any case"" Length=""4"" Pattern=""^[A-Z]$"" Placeholder=""A"" Uppercase />

<BitOtpInput Label=""Lower case letters, typed in any case"" Length=""4"" Pattern=""^[a-z]$"" Placeholder=""a"" Lowercase />";

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

<BitOtpInput Label=""Single tab stop"" Length=""6"" SingleTabStop />

<BitOtpInput Label=""Described group"" Length=""6""
             Description=""Enter the code from the text message we sent to +1 555 0100."" />";

    private readonly string example10RazorCode = @"
<BitOtpInput Label=""Paste a code"" Length=""6"" Type=""BitInputType.Number"" @bind-Value=""pasteValue"" />
<div>Value: @pasteValue</div>

<BitOtpInput Label=""Alphanumeric, pulled out of the message"" Length=""6"" Uppercase
             PasteTransformer=""@(v => Regex.Match(v, ""[A-Za-z0-9]{6}"").Value)""
             @bind-Value=""transformedPasteValue"" />
<div>Value: @transformedPasteValue</div>

<BitOtpInput Label=""Without the SMS auto fill"" Length=""6"" NoSmsAutoFill />";
    private readonly string example10CsharpCode = @"
private string? pasteValue;
private string? transformedPasteValue;";

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

<BitOtpInput Label=""OnInvalid (digits only)"" Type=""BitInputType.Number"" OnInvalid=""args => onInvalidArgs = args"" />
<div>Rejected: @onInvalidArgs?.Value</div>
<div>Input index: @onInvalidArgs?.Index</div>

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
private (string Value, int Index)? onInvalidArgs;
private (FocusEventArgs Event, int Index)? onFocusInArgs;
private (FocusEventArgs Event, int Index)? onFocusOutArgs;
private (ChangeEventArgs Event, int Index)? onInputArgs;
private (KeyboardEventArgs Event, int Index)? onKeyDownArgs;
private (ClipboardEventArgs Event, int Index)? onPasteArgs;";

    private readonly string example13RazorCode = @"
<BitOtpInput @ref=""apiOtpInput"" Label=""OTP"" Length=""6"" DefaultValue=""123456"" />

<BitStack Horizontal FitWidth Gap=""0.5rem"" Wrap>
    <BitButton OnClick=""() => apiOtpInput?.FocusAsync(0)"">Focus first</BitButton>
    <BitButton OnClick=""() => apiOtpInput?.FocusAsync(5)"">Focus last</BitButton>
    <BitButton Variant=""BitVariant.Outline"" OnClick=""() => apiOtpInput?.BlurAsync()"">Blur</BitButton>
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
<BitOtpInput @ref=""invalidOtpInput"" Label=""Verification code"" Length=""6""
             Type=""BitInputType.Number""
             Invalid=""invalidState""
             Description=""@invalidDescription""
             OnFill=""HandleInvalidDemoFill"" />

<BitStack Horizontal FitWidth Gap=""0.5rem"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""HandleInvalidDemoRetry"">Clear & retry</BitButton>
</BitStack>";
    private readonly string example15CsharpCode = @"
private bool invalidState;
private BitOtpInput? invalidOtpInput;
private string invalidDescription = ""Enter the 6 digit code we sent you. Try 123456."";

private void HandleInvalidDemoFill(string? value)
{
    // Only the server that issued the code knows whether it is the right one, so the error state is
    // set from the answer it gives rather than from a validator, and the answer itself goes into the
    // description, which the group of the inputs is described by.
    invalidState = value != ""123456"";

    invalidDescription = invalidState
        ? ""That code is not correct or has expired. Try 123456.""
        : ""That code is correct."";
}

private async Task HandleInvalidDemoRetry()
{
    invalidState = false;
    invalidDescription = ""Enter the 6 digit code we sent you. Try 123456."";

    if (invalidOtpInput is null) return;

    await invalidOtpInput.Clear();
    await invalidOtpInput.FocusAsync();
}";

    private readonly string example16RazorCode = @"
<BitOtpInput Label=""Fill"" Variant=""BitVariant.Fill"" DefaultValue=""12345"" />
<BitOtpInput Label=""Outline"" Variant=""BitVariant.Outline"" DefaultValue=""12345"" />
<BitOtpInput Label=""Text"" Variant=""BitVariant.Text"" DefaultValue=""12345"" />";

    private readonly string example17RazorCode = @"
<BitOtpInput Label=""Primary"" Accent=""BitColor.Primary"" />
<BitOtpInput Label=""Secondary"" Accent=""BitColor.Secondary"" />
<BitOtpInput Label=""Tertiary"" Accent=""BitColor.Tertiary"" />
<BitOtpInput Label=""Info"" Accent=""BitColor.Info"" />
<BitOtpInput Label=""Success"" Accent=""BitColor.Success"" />
<BitOtpInput Label=""Warning"" Accent=""BitColor.Warning"" />
<BitOtpInput Label=""SevereWarning"" Accent=""BitColor.SevereWarning"" />
<BitOtpInput Label=""Error"" Accent=""BitColor.Error"" />";

    private readonly string example18RazorCode = @"
<BitOtpInput Label=""Small"" Size=""BitSize.Small"" />
<BitOtpInput Label=""Medium"" Size=""BitSize.Medium"" />
<BitOtpInput Label=""Large"" Size=""BitSize.Large"" />";

    private readonly string example19RazorCode = @"
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

    .custom-label {
        color: tomato;
        letter-spacing: 0.1rem;
    }

    .custom-description {
        color: tomato;
    }

    .custom-wrapper {
        gap: 0.25rem;
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


<BitOtpInput Label=""Styles"" Description=""Every part of the component has a slot of its own.""
             Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                               Label = ""color: blueviolet; letter-spacing: 0.1rem;"",
                               Description = ""color: blueviolet;"",
                               InputsWrapper = ""gap: 0.25rem;"",
                               Input = ""border-color: blueviolet;"",
                               Filled = ""background-color: #f3e8ff;"",
                               Focused = ""box-shadow: blueviolet 0 0 1rem;"" })"" />

<BitOtpInput Label=""Classes"" Length=""6"" Separator=""-""
             Description=""Every part of the component has a slot of its own.""
             Classes=""@(new() { Root = ""custom-root"",
                                Label = ""custom-label"",
                                Description = ""custom-description"",
                                InputsWrapper = ""custom-wrapper"",
                                Input = ""custom-input"",
                                Filled = ""custom-filled"",
                                Focused = ""custom-focused"",
                                Separator = ""custom-separator"" })"" />";

    private readonly string example20RazorCode = @"
<BitOtpInput Label=""پیش‌فرض"" Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""معکوس"" Reversed Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""جداکننده"" Length=""6"" Separator=""-"" Dir=""BitDir.Rtl"" />";
}
