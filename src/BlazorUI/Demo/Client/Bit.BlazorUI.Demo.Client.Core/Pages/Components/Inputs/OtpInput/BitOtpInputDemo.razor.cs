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
            Description = "The color of the focused input's border and focus ring, and of the loading bar. The error state wins over it.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Focuses the first empty input on the first render that finds the component enabled.",
        },
        new()
        {
            Name = "AutoShift",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shifts the rest of the code one input back when a character is cleared with Backspace or Delete, instead of leaving a hole.",
        },
        new()
        {
            Name = "AutoSubmit",
            Type = "bool",
            DefaultValue = "false",
            Description = "Submits the enclosing form (a plain form or an EditForm) right after OnFill, the way pressing Enter would: the form still validates first. Nothing happens outside of a form.",
        },
        new()
        {
            Name = "BlurOnFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the focus from the inputs once the code is complete, which dismisses a phone's virtual keyboard.",
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
            Description = "Helper text under the inputs, referenced by the group's aria-describedby. While Invalid or IsLoading is on it is also announced through a live region.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the helper text, taking precedence over Description. It is described the same way but never copied into the live region.",
        },
        new()
        {
            Name = "FullWidth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Stretches the row across its container and shares the width evenly between the inputs. The height stays the one of the Size.",
        },
        new()
        {
            Name = "InputAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "Composite format of each input's aria-label: {0} is the one based position and {1} the Length. Defaults to \"{0} of {1}\".",
        },
        new()
        {
            Name = "InputMode",
            Type = "BitInputMode?",
            DefaultValue = "null",
            Description = "The inputmode attribute of the inputs, which picks the virtual keyboard without changing the accepted characters. Defaults to the one the Type implies.",
            LinkType = LinkType.Link,
            Href = "#input-mode-enum",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Paints the error state and sets aria-invalid without an EditContext, e.g. for a code the server rejected. A failing validation shows the same state on its own.",
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            DefaultValue = "false",
            Description = "The busy state of a submitted code: draws a progress bar, marks the group aria-busy, announces the Description and holds the code still like ReadOnly. Clear is not blocked by it.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label displayed above the inputs, bound to the first input and naming the group of inputs.",
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
            Description = "The number of inputs, which is the length of the code. Values below 1 are treated as 1.",
        },
        new()
        {
            Name = "Lowercase",
            Type = "bool",
            DefaultValue = "false",
            Description = "Converts every character to lower case before the Pattern is applied. Uppercase wins when both are set.",
        },
        new()
        {
            Name = "Mask",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text shown in place of every filled input's character. The value stays the typed code, and a masked code is kept off the clipboard.",
        },
        new()
        {
            Name = "Merged",
            Type = "bool",
            DefaultValue = "false",
            Description = "Glues the inputs of each group (the ones the Separator makes) into a single field with rounding only at its ends.",
        },
        new()
        {
            Name = "NormalizeDigits",
            Type = "bool",
            DefaultValue = "false",
            Description = "Converts the digits of other numbering systems (Persian, Arabic-Indic, full width, ...) to ASCII before the Type and the Pattern are applied.",
        },
        new()
        {
            Name = "NoSmsAutoFill",
            Type = "bool",
            DefaultValue = "false",
            Description = "Turns off the WebOTP SMS auto fill, the one-time-code autocomplete and the password managers' autofill.",
        },
        new()
        {
            Name = "OnFill",
            Type = "EventCallback<string?>",
            Description = "Callback for when all of the inputs are filled, raised once per completed code.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<(FocusEventArgs Event, int Index)>",
            Description = "onfocusin event callback for each input, with the index of the input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<(FocusEventArgs Event, int Index)>",
            Description = "onfocusout event callback for each input, with the index of the input.",
        },
        new()
        {
            Name = "OnInput",
            Type = "EventCallback<(ChangeEventArgs Event, int Index)>",
            Description = "oninput event callback for each input, with the index of the input.",
        },
        new()
        {
            Name = "OnInvalid",
            Type = "EventCallback<(string Value, int Index)>",
            Description = "Callback for when a keystroke, paste or auto fill is rejected in full by the Type, the Pattern or the PasteTransformer, with the rejected text and the index of the input. A paste that only loses some characters does not raise it.",
        },
        new()
        {
            Name = "OnKeyDown",
            Type = "EventCallback<(KeyboardEventArgs Event, int Index)>",
            Description = "onkeydown event callback for each input, with the index of the input.",
        },
        new()
        {
            Name = "OnPaste",
            Type = "EventCallback<(ClipboardEventArgs Event, int Index)>",
            Description = "onpaste event callback for each input, with the index of the input.",
        },
        new()
        {
            Name = "PasteTransformer",
            Type = "Func<string, string>?",
            DefaultValue = "null",
            Description = "Applied to a pasted or auto filled chunk before it is filtered, e.g. to pull the code out of the message around it. An empty result rejects the chunk; an exception leaves it untouched. Not applied to a single typed character.",
        },
        new()
        {
            Name = "Pattern",
            Type = "string?",
            DefaultValue = "null",
            Description = "A regular expression every single character has to match. Non-matching characters are rejected when typed and dropped when pasted; an invalid expression is ignored.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Hint text of the empty inputs. A string exactly Length long is spread one character per input; any other is shown in every input.",
        },
        new()
        {
            Name = "Reversed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the inputs in the opposite order. The arrow keys follow.",
        },
        new()
        {
            Name = "Separator",
            Type = "string?",
            DefaultValue = "null",
            Description = "Text rendered between the groups of inputs. It is hidden from assistive technologies and never part of the value.",
        },
        new()
        {
            Name = "SeparatorInterval",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of inputs in each group the Separator is rendered between, e.g. 3 for 123-456. Values below 1 are treated as 1.",
        },
        new()
        {
            Name = "SeparatorTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template rendered in place of the Separator text, with the zero based index of the next input as its context.",
        },
        new()
        {
            Name = "Sequential",
            Type = "bool",
            DefaultValue = "false",
            Description = "Keeps the code free of holes: focusing, or pasting into, an input past the first empty one lands on that first empty input instead. A complete code is left editable anywhere.",
        },
        new()
        {
            Name = "SingleTabStop",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the whole component a single tab stop: only the first input is reachable with Tab.",
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
            Description = "Type of the inputs, deciding the accepted characters and the virtual keyboard. Number accepts digits only; Number, Email and Url render as text inputs.",
            LinkType = LinkType.Link,
            Href = "#input-type-enum",
        },
        new()
        {
            Name = "Uppercase",
            Type = "bool",
            DefaultValue = "false",
            Description = "Converts every character to upper case before the Pattern is applied.",
        },
        new()
        {
            Name = "Variant",
            Type = "BitVariant?",
            DefaultValue = "null",
            Description = "The visual variant of the inputs: Outline (default), Fill or Text (underline only).",
            LinkType = LinkType.Link,
            Href = "#variant-enum",
        },
        new()
        {
            Name = "Vertical",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the inputs vertically. The arrow keys follow.",
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
                },
                new()
                {
                    Name = "Loader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress bar rendered under the inputs while the otp input is in the loading state.",
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-OtpInput-gap",
            DefaultValue = "0.625rem",
            Description = "Room between the inputs. Merged closes it to 0 whatever this holds.",
        },
        new()
        {
            Name = "--bit-OtpInput-input-size",
            DefaultValue = "--bit-siz-ctrl-sm / --bit-siz-ctrl-md / --bit-siz-ctrl-lg, per Size",
            Description = "Width and height of every input, which is a square of the control height of its size class. FullWidth overrides the width alone.",
        },
        new()
        {
            Name = "--bit-OtpInput-input-width",
            DefaultValue = "--bit-OtpInput-input-size",
            Description = "Width of an input on its own, for a box wider than it is tall.",
        },
        new()
        {
            Name = "--bit-OtpInput-input-height",
            DefaultValue = "--bit-OtpInput-input-size",
            Description = "Height of an input on its own.",
        },
        new()
        {
            Name = "--bit-OtpInput-font-family",
            DefaultValue = "--bit-tpg-font-family",
            Description = "Typeface of the whole component, which is where a tabular or monospaced face for the code is set - the one piece of text in a form that is read character by character.",
        },
        new()
        {
            Name = "--bit-OtpInput-font-size",
            DefaultValue = "--bit-tpg-fs-xs / --bit-tpg-fs-sm / --bit-tpg-fs-md, per Size",
            Description = "Size of the code, inherited by the label and the placeholder.",
        },
        new()
        {
            Name = "--bit-OtpInput-label-font-size",
            DefaultValue = "--bit-OtpInput-font-size",
            Description = "Size of the label above the inputs, which follows the size of the code unless it is set on its own.",
        },
        new()
        {
            Name = "--bit-OtpInput-label-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the label above the inputs.",
        },
        new()
        {
            Name = "--bit-OtpInput-description-font-size",
            DefaultValue = "--bit-tpg-fs-2xs / --bit-tpg-fs-xs / --bit-tpg-fs-sm, per Size",
            Description = "Size of the helper text under the inputs, one step of the type ramp below the code.",
        },
        new()
        {
            Name = "--bit-OtpInput-font-weight",
            DefaultValue = "--bit-tpg-fw-regular",
            Description = "Weight of the character inside an input.",
        },
        new()
        {
            Name = "--bit-OtpInput-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of an input, and of the two ends of every group while Merged is on. The Text variant squares them off whatever this holds.",
        },
        new()
        {
            Name = "--bit-OtpInput-border-width",
            DefaultValue = "--bit-shp-border-width",
            Description = "Thickness of an input's rule, and with it the overlap that glues two Merged inputs together.",
        },
        new()
        {
            Name = "--bit-OtpInput-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the typed character.",
        },
        new()
        {
            Name = "--bit-OtpInput-background",
            DefaultValue = "Per Variant: --bit-clr-bg-pri (Outline), --bit-clr-bg-sec (Fill), transparent (Text)",
            Description = "Input background at rest, and the fallback of the hover and filled backgrounds below.",
        },
        new()
        {
            Name = "--bit-OtpInput-hover-background",
            DefaultValue = "Per Variant: the rest background (Outline, Text), --bit-clr-bg-sec-hover (Fill)",
            Description = "Input background while hovered, on an input that is neither disabled nor read-only.",
        },
        new()
        {
            Name = "--bit-OtpInput-border-color",
            DefaultValue = "Per Variant: --bit-clr-brd-pri (Outline, Text), transparent (Fill)",
            Description = "Input rule at rest, and the fallback of the hover and filled rules below.",
        },
        new()
        {
            Name = "--bit-OtpInput-hover-border-color",
            DefaultValue = "Per Variant: --bit-clr-brd-pri-hover (Outline, Text), the rest rule (Fill)",
            Description = "Input rule while hovered.",
        },
        new()
        {
            Name = "--bit-OtpInput-filled-background",
            DefaultValue = "--bit-OtpInput-background",
            Description = "Background of an input that already holds a character, which is what turns the row into its own progress indicator. It has no parameter behind it.",
        },
        new()
        {
            Name = "--bit-OtpInput-filled-border-color",
            DefaultValue = "--bit-OtpInput-border-color",
            Description = "Rule of an input that already holds a character.",
        },
        new()
        {
            Name = "--bit-OtpInput-focus-border-color",
            DefaultValue = "The Accent role's main color",
            Description = "Input rule while focused.",
        },
        new()
        {
            Name = "--bit-OtpInput-focus-color",
            DefaultValue = "The Accent role's focus color",
            Description = "Color of the keyboard focus ring.",
        },
        new()
        {
            Name = "--bit-OtpInput-placeholder-color",
            DefaultValue = "--bit-clr-fg-ter",
            Description = "Hint character of an empty input.",
        },
        new()
        {
            Name = "--bit-OtpInput-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Label above the inputs.",
        },
        new()
        {
            Name = "--bit-OtpInput-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Helper text under the inputs, outside the error state.",
        },
        new()
        {
            Name = "--bit-OtpInput-separator-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text drawn between the groups of the code.",
        },
        new()
        {
            Name = "--bit-OtpInput-invalid-color",
            DefaultValue = "--bit-clr-err",
            Description = "Input rule, helper text and loading bar while Invalid is on or a validation is failing.",
        },
        new()
        {
            Name = "--bit-OtpInput-invalid-focus-color",
            DefaultValue = "--bit-clr-err-focus",
            Description = "Focus ring color in the error state.",
        },
        new()
        {
            Name = "--bit-OtpInput-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Character, placeholder, label, helper text, separator and loading bar when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-OtpInput-disabled-background",
            DefaultValue = "--bit-clr-bg-dis",
            Description = "Input background when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-OtpInput-disabled-border-color",
            DefaultValue = "--bit-clr-brd-dis",
            Description = "Input rule when IsEnabled is false.",
        },
        new()
        {
            Name = "--bit-OtpInput-loader-color",
            DefaultValue = "The Accent role's main color",
            Description = "The sweep of the bar drawn while IsLoading is on. The error and disabled states paint it with their own color instead.",
        },
        new()
        {
            Name = "--bit-OtpInput-loader-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "The track the sweep of the loading bar travels along.",
        },
        new()
        {
            Name = "--bit-OtpInput-loader-height",
            DefaultValue = "--bit-siz-track-sm",
            Description = "Thickness of the loading bar.",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputElements",
            Type = "ElementReference[]",
            Description = "The ElementReferences to the input elements of the BitOtpInput. The inherited InputElement is the first of them.",
        },
        new()
        {
            Name = "BlurAsync",
            Type = "() => ValueTask",
            Description = "Removes the focus from the input that holds it, dismissing a phone's virtual keyboard. Does nothing when the focus is elsewhere on the page.",
        },
        new()
        {
            Name = "Clear",
            Type = "() => Task",
            Description = "Clears all of the inputs and the value. Does nothing while the component is disabled or read-only.",
        },
        new()
        {
            Name = "FocusAsync",
            Type = "(int index = 0) => ValueTask",
            Description = "Focuses the input at the given index, clamped into range. The inherited FocusAsync() and FocusAsync(bool preventScroll) focus the first input. Does nothing before the first render.",
        }
    ];



    private readonly BitOtpInputParams[] otpInputParams =
    [
        new()
        {
            Length = 6,
            Separator = "-",
            SeparatorInterval = 3,
            Type = BitInputType.Number,
            Variant = BitVariant.Fill,
            NormalizeDigits = true,
            PasteTransformer = v => System.Text.RegularExpressions.Regex.Match(v, @"\p{Nd}{6}").Value,
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

    private bool autoSubmitted;
    private ValidationOtpInputModel autoSubmitOtpInputModel = new();
    private async Task HandleAutoSubmit()
    {
        autoSubmitted = true;

        await Task.Delay(3000);

        autoSubmitted = false;
        autoSubmitOtpInputModel = new();

        StateHasChanged();
    }

    private bool isLoading;
    private bool loadingInvalid;
    private BitOtpInput? loadingOtpInput;
    private string loadingDescription = "Enter the 6 digit code we sent you. Try 123456.";
    private async Task HandleLoadingDemoFill(string? value)
    {
        // The code is on its way to the server, which is what the component says while the answer is
        // awaited: the boxes are held still and the wait is announced along with the group.
        isLoading = true;
        loadingInvalid = false;
        loadingDescription = "Checking the code…";

        await Task.Delay(2000);

        isLoading = false;
        loadingInvalid = value != "123456";

        loadingDescription = loadingInvalid
            ? "That code is not correct or has expired. Try 123456."
            : "That code is correct.";
    }

    private async Task HandleLoadingDemoRetry()
    {
        isLoading = false;
        loadingInvalid = false;
        loadingDescription = "Enter the 6 digit code we sent you. Try 123456.";

        if (loadingOtpInput is null) return;

        await loadingOtpInput.Clear();
        await loadingOtpInput.FocusAsync();
    }
}
