namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public partial class BitOtpInputDemo
{
    private readonly string example1RazorCode = @"
<BitOtpInput />

<BitOtpInput Length=""4"" />

<BitOtpInput IsEnabled=""false"" DefaultValue=""12345"" />

<BitOtpInput ReadOnly DefaultValue=""12345"" />

<BitOtpInput AutoFocus />

<BitOtpInput AutoShift DefaultValue=""12345"" />

<BitOtpInput BlurOnFill Length=""4"" />

<BitOtpInput Sequential Length=""6"" />";

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

<BitOtpInput Label=""Verification code"" Length=""6""
             Description=""We sent a 6 digit code to +1 555 0100. It stays valid for 10 minutes."" />

<BitOtpInput Label=""Verification code"" Length=""6"">
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
<BitOtpInput Label=""A hint in every empty box"" Placeholder=""•"" />

<BitOtpInput Label=""One hint character per box"" Length=""6"" Placeholder=""123456"" />

<BitOtpInput Label=""Bullet"" Mask=""●"" DefaultValue=""12345"" />

<BitOtpInput Label=""Asterisk, over a hint"" Mask=""*"" Placeholder=""0"" Type=""BitInputType.Number"" />

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
<BitOtpInput Label=""Dash"" Length=""6"" Separator=""-"" />

<BitOtpInput Label=""Dot"" Length=""6"" Separator=""•"" Type=""BitInputType.Number"" />

<BitOtpInput Label=""Grouped by 3"" Length=""6"" Separator=""-"" SeparatorInterval=""3"" />

<BitOtpInput Label=""Grouped by 4"" Length=""8"" Separator=""—"" SeparatorInterval=""4"" Type=""BitInputType.Number"" />

<BitOtpInput Label=""Icon separator"" Length=""6"" SeparatorInterval=""3"" Type=""BitInputType.Number"">
    <SeparatorTemplate>
        <BitIcon IconName=""@BitIconName.Remove"" />
    </SeparatorTemplate>
</BitOtpInput>";

    private readonly string example7RazorCode = @"
<BitOtpInput Label=""Fill"" Variant=""BitVariant.Fill"" DefaultValue=""12345"" />
<BitOtpInput Label=""Outline"" Variant=""BitVariant.Outline"" DefaultValue=""12345"" />
<BitOtpInput Label=""Text"" Variant=""BitVariant.Text"" DefaultValue=""12345"" />";

    private readonly string example8RazorCode = @"
<BitOtpInput Label=""Default"" />
<BitOtpInput Label=""Reversed"" Reversed />
<BitOtpInput Label=""Vertical"" Vertical />
<BitOtpInput Label=""Reversed Vertical"" Vertical Reversed />

<div style=""max-width: 24rem;"">
    <BitOtpInput Label=""FullWidth"" Length=""6"" FullWidth Type=""BitInputType.Number"" />

    <BitOtpInput Label=""FullWidth, grouped"" Length=""6"" FullWidth Separator=""-"" SeparatorInterval=""3""
                 Type=""BitInputType.Number"" />
</div>";

    private readonly string example9RazorCode = @"
<BitOtpInput Label=""Merged"" Length=""6"" Merged Type=""BitInputType.Number"" />

<BitOtpInput Label=""Merged, grouped by 3"" Length=""6"" Merged Separator=""-"" SeparatorInterval=""3""
             Type=""BitInputType.Number"" />

<BitOtpInput Label=""Merged, filled"" Length=""6"" Merged Variant=""BitVariant.Fill""
             Type=""BitInputType.Number"" DefaultValue=""123456"" />

<BitOtpInput Label=""Merged, underlined"" Length=""6"" Merged Variant=""BitVariant.Text""
             Type=""BitInputType.Number"" />

<BitOtpInput Label=""Merged, vertical"" Length=""4"" Merged Vertical Type=""BitInputType.Number"" />

<BitOtpInput Label=""Merged, reversed"" Length=""4"" Merged Reversed Type=""BitInputType.Number"" />";

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

@if (formIsValidSubmit is false)
{
    <EditForm Model=""validationOtpInputModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <BitOtpInput Label=""Submitted by hand"" Length=""6"" @bind-Value=""validationOtpInputModel.OtpValue"" />
        <ValidationMessage For=""() => validationOtpInputModel.OtpValue"" />

        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">
        The form submitted successfully.
    </BitMessage>
}


@if (autoSubmitted is false)
{
    <EditForm Model=""autoSubmitOtpInputModel"" OnValidSubmit=""HandleAutoSubmit"">
        <DataAnnotationsValidator />

        <BitOtpInput Label=""Submitted on fill"" Length=""6"" Type=""BitInputType.Number"" AutoSubmit
                     @bind-Value=""autoSubmitOtpInputModel.OtpValue"" />
        <ValidationMessage For=""() => autoSubmitOtpInputModel.OtpValue"" />
    </EditForm>
}
else
{
    <BitMessage Color=""BitColor.Success"">
        Submitted on fill: @autoSubmitOtpInputModel.OtpValue
    </BitMessage>
}";
    private readonly string example14CsharpCode = @"
public class ValidationOtpInputModel
{
    [Required(ErrorMessage = ""The OTP value is required."")]
    [MinLength(6, ErrorMessage = ""Minimum length is 6."")]
    public string OtpValue { get; set; }
}

private bool formIsValidSubmit;
private ValidationOtpInputModel validationOtpInputModel = new();

private void HandleValidSubmit()
{
    formIsValidSubmit = true;
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}


private bool autoSubmitted;
private ValidationOtpInputModel autoSubmitOtpInputModel = new();

private void HandleAutoSubmit()
{
    autoSubmitted = true;
}";

    private readonly string example15RazorCode = @"
<BitOtpInput @ref=""loadingOtpInput"" Label=""Verification code"" Length=""6""
             Type=""BitInputType.Number""
             Invalid=""loadingInvalid""
             IsLoading=""isLoading""
             Description=""@loadingDescription""
             OnFill=""HandleLoadingDemoFill"" />

<BitStack Horizontal FitWidth Gap=""0.5rem"">
    <BitButton Variant=""BitVariant.Outline"" OnClick=""HandleLoadingDemoRetry"">Clear & retry</BitButton>
</BitStack>";
    private readonly string example15CsharpCode = @"
private bool isLoading;
private bool loadingInvalid;
private BitOtpInput? loadingOtpInput;
private string loadingDescription = ""Enter the 6 digit code we sent you. Try 123456."";

private async Task HandleLoadingDemoFill(string? value)
{
    isLoading = true;
    loadingInvalid = false;
    loadingDescription = ""Checking the code…"";

    await Task.Delay(2000); // the server checking the code

    isLoading = false;
    loadingInvalid = value != ""123456"";

    loadingDescription = loadingInvalid
        ? ""That code is not correct or has expired. Try 123456.""
        : ""That code is correct."";
}

private async Task HandleLoadingDemoRetry()
{
    isLoading = false;
    loadingInvalid = false;
    loadingDescription = ""Enter the 6 digit code we sent you. Try 123456."";

    if (loadingOtpInput is null) return;

    await loadingOtpInput.Clear();
    await loadingOtpInput.FocusAsync();
}";

    private readonly string example16RazorCode = @"
<BitOtpInput Label=""Verification code"" Length=""6"" />

<BitOtpInput AriaLabel=""Enter the 6 digit code sent to your phone"" Length=""6"" />

<BitOtpInput Label=""Localized announcement"" Length=""6"" InputAriaLabelFormat=""رقم {0} از {1}"" />

<BitOtpInput Label=""Single tab stop"" Length=""6"" SingleTabStop />

<BitOtpInput Label=""Described group"" Length=""6""
             Description=""Enter the code from the text message we sent to +1 555 0100."" />";

    private readonly string example17RazorCode = @"
<BitParams Parameters=""@otpInputParams"">
    <BitOtpInput Label=""Takes the length, the type, the separator and the variant from the cascade"" />

    <BitOtpInput Label=""So does this one, without repeating any of it"" />

    <BitOtpInput Label=""Its own Length, the cascaded rest"" Length=""4"" />
</BitParams>

<BitOtpInput Label=""Outside the cascade, and back to the defaults"" />";
    private readonly string example17CsharpCode = @"
private readonly BitOtpInputParams[] otpInputParams =
[
    new()
    {
        Length = 6,
        Separator = ""-"",
        SeparatorInterval = 3,
        Type = BitInputType.Number,
        Variant = BitVariant.Fill,
        NormalizeDigits = true,
        PasteTransformer = v => System.Text.RegularExpressions.Regex.Match(v, @""\p{Nd}{6}"").Value,
    }
];";

    private readonly string example18RazorCode = @"
<BitOtpInput Label=""Primary"" Accent=""BitColor.Primary"" />
<BitOtpInput Label=""Secondary"" Accent=""BitColor.Secondary"" />
<BitOtpInput Label=""Tertiary"" Accent=""BitColor.Tertiary"" />
<BitOtpInput Label=""Info"" Accent=""BitColor.Info"" />
<BitOtpInput Label=""Success"" Accent=""BitColor.Success"" />
<BitOtpInput Label=""Warning"" Accent=""BitColor.Warning"" />
<BitOtpInput Label=""SevereWarning"" Accent=""BitColor.SevereWarning"" />
<BitOtpInput Label=""Error"" Accent=""BitColor.Error"" />";

    private readonly string example19RazorCode = @"
<BitOtpInput Label=""Small"" Size=""BitSize.Small"" />
<BitOtpInput Label=""Medium"" Size=""BitSize.Medium"" />
<BitOtpInput Label=""Large"" Size=""BitSize.Large"" />";

    private readonly string example20RazorCode = @"
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
                                Separator = ""custom-separator"" })"" />


<BitOtpInput Label=""Bigger boxes, softer corners"" Length=""6"" Type=""BitInputType.Number""
             Style=""--bit-OtpInput-input-size: 3.5rem; --bit-OtpInput-radius: 1rem; --bit-OtpInput-font-size: 1.5rem; --bit-OtpInput-label-font-size: 0.875rem; --bit-OtpInput-font-weight: 600; --bit-OtpInput-gap: 0.5rem;"" />

<BitOtpInput Label=""A filled box shows its own progress"" Length=""6"" Type=""BitInputType.Number""
             Style=""--bit-OtpInput-filled-background: color-mix(in srgb, var(--bit-clr-pri) 12%, transparent); --bit-OtpInput-filled-border-color: var(--bit-clr-pri);"" />

<BitOtpInput Label=""A ticket stub, wider than it is tall"" Length=""6"" Uppercase Merged
             Style=""--bit-OtpInput-input-width: 2.75rem; --bit-OtpInput-input-height: 3.5rem; --bit-OtpInput-border-width: 2px; --bit-OtpInput-background: var(--bit-clr-bg-sec); --bit-OtpInput-radius: 0.25rem;"" />

<BitOtpInput Label=""Set in the monospaced face of the theme"" Length=""6"" Uppercase DefaultValue=""0OI1L5""
             Style=""--bit-OtpInput-font-family: var(--bit-tpg-font-family-mono);"" />


<div style=""--bit-OtpInput-radius: 999px; --bit-OtpInput-focus-border-color: var(--bit-clr-suc); --bit-OtpInput-focus-color: var(--bit-clr-suc-focus);"">
    <BitOtpInput Label=""Sign in"" Length=""4"" Type=""BitInputType.Number"" />

    <BitOtpInput Label=""Confirm"" Length=""4"" Type=""BitInputType.Number"" />
</div>";

    private readonly string example21RazorCode = @"
<BitOtpInput Label=""پیش‌فرض"" Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""معکوس"" Reversed Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""جداکننده"" Length=""6"" Separator=""-"" Dir=""BitDir.Rtl"" />";
}
