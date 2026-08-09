namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.CircularTimePicker;

public partial class BitCircularTimePickerDemo
{
    private readonly string example1RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" />

<BitCircularTimePicker Label=""Disabled"" IsEnabled=""false"" />

<BitCircularTimePicker Label=""Required"" Required />

<BitCircularTimePicker Label=""PlaceHolder"" Placeholder=""Select a time"" />

<BitCircularTimePicker Label=""Custom icon"" IconName=""@BitIconName.Airplane"" />";

    private readonly string example2RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" Standalone />

<BitCircularTimePicker Label=""Disabled"" IsEnabled=""false"" Standalone />";

    private readonly string example3RazorCode = @"
<BitCircularTimePicker Label=""24-hour (default)"" Standalone DefaultValue=""@(new TimeSpan(21, 45, 0))"" />

<BitCircularTimePicker Label=""12-hour"" Standalone
                       TimeFormat=""BitTimeFormat.TwelveHours""
                       DefaultValue=""@(new TimeSpan(21, 45, 0))"" />

<BitCircularTimePicker Label=""12-hour, AM/PM under the clock"" Standalone AmPmInClock
                       TimeFormat=""BitTimeFormat.TwelveHours""
                       DefaultValue=""@(new TimeSpan(21, 45, 0))"" />";

    private readonly string example4RazorCode = @"
<BitCircularTimePicker Label=""Hours, minutes & seconds"" Standalone ShowSeconds
                       DefaultValue=""@(new TimeSpan(9, 30, 15))"" />

<BitCircularTimePicker Label=""SecondStep = 10"" Standalone ShowSeconds SecondStep=""10""
                       DefaultValue=""@(new TimeSpan(9, 30, 20))"" />

<BitCircularTimePicker Label=""With the field"" ShowSeconds
                       Placeholder=""Select a time""
                       @bind-Value=""@secondsTime"" />
<div>Selected time: @secondsTime.ToString()</div>";

    private readonly string example5RazorCode = @"
<BitCircularTimePicker Label=""OnlyHours"" Standalone
                       EditMode=""BitCircularTimePickerEditMode.OnlyHours""
                       DefaultValue=""@(new TimeSpan(9, 30, 0))"" />

<BitCircularTimePicker Label=""OnlyMinutes"" Standalone
                       EditMode=""BitCircularTimePickerEditMode.OnlyMinutes""
                       DefaultValue=""@(new TimeSpan(9, 30, 0))"" />

<BitCircularTimePicker Label=""OnlySeconds"" Standalone
                       EditMode=""BitCircularTimePickerEditMode.OnlySeconds""
                       DefaultValue=""@(new TimeSpan(9, 30, 15))"" />

<BitCircularTimePicker Label=""StartView: Minute"" Standalone
                       StartView=""BitCircularTimePickerView.Minute""
                       OnViewChange=""v => changedView = v""
                       DefaultValue=""@(new TimeSpan(9, 30, 0))"" />

<div>Last view change: @(changedView?.ToString() ?? ""-"")</div>";
    private readonly string example5CsharpCode = @"
private BitCircularTimePickerView? changedView;";

    private readonly string example6RazorCode = @"
<BitCircularTimePicker Label=""MinuteStep = 5"" Standalone MinuteStep=""5"" DefaultValue=""@(new TimeSpan(10, 15, 0))"" />

<BitCircularTimePicker Label=""MinuteStep = 15"" Standalone MinuteStep=""15"" DefaultValue=""@(new TimeSpan(10, 15, 0))"" />

<BitCircularTimePicker Label=""HourStep = 3"" Standalone HourStep=""3"" DefaultValue=""@(new TimeSpan(9, 0, 0))"" />";

    private readonly string example7RazorCode = @"
<BitCircularTimePicker Label=""Between 08:30 and 17:15"" Standalone
                       MinTime=""@(new TimeSpan(8, 30, 0))""
                       MaxTime=""@(new TimeSpan(17, 15, 0))""
                       DefaultValue=""@(new TimeSpan(8, 30, 0))"" />

<BitCircularTimePicker Label=""Working hours only, on the half hour"" Standalone
                       AllowedHours=""@(h => h is >= 9 and <= 17)""
                       AllowedMinutes=""@(m => m is 0 or 30)""
                       DefaultValue=""@(new TimeSpan(9, 0, 0))"" />";

    private readonly string example8RazorCode = @"
<BitCircularTimePicker Label=""Now & Clear"" ShowNowButton ShowClearButton Placeholder=""Select a time"" />

<BitCircularTimePicker Label=""Close button"" ShowCloseButton Placeholder=""Select a time"" />

<BitCircularTimePicker Label=""AutoClose"" AutoClose Placeholder=""Select a time"" />";

    private readonly string example9RazorCode = @"
<BitCircularTimePicker Label=""Controlled callout"" Placeholder=""Select a time"" @bind-IsOpen=""isCalloutOpen"" />

<BitButton OnClick=""() => isCalloutOpen = !isCalloutOpen"">
    @(isCalloutOpen ? ""Close"" : ""Open"") from outside
</BitButton>

<BitCircularTimePicker Label=""Any direction"" Placeholder=""Select a time""
                       DropDirection=""BitDropDirection.All"" />";
    private readonly string example9CsharpCode = @"
private bool isCalloutOpen;";

    private readonly string example10RazorCode = @"
<BitCircularTimePicker Label=""Text input allowed""
                       AllowTextInput
                       ValueFormat=""HH:mm""
                       Placeholder=""Enter a time (HH:mm)"" />";

    private readonly string example11RazorCode = @"
<BitCircularTimePicker Label=""Formatted time""
                       ValueFormat=""hh-mm tt""
                       Placeholder=""Select a time""
                       TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example12RazorCode = @"
<BitCircularTimePicker Label=""Two-way bound"" @bind-Value=""@selectedTime"" />
<div>Selected time: @selectedTime.ToString()</div>

<BitCircularTimePicker Label=""Uncontrolled""
                       DefaultValue=""@(new TimeSpan(7, 30, 0))""
                       OnChange=""v => changedTime = v"" />
<div>Changed time: @changedTime.ToString()</div>";
    private readonly string example12CsharpCode = @"
private TimeSpan? selectedTime = new(5, 12, 0);
private TimeSpan? changedTime;";

    private readonly string example13RazorCode = @"
<BitCircularTimePicker Label=""fa-IR culture""
                       TimeFormat=""BitTimeFormat.TwelveHours""
                       Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitCircularTimePicker Label=""Face taken from the current culture""
                       TimeFormat=""@GetTimeFormatOf(CultureInfo.CurrentUICulture)"" />";
    private readonly string example13CsharpCode = @"
// The short time pattern of a culture spells the hour with an ""h"" where its readers
// expect a 12-hour clock and with an ""H"" where they expect a 24-hour one.
private static BitTimeFormat GetTimeFormatOf(CultureInfo culture)
{
    return culture.DateTimeFormat.ShortTimePattern.Contains('h')
        ? BitTimeFormat.TwelveHours
        : BitTimeFormat.TwentyFourHours;
}";

    private readonly string example14RazorCode = @"
<BitCircularTimePicker Label=""Basic"" ReadOnly @bind-Value=""@readOnlyTime"" />

<BitCircularTimePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""@readOnlyTime"" />

<BitCircularTimePicker Label=""Standalone"" Standalone ReadOnly @bind-Value=""@readOnlyTime"" />";
    private readonly string example14CsharpCode = @"
private TimeSpan? readOnlyTime = new(2, 50, 0);";

    private readonly string example15RazorCode = @"
<BitCircularTimePicker Label=""Underlined"" Underlined Placeholder=""Select a time"" />

<BitCircularTimePicker Label=""No border"" HasBorder=""false"" Placeholder=""Select a time"" />

<BitCircularTimePicker Label=""Icon on the left"" IconLocation=""BitIconLocation.Left"" Placeholder=""Select a time"" />";

    private readonly string example16RazorCode = @"
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
    private readonly string example16CsharpCode = @"
private BitCircularTimePicker circularTimePicker = default!;

private async Task OpenCallout()
{
    await circularTimePicker.OpenCallout();
}";

    private readonly string example17RazorCode = @"
<BitCircularTimePicker Label=""Responsive CircularTimePicker""
                       Placeholder=""Select a time""
                       Responsive />";

    private readonly string example18RazorCode = @"
<BitCircularTimePicker Label=""Try it with the keyboard""
                       Placeholder=""Select a time""
                       CalloutAriaLabel=""Pick a meeting time""
                       HourButtonTitle=""Meeting hour""
                       MinuteButtonTitle=""Meeting minute"" />";

    private readonly string example19RazorCode = @"
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
                               AllowTextInput
                               MinTime=""@(new TimeSpan(8, 0, 0))""
                               MaxTime=""@(new TimeSpan(18, 0, 0))""
                               Label=""Custom Invalid Error Message""
                               InvalidErrorMessage=""Please enter a time between 08:00 and 18:00."" />
        <ValidationMessage For=""@(() => formValidationCircularTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example19CsharpCode = @"
public class FormValidationCircularTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}

private string successMessage = string.Empty;
private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();

private async Task HandleValidSubmit()
{
    successMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    successMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    successMessage = string.Empty;
}";

    private readonly string example20RazorCode = @"
<BitCircularTimePicker Label=""Watch the log""
                       Placeholder=""Select a time""
                       ShowClearButton
                       OnOpen=""LogOpen""
                       OnClose=""LogClose""
                       OnClick=""LogClick""
                       OnFocusIn=""LogFocusIn""
                       OnFocusOut=""LogFocusOut""
                       OnViewChange=""LogViewChange""
                       OnSelectTime=""LogSelectTime""
                       OnChange=""LogChange"" />

<div class=""event-log"">
    @foreach (var log in eventLogs)
    {
        <div>@log</div>
    }
</div>";
    private readonly string example20CsharpCode = @"
private readonly List<string> eventLogs = [];

private void LogOpen() => Log(""OnOpen"");
private void LogClose() => Log(""OnClose"");
private void LogClick() => Log(""OnClick"");
private void LogFocusIn() => Log(""OnFocusIn"");
private void LogFocusOut() => Log(""OnFocusOut"");
private void LogViewChange(BitCircularTimePickerView view) => Log($""OnViewChange: {view}"");
private void LogSelectTime(TimeSpan? time) => Log($""OnSelectTime: {time}"");
private void LogChange(TimeSpan? time) => Log($""OnChange: {time}"");

private void Log(string message)
{
    eventLogs.Insert(0, message);

    if (eventLogs.Count > 8)
    {
        eventLogs.RemoveRange(8, eventLogs.Count - 8);
    }
}";

    private readonly string example21RazorCode = @"
<BitCircularTimePicker Color=""BitColor.Primary"" Label=""Primary"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.Secondary"" Label=""Secondary"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.Tertiary"" Label=""Tertiary"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Color=""BitColor.Info"" Label=""Info"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.Success"" Label=""Success"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.Warning"" Label=""Warning"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.SevereWarning"" Label=""SevereWarning"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.Error"" Label=""Error"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Color=""BitColor.PrimaryBackground"" Label=""PrimaryBackground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.SecondaryBackground"" Label=""SecondaryBackground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.TertiaryBackground"" Label=""TertiaryBackground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Color=""BitColor.PrimaryForeground"" Label=""PrimaryForeground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.SecondaryForeground"" Label=""SecondaryForeground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.TertiaryForeground"" Label=""TertiaryForeground"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Color=""BitColor.PrimaryBorder"" Label=""PrimaryBorder"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.SecondaryBorder"" Label=""SecondaryBorder"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />
<BitCircularTimePicker Color=""BitColor.TertiaryBorder"" Label=""TertiaryBorder"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCircularTimePicker Label=""FontAwesome"" Icon=""@(""fa-solid fa-clock"")"" />

<BitCircularTimePicker Label=""FontAwesome (Css)"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" />

<BitCircularTimePicker Label=""FontAwesome (Fa)"" Icon=""@BitIconInfo.Fa(""solid clock"")""
                       ShowCloseButton CloseButtonIcon=""@BitIconInfo.Fa(""solid xmark"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCircularTimePicker Label=""Bootstrap"" Icon=""@(""bi bi-clock-fill"")"" />

<BitCircularTimePicker Label=""Bootstrap (Css)"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" />

<BitCircularTimePicker Label=""Bootstrap (Bi)"" Icon=""@BitIconInfo.Bi(""clock-fill"")""
                       ShowCloseButton CloseButtonIcon=""@BitIconInfo.Bi(""x-lg"")"" />";

    private readonly string example23RazorCode = @"
<BitCircularTimePicker Size=""BitSize.Small"" Label=""Small"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Size=""BitSize.Medium"" Label=""Medium"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />

<BitCircularTimePicker Size=""BitSize.Large"" Label=""Large"" DefaultValue=""@(new TimeSpan(10, 10, 0))"" />";

    private readonly string example24RazorCode = @"
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

    .custom-clock-disabled-number {
        opacity: 0.4;
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

    .custom-clear-button {
        color: blueviolet;
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
                                         ClockFace = ""box-shadow: dodgerblue 0 0 1rem;"",
                                         ClockPointerThumb = ""background-color: blue;"" })"" />

<BitCircularTimePicker @bind-Value=""@classesValue""
                       Label=""Select a time""
                       ShowClearButton
                       MinuteStep=""5""
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
                                          ClockDisabledNumber = ""custom-clock-disabled-number"",
                                          ClockPointerThumbMinute = ""custom-clock-pointer-thumb-minute"",
                                          ClearButton = ""custom-clear-button"" })"" />";
    private readonly string example24CsharpCode = @"
private TimeSpan? classesValue;";

    private readonly string example25RazorCode = @"
<BitCircularTimePicker Dir=""BitDir.Rtl""
                       Label=""ساعت""
                       Placeholder=""یک ساعت انتخاب کنید""
                       TimeFormat=""BitTimeFormat.TwelveHours"" />";
}
