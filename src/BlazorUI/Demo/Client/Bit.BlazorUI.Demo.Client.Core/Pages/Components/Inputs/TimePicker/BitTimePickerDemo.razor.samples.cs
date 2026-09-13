namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePicker;

public partial class BitTimePickerDemo
{
    private readonly string example1RazorCode = @"
<BitTimePicker Label=""Basic TimePicker"" />
<BitTimePicker Label=""Disabled"" IsEnabled=""false"" />
<BitTimePicker Label=""Required"" Required />
<BitTimePicker Label=""Placeholder"" Placeholder=""Select a time..."" />";

    private readonly string example2RazorCode = @"
<BitTimePicker TimeFormat=""BitTimeFormat.TwelveHours""
               Placeholder=""Select a time...""
               Label=""12 hours (AM/PM)"" />

<BitTimePicker TimeFormat=""BitTimeFormat.TwentyFourHours""
               Placeholder=""Select a time...""
               Label=""24 hours"" />";

    private readonly string example3RazorCode = @"
<BitTimePicker ShowSeconds
               Label=""Seconds enabled""
               Placeholder=""Select a time..."" />

<BitTimePicker ShowSeconds
               TimeFormat=""BitTimeFormat.TwelveHours""
               Label=""Seconds (AM/PM)""
               Placeholder=""Select a time..."" />";

    private readonly string example4RazorCode = @"
<BitTimePicker AllowTextInput Label=""Start time"" />

<BitTimePicker AllowTextInput
               ShowSeconds
               Label=""Start time (with seconds)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example5RazorCode = @"
<BitTimePicker IconName=""@BitIconName.HourGlass"" Placeholder=""Select a time..."" />

<BitTimePicker IconName=""@BitIconName.HourGlass""
               IconLocation=""BitIconLocation.Left""
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Custom spin & close icons""
               ShowCloseButton
               IncreaseHourIconName=""@BitIconName.CaretSolidUp""
               DecreaseHourIconName=""@BitIconName.CaretSolidDown""
               IncreaseMinuteIconName=""@BitIconName.CaretSolidUp""
               DecreaseMinuteIconName=""@BitIconName.CaretSolidDown""
               CloseButtonIconName=""@BitIconName.ChromeClose""
               Placeholder=""Select a time..."" />";

    private readonly string example6RazorCode = @"
<BitTimePicker Placeholder=""Select a time..."" ValueFormat=""hh-mm.ss"" />";

    private readonly string example7RazorCode = @"
<BitTimePicker @bind-Value=""@selectedTime"" Placeholder=""Select a time..."" />
<div>Selected time: @selectedTime.ToString()</div>

<BitTimePicker DefaultValue=""new(8, 15, 0)"" Label=""DefaultValue (08:15)"" />";
    private readonly string example7CsharpCode = @"
private TimeSpan? selectedTime = new(5, 12, 15);";

    private readonly string example8RazorCode = @"
<BitTimePicker Label=""فارسی""
               Dir=""BitDir.Rtl""
               TimeFormat=""BitTimeFormat.TwelveHours""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()""
               Placeholder=""یک زمان انتخاب کنید..."" />";

    private readonly string example9RazorCode = @"
<BitTimePicker Standalone Label=""Standalone picker"" />

<BitTimePicker Standalone
               Label=""Picker (AM/PM)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitTimePicker Standalone
               Label=""Disabled""
               IsEnabled=""false""
               Value=""new(10, 24, 0)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example10RazorCode = @"
<BitTimePicker Label=""Basic"" ReadOnly @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Standalone"" Standalone ReadOnly @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Standalone TimeFormat (AM/PM)"" Standalone ReadOnly TimeFormat=""BitTimeFormat.TwelveHours"" @bind-Value=""@readOnlyTime"" />";
    private readonly string example10CsharpCode = @"
private TimeSpan? readOnlyTime = new(2, 50, 0);";

    private readonly string example11RazorCode = @"
<BitTimePicker Label=""Working hours (09:00 to 17:00)""
               MinTime=""new(9, 0, 0)""
               MaxTime=""new(17, 0, 0)""
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Morning only (until 11:59)""
               MaxTime=""new(11, 59, 0)""
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Even hours, on the quarter""
               AllowedHours=""@(h => h % 2 == 0)""
               AllowedMinutes=""@(m => m % 15 == 0)""
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Later today only (DisablePast)""
               DisablePast
               ShowNowButton
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Earlier today only (DisableFuture)""
               DisableFuture
               ShowNowButton
               Placeholder=""Select a time..."" />";

    private readonly string example12RazorCode = @"
<BitTimePicker HourStep=""2""
               Label=""HourStep = 2""
               Placeholder=""Select a time..."" />

<BitTimePicker MinuteStep=""15""
               Label=""MinuteStep = 15""
               Placeholder=""Select a time..."" />

<BitTimePicker ShowSeconds
               SecondStep=""30""
               Label=""SecondStep = 30""
               Placeholder=""Select a time..."" />";

    private readonly string example13RazorCode = @"
<BitTimePicker Label=""Starts from 09:30""
               StartingValue=""new(9, 30, 0)""
               Placeholder=""Select a time..."" />

<BitTimePicker Label=""Starts from midnight (default)""
               Placeholder=""Select a time..."" />";

    private readonly string example14RazorCode = @"
<BitTimePicker @bind-Value=""@actionsTime""
               ShowNowButton
               ShowClearButton
               Label=""Now & Clear buttons""
               Placeholder=""Select a time..."" />
<div>Selected time: @actionsTime.ToString()</div>

<BitTimePicker ShowCloseButton
               Label=""Close button""
               CloseButtonTitle=""Dismiss the picker""
               Placeholder=""Select a time..."" />

<BitTimePicker Standalone
               ShowNowButton
               ShowClearButton
               NowButtonText=""Current time""
               ClearButtonText=""Reset""
               Label=""Custom button texts"" />";
    private readonly string example14CsharpCode = @"
private TimeSpan? actionsTime;";

    private readonly string example15RazorCode = @"
<BitTimePicker Label=""Underlined"" Underlined Placeholder=""Select a time..."" />

<BitTimePicker Label=""No border"" HasBorder=""false"" Placeholder=""Select a time..."" />

<BitTimePicker Label=""DropDirection (All)""
               DropDirection=""BitDropDirection.All""
               Placeholder=""Select a time..."" />";

    private readonly string example16RazorCode = @"
<EditForm Model=""formValidationTimePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div class=""example-content"">
        <BitTimePicker @bind-Value=""formValidationTimePickerModel.Time""
                       AllowTextInput
                       Label=""Time required""
                       AriaLabel=""Select a time""
                       Placeholder=""Select a time...""
                       MinTime=""new(8, 0, 0)""
                       MaxTime=""new(18, 0, 0)""
                       AllowedMinutes=""@(m => m % 15 == 0)""
                       InvalidErrorMessage=""Invalid Time!""
                       OutOfRangeErrorMessage=""The time must be between 08:00 and 18:00!""
                       DisallowedTimeErrorMessage=""Only quarter hours can be booked!"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>";
    private readonly string example16CsharpCode = @"
public class FormValidationTimePickerModel
{
    [Required]
    public TimeSpan? Time { get; set; }
}

private string successMessage = string.Empty;
private FormValidationTimePickerModel formValidationTimePickerModel = new();

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

    private readonly string example17RazorCode = @"
<BitTimePicker Responsive
               ShowCloseButton
               Placeholder=""Select a time..."" />";

    private readonly string example18RazorCode = @"
<BitTimePicker Label=""Try it with the keyboard""
               CalloutAriaLabel=""Pick a meeting time""
               HourInputAriaLabel=""Meeting hour""
               MinuteInputAriaLabel=""Meeting minute""
               IncreaseHourTitle=""One hour later""
               DecreaseHourTitle=""One hour earlier""
               IncreaseMinuteTitle=""One minute later""
               DecreaseMinuteTitle=""One minute earlier""
               Placeholder=""Select a time..."" />";

    private readonly string example19RazorCode = @"
<BitTimePicker Label=""Watch the log""
               ShowClearButton
               Placeholder=""Select a time...""
               OnOpen=""LogOpen""
               OnClose=""LogClose""
               OnClick=""LogClick""
               OnClear=""LogClear""
               OnFocusIn=""LogFocusIn""
               OnFocusOut=""LogFocusOut""
               OnSelectTime=""LogSelectTime""
               OnChange=""LogChange"" />

<div class=""event-log"">
    @foreach (var log in eventLogs)
    {
        <div>@log</div>
    }
</div>";
    private readonly string example19CsharpCode = @"
private readonly List<string> eventLogs = [];

private void LogOpen() => Log(""OnOpen"");
private void LogClose() => Log(""OnClose"");
private void LogClick() => Log(""OnClick"");
private void LogClear() => Log(""OnClear"");
private void LogFocusIn() => Log(""OnFocusIn"");
private void LogFocusOut() => Log(""OnFocusOut"");
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

    private readonly string example20RazorCode = @"
<BitTimePicker @ref=""programmaticPicker"" @bind-IsOpen=""isCalloutOpen"" Label=""Controlled callout"" />
<div>IsOpen: @isCalloutOpen</div>

<BitButton OnClick=""() => programmaticPicker!.OpenCallout()"">OpenCallout()</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => programmaticPicker!.DismissCallout()"">
    DismissCallout()
</BitButton>";
    private readonly string example20CsharpCode = @"
private bool isCalloutOpen;
private BitTimePicker? programmaticPicker;";

    private readonly string example21RazorCode = @"
<style>
    .callout-header {
        width: 100%;
        font-weight: 600;
        text-align: center;
    }

    .callout-footer {
        display: flex;
        width: 100%;
        gap: 0.25rem;
        justify-content: space-between;
    }
</style>

<BitTimePicker @ref=""timePicker"" Placeholder=""Select a time..."">
    <LabelTemplate>
        Custom label <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitButton>
    </LabelTemplate>
</BitTimePicker>

<BitTimePicker Label=""Custom icon"" Placeholder=""Select a time..."">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitTimePicker>

<BitTimePicker @bind-Value=""@templateTime"" Label=""Callout header & footer"" Placeholder=""Select a time..."">
    <CalloutHeaderTemplate>
        <div class=""callout-header"">Pick a meeting time</div>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <div class=""callout-footer"">
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => templateTime = new(9, 0, 0)"">09:00</BitButton>
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => templateTime = new(13, 30, 0)"">13:30</BitButton>
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text"" OnClick=""() => templateTime = new(17, 45, 0)"">17:45</BitButton>
        </div>
    </CalloutFooterTemplate>
</BitTimePicker>";
    private readonly string example21CsharpCode = @"
private TimeSpan? templateTime;
private BitTimePicker timePicker;

private async Task OpenCallout()
{
    await timePicker.OpenCallout();
}";

    private readonly string example22RazorCode = @"
<BitTimePicker Label=""Primary"" Color=""BitColor.Primary"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Secondary"" Color=""BitColor.Secondary"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Tertiary"" Color=""BitColor.Tertiary"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Info"" Color=""BitColor.Info"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Success"" Color=""BitColor.Success"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Warning"" Color=""BitColor.Warning"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""SevereWarning"" Color=""BitColor.SevereWarning"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""Error"" Color=""BitColor.Error"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""PrimaryBackground"" Color=""BitColor.PrimaryBackground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""SecondaryBackground"" Color=""BitColor.SecondaryBackground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""TertiaryBackground"" Color=""BitColor.TertiaryBackground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""PrimaryForeground"" Color=""BitColor.PrimaryForeground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""SecondaryForeground"" Color=""BitColor.SecondaryForeground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""TertiaryForeground"" Color=""BitColor.TertiaryForeground"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""PrimaryBorder"" Color=""BitColor.PrimaryBorder"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""SecondaryBorder"" Color=""BitColor.SecondaryBorder"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />
<BitTimePicker Label=""TertiaryBorder"" Color=""BitColor.TertiaryBorder"" ShowNowButton TimeFormat=""BitTimeFormat.TwelveHours"" Value=""new(10, 30, 0)"" />";

    private readonly string example23RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTimePicker Icon=""@(""fa-solid fa-clock"")"" Placeholder=""Select a time..."" />

<BitTimePicker Icon=""@BitIconInfo.Css(""fa-solid fa-hourglass-half"")""
               Placeholder=""Select a time...""
               IconLocation=""BitIconLocation.Left"" />

<BitTimePicker Icon=""@BitIconInfo.Fa(""solid stopwatch"")""
               Placeholder=""Select a time..."" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTimePicker Icon=""@(""bi bi-clock-fill"")"" Placeholder=""Select a time..."" />

<BitTimePicker Icon=""@BitIconInfo.Css(""bi bi-alarm-fill"")""
               Placeholder=""Select a time...""
               IconLocation=""BitIconLocation.Left"" />

<BitTimePicker Icon=""@BitIconInfo.Bi(""stopwatch-fill"")""
               Placeholder=""Select a time..."" />";

    private readonly string example24RazorCode = @"
<BitTimePicker Label=""Small"" Size=""BitSize.Small"" TimeFormat=""BitTimeFormat.TwelveHours"" Placeholder=""Select a time..."" />

<BitTimePicker Label=""Medium"" Size=""BitSize.Medium"" TimeFormat=""BitTimeFormat.TwelveHours"" Placeholder=""Select a time..."" />

<BitTimePicker Label=""Large"" Size=""BitSize.Large"" TimeFormat=""BitTimeFormat.TwelveHours"" Placeholder=""Select a time..."" />";

    private readonly string example25RazorCode = @"
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

    .custom-button {
        border-radius: 50%;
        background-color: blueviolet;
    }
</style>


<BitTimePicker Style=""margin: 1rem; box-shadow: dodgerblue 0 0 1rem;"" />

<BitTimePicker Class=""custom-class"" />


<BitTimePicker Placeholder=""Select a time...""
               Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                 Focused = ""--focused-background: #b2b2b25a;"",
                                 Input = ""padding: 0.5rem;"",
                                 InputContainer = ""background: var(--focused-background);"",
                                 IncreaseHourButton = ""color: dodgerblue;"",
                                 DecreaseHourButton = ""color: dodgerblue;"",
                                 IncreaseMinuteButton = ""color: dodgerblue;"",
                                 DecreaseMinuteButton = ""color: dodgerblue;"" })"" />

<BitTimePicker @bind-Value=""@classesValue""
               Label=""Select a time""
               Classes=""@(new() { Root = ""custom-root"",
                                  Focused = ""custom-focus"",
                                  Input = ""custom-input"",
                                  InputContainer = ""custom-input-container"",
                                  Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"",
                                  IncreaseHourButton = ""custom-button"",
                                  DecreaseHourButton = ""custom-button"",
                                  IncreaseMinuteButton = ""custom-button"",
                                  DecreaseMinuteButton = ""custom-button"" })"" />";
    private readonly string example25CsharpCode = @"
private TimeSpan? classesValue;";

    private readonly string example26RazorCode = @"
<BitTimePicker Dir=""BitDir.Rtl""
               Standalone
               Label=""زمان""
               Value=""new(10, 24, 0)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitTimePicker Dir=""BitDir.Rtl""
               ShowCloseButton
               Label=""زمان""
               Placeholder=""زمان خود را انتخاب کنید..."" />";
}
