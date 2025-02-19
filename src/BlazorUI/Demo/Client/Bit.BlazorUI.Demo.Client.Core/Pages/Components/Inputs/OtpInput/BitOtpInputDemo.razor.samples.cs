using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.OtpInput;

public partial class BitOtpInputDemo
{
    private readonly string example1RazorCode = @"
<BitOtpInput />
<BitOtpInput IsEnabled=""false"" />
<BitOtpInput Length=""4"" />
<BitOtpInput AutoShift />
<BitOtpInput AutoFocus />";

    private readonly string example2RazorCode = @"
<BitOtpInput Label=""OTP"" />

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
<BitOtpInput Label=""Password"" Type=""BitInputType.Password"" />";

    private readonly string example4RazorCode = @"
<BitOtpInput Label=""Default"" />
<BitOtpInput Label=""Reversed"" Reversed />
<BitOtpInput Label=""Vertical"" Vertical />
<BitOtpInput Label=""Reversed Vertical"" Vertical Reversed />";

    private readonly string example5RazorCode = @"
<BitOtpInput Label=""One-way"" Value=""@oneWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""oneWayValue"" />

<BitOtpInput Label=""Two-way"" @bind-Value=""twoWayValue"" />
<BitTextField Style=""margin-top: 5px;"" @bind-Value=""twoWayValue"" />

<BitOtpInput Label=""Immediate"" @bind-Value=""@immediateValue"" Immediate />
<div>Value: [@immediateValue]</div>

<BitOtpInput Label=""Debounce"" @bind-Value=""@debounceValue"" Immediate DebounceTime=""300"" />
<div>Value: [@debounceValue]</div>

<BitOtpInput Label=""Throttle"" @bind-Value=""@throttleValue"" Immediate ThrottleTime=""300"" />
<div>Value: [@throttleValue]</div>";
    private readonly string example5CsharpCode = @"
private string? oneWayValue;
private string? twoWayValue;
private string? onChangeValue;
private string? immediateValue;
private string? debounceValue;
private string? throttleValue;";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
private string? onChangeValue;
private string? onFillValue;
private (FocusEventArgs Event, int Index)? onFocusInArgs;
private (FocusEventArgs Event, int Index)? onFocusOutArgs;
private (ChangeEventArgs Event, int Index)? onInputArgs;
private (KeyboardEventArgs Event, int Index)? onKeyDownArgs;
private (ClipboardEventArgs Event, int Index)? onPasteArgs;";

    private readonly string example7RazorCode = @"
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
    private readonly string example7CsharpCode = @"
public class ValidationOtpInputModel
{
    [Required(ErrorMessage = ""The OTP value is required."")]
    [MinLength(6, ErrorMessage = ""Minimum length is 6."")]
    public string OtpValue { get; set; }
}

private ValidationOtpInputModel validationOtpInputModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example8RazorCode = @"
<BitOtpInput Label=""Small"" Size=""BitSize.Small"" />
<BitOtpInput Label=""Medium"" Size=""BitSize.Medium"" />
<BitOtpInput Label=""Large"" Size=""BitSize.Large"" />";

    private readonly string example9RazorCode = @"
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

    .custom-focused {
        border-color: red;
        box-shadow: tomato 0 0 1rem;
    }
</style>


<BitOtpInput Style=""margin-inline: 1rem; box-shadow: aqua 0 0 0.5rem;"" />

<BitOtpInput Class=""custom-class"" />


<BitOtpInput Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                               Input = ""border-color: blueviolet;"",
                               Focused = ""box-shadow: blueviolet 0 0 1rem;"" })"" />

<BitOtpInput Classes=""@(new() { Root = ""custom-root"",
                                Input = ""custom-input"",
                                Focused = ""custom-focused"" })"" />";

    private readonly string example10RazorCode = @"
<BitOtpInput Label=""Default"" Dir=""BitDir.Rtl"" />
<BitOtpInput Label=""Reversed"" Reversed Dir=""BitDir.Rtl"" />";
}
