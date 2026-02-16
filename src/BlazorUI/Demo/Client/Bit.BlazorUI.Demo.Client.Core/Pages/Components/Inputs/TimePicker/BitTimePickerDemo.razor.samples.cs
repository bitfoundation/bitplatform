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
<BitTimePicker AllowTextInput Label=""Start time"" />";

    private readonly string example4RazorCode = @"
<BitTimePicker IconName=""@BitIconName.HourGlass"" Placeholder=""Select a time..."" />

<BitTimePicker IconName=""@BitIconName.HourGlass""
               IconLocation=""BitIconLocation.Left""
               Placeholder=""Select a time..."" />";

    private readonly string example5RazorCode = @"
<BitTimePicker Placeholder=""Select a time..."" ValueFormat=""hh-mm.ss"" />";

    private readonly string example6RazorCode = @"
<BitTimePicker @bind-Value=""@selectedTime"" Placeholder=""Select a time..."" />
<div>Selected time: @selectedTime.ToString()</div>";
    private readonly string example6CsharpCode = @"
private TimeSpan? selectedTime = new(5, 12, 15);";

    private readonly string example7RazorCode = @"
<BitTimePicker Standalone Label=""Standalone picker"" />

<BitTimePicker Standalone
               Label=""Picker (AM/PM)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitTimePicker Standalone
               Label=""Disabled""
               IsEnabled=""false""
               Value=""new(10, 24, 0)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example8RazorCode = @"
<BitTimePicker Label=""Basic"" ReadOnly @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Standalone"" Standalone ReadOnly @bind-Value=""@readOnlyTime"" />
<BitTimePicker Label=""Standalone TimeFormat (AM/PM)"" Standalone ReadOnly TimeFormat=""BitTimeFormat.TwelveHours"" @bind-Value=""@readOnlyTime"" />";
    private readonly string example8CsharpCode = @"
private TimeSpan? readOnlyTime = new(2, 50, 0);";

    private readonly string example9RazorCode = @"
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
                       InvalidErrorMessage=""Invalid Time!"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>";
    private readonly string example9CsharpCode = @"
public class FormValidationTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCircularTimePickerModel formValidationTimePickerModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(3000);

    formValidationTimePickerModel = new();

    StateHasChanged();
}";

    private readonly string example10RazorCode = @"
<BitTimePicker Responsive
               ShowCloseButton
               Placeholder=""Select a time..."" />";

    private readonly string example11RazorCode = @"
<BitTimePicker @ref=""timePicker"" Placeholder=""Select a time..."">
    <LabelTemplate>
        Custom label <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitButton>
    </LabelTemplate>
</BitTimePicker>

<BitTimePicker Label=""Custom icon"" Placeholder=""Select a time..."">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitTimePicker>";
    private readonly string example11CsharpCode = @"
private BitTimePicker timePicker;

private async Task OpenCallout()
{
    await timePicker.OpenCallout();
}";

    private readonly string example12RazorCode = @"
<BitTimePicker HourStep=""2""
               Label=""HourStep = 2""
               Placeholder=""Select a time..."" />

<BitTimePicker MinuteStep=""15""
               Label=""MinuteStep = 15""
               Placeholder=""Select a time..."" />";

    private readonly string example13RazorCode = @"
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

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
private TimeSpan? classesValue;";

    private readonly string example15RazorCode = @"
<BitTimePicker Dir=""BitDir.Rtl""
               Standalone
               Label=""تایم""
               Value=""new(10, 24, 0)""
               TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitTimePicker Dir=""BitDir.Rtl""
               ShowCloseButton
               Label=""تایم""
               Placeholder=""تایم خود را انتخاب کنید..."" />";
}
