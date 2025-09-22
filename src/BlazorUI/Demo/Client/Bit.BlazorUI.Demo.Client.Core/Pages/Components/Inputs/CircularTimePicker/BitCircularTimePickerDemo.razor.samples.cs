namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.CircularTimePicker;

public partial class BitCircularTimePickerDemo
{
    private readonly string example1RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" />
<BitCircularTimePicker Label=""Disabled"" IsEnabled=""false"" />
<BitCircularTimePicker Label=""Required"" Required />
<BitCircularTimePicker Label=""PlaceHolder"" Placeholder=""Select a time"" />
<BitCircularTimePicker Label=""TimeFormat (AM/PM)"" TimeFormat=""BitTimeFormat.TwelveHours"" />
<BitCircularTimePicker Label=""Custom icon"" IconName=""@BitIconName.Airplane"" />";

    private readonly string example2RazorCode = @"
<BitCircularTimePicker Label=""Text input allowed""
                       AllowTextInput=""true""
                       ValueFormat=""hh:mm""
                       Placeholder=""Enter a time (hh:mm)"" />";

    private readonly string example3RazorCode = @"
<BitCircularTimePicker Label=""Formatted time""
                       ValueFormat=""hh-mm.ss""
                       Placeholder=""Select a time"" />";

    private readonly string example5RazorCode = @"
<BitCircularTimePicker @bind-Value=""@selectedTime"" />
<div>Selected time: @selectedTime.ToString()</div>";
    private readonly string example5CsharpCode = @"
private TimeSpan? selectedTime = new(5, 12, 15);";

    private readonly string example6RazorCode = @"
<BitCircularTimePicker Label=""fa-IR culture""
                       TimeFormat=""BitTimeFormat.TwelveHours""
                       Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />";


    private readonly string example7RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" Standalone />
<BitCircularTimePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitCircularTimePicker Label=""PlaceHolder"" Placeholder=""Select a time"" Standalone />
<BitCircularTimePicker Label=""TimeFormat (AM/PM)"" TimeFormat=""BitTimeFormat.TwelveHours"" Standalone />";

    private readonly string example8RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" ReadOnly @bind-Value=""@readOnlyTime"" />

<BitCircularTimePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""@readOnlyTime"" />

<BitCircularTimePicker Label=""Standalone CircularTimePicker"" Standalone ReadOnly @bind-Value=""@readOnlyTime"" />

<BitCircularTimePicker Label=""Standalone TimeFormat (AM/PM)"" Standalone ReadOnly TimeFormat=""BitTimeFormat.TwelveHours"" @bind-Value=""@readOnlyTime"" />";
    private readonly string example8CsharpCode = @"
private TimeSpan? readOnlyTime = new(2, 50, 0);";

    private readonly string example9RazorCode = @"
<BitCircularTimePicker @ref=""circularTimePicker"">
    <LabelTemplate>
        Custom label <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitButton>
    </LabelTemplate>
</BitCircularTimePicker>

<BitCircularTimePicker Label=""Custom left-handed icon""
                       IconLocation=""BitIconLocation.Left""
                       Placeholder=""Select a time"">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitCircularTimePicker>";
    private readonly string example9CsharpCode = @"
private BitCircularTimePicker circularTimePicker;

private async Task OpenCallout()
{
    await circularTimePicker.OpenCallout();
}";

    private readonly string example10RazorCode = @"
<BitCircularTimePicker Label=""Response CircularTimePicker""
                       Placeholder=""Select a time"" 
                       Responsive />";

    private readonly string example11RazorCode = @"
<EditForm Model=""formValidationCircularTimePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitCircularTimePicker @bind-Value=""formValidationCircularTimePickerModel.Time""
                                AllowTextInput
                                Placeholder=""Select a time""
                                Label=""Time required"" />
        <ValidationMessage For=""@(() => formValidationCircularTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>

@if (string.IsNullOrEmpty(successMessage) is false)
{
    <BitMessage Color=""BitColor.Success"">@successMessage</BitMessage>
}



<EditForm Model=""formValidationCircularTimePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitCircularTimePicker @bind-Value=""formValidationCircularTimePickerModel.Time""
                        AllowTextInput=""true""
                        Label=""Custom Invalid Error Message""
                        InvalidErrorMessage=""Invalid Time!!!"" />
        <ValidationMessage For=""@(() => formValidationCircularTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example11CsharpCode = @"
public class FormValidationCircularTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(3000);

    formValidationCircularTimePickerModel = new();

    StateHasChanged();
}";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid tomato;
    }

    .custom-class *, .custom-class *::after {
        border: none;
    }


    .custom-root {
        height: 3rem;
        margin: 1rem;
        display: flex;
        align-items: end;
        position: relative;
        border-radius: 0.5rem;
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

    .custom-input-container {
        border-radius: 0;
        position: relative;
        border-width: 0 0 1px 0;
    }

    .custom-input-container::after {
        content: '';
        width: 0;
        height: 2px;
        border: none;
        position: absolute;
        inset: 100% 0 0 50%;
        background-color: blueviolet;
        transition: width 0.3s ease, left 0.3s ease;
    }

    .custom-focus .custom-input-container::after {
        left: 0;
        width: 100%;
    }

    .custom-focus .custom-label {
        color: blueviolet;
        transform: translate(0, 1.5px) scale(0.75);
    }

    .custom-toolbar {
        background-color: blueviolet;
    }

    .custom-clock-face {
        background-color: blueviolet;
    }

    .custom-clock-number {
        font-weight: bold;
    }

    .custom-clock-pin,
    .custom-clock-pointer,
    .custom-clock-pointer-thumb,
    .custom-clock-selected-number {
        color: gray;
        background-color: white;
    }

    .custom-clock-pointer-thumb-minute {
        border-color: white;
    }
</style>


<BitCircularTimePicker Style=""margin: 1rem; box-shadow: dodgerblue 0 0 1rem;"" />

<BitCircularTimePicker Class=""custom-class"" />


<BitCircularTimePicker Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                         Focused = ""--focused-background: #b2b2b25a;"",
                                         Input = ""padding: 0.5rem;"",
                                         InputContainer = ""background: var(--focused-background);"",
                                         HourButton = ""color: gray;"",
                                         MinuteButton = ""color: gray;"",
                                         HourMinuteSeparator = ""color: gray;"",
                                         Toolbar = ""background-color: transparent;"",
                                         ClockFace=""box-shadow: dodgerblue 0 0 1rem;"",
                                         ClockPointerThumb = ""background-color: blue;"" })"" />

<BitCircularTimePicker @bind-Value=""@classesValue""
                       Label=""Select a date""
                       Classes=""@(new() { Root = ""custom-root"",
                                          Focused = ""custom-focus"",
                                          Input = ""custom-input"",
                                          InputContainer = ""custom-input-container"",
                                          Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"",
                                          Toolbar = ""custom-toolbar"",
                                          ClockPin = ""custom-clock-pin"",
                                          ClockFace = ""custom-clock-face"",
                                          ClockNumber = ""custom-clock-number"",
                                          ClockPointer = ""custom-clock-pointer"",
                                          ClockPointerThumb = ""custom-clock-pointer-thumb"",
                                          ClockSelectedNumber = ""custom-clock-selected-number"",
                                          ClockPointerThumbMinute = ""custom-clock-pointer-thumb-minute"" })"" />";
    private readonly string example12CsharpCode = @"
private TimeSpan? classesValue;";

    private readonly string example13RazorCode = @"
<BitCircularTimePicker Dir=""BitDir.Rtl"" />";
}
