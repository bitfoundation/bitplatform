namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public partial class BitDatePickerDemo
{
    private readonly string example1RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" />
<BitDatePicker Label=""Required"" Required />
<BitDatePicker Label=""PlaceHolder"" Placeholder=""Select a date"" />
<BitDatePicker Label=""Week numbers"" ShowWeekNumbers />
<BitDatePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth />
<BitDatePicker Label=""TimePicker"" ShowTimePicker />
<BitDatePicker Label=""Show clear button when has a value"" ShowClearButton />
<BitDatePicker Label=""StartingValue: December 2020, 20:45"" ShowTimePicker StartingValue=""startingValue"" />";

    private readonly string example1CsharpCode = @"
private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);";

    private readonly string example2RazorCode = @"
<BitDatePicker MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />";

    private readonly string example3RazorCode = @"
<BitDatePicker ShowTimePicker
               Label=""HourStep = 2""
               HourStep=""2"" />

<BitDatePicker ShowTimePicker
               Label=""MinuteStep = 15""
               MinuteStep=""15"" />";

    private readonly string example4RazorCode = @"
<BitDatePicker Label=""Formatted Date""
               DateFormat=""dd=MM(yy)""
               Placeholder=""Select a date"" />";

    private readonly string example5RazorCode = @"
<BitDatePicker Label=""Text input allowed""
               AllowTextInput
               DateFormat=""dd/MM/yyyy""
               Placeholder=""Enter a date (dd/MM/yyyy)"" />";

    private readonly string example6RazorCode = @"
<BitDatePicker @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>";
    private readonly string example6CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example7RazorCode = @"
<BitDatePicker Label=""fa-IR culture with Farsi names""
               GoToTodayTitle=""برو به امروز""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDatePicker Label=""fa-IR culture with Fingilish names""
               GoToTodayTitle=""Boro be emrouz""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example8RazorCode = @"
<BitDatePicker @bind-Value=""@timeZoneDate1"" ShowTimePicker />
<div>Selected date: @timeZoneDate1?.ToString()</div>

@{
    TimeZoneInfo? timeZoneInfo = null;
    var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
    if (allTimeZones.Count > 0)
    {
        timeZoneInfo = allTimeZones[0];
    }
}
@if (timeZoneInfo is not null) {
    <div>""@timeZoneInfo.Id"" TimeZone:</div><br/>
    <BitDatePicker TimeZone=""timeZoneInfo"" @bind-Value=""@timeZoneDate2"" ShowTimePicker />
    <div>Selected date: @timeZoneDate2?.ToString()</div>
}";
    private readonly string example8CsharpCode = @"
private DateTimeOffset? timeZoneDate1;
private DateTimeOffset? timeZoneDate2;";

    private readonly string example9RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" Standalone />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitDatePicker Label=""Week numbers"" ShowWeekNumbers Standalone />
<BitDatePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth Standalone />
<BitDatePicker Label=""TimePicker"" ShowTimePicker Standalone />";

    private readonly string example10RazorCode = @"
<BitDatePicker Label=""Basic"" ReadOnly @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Standalone"" ReadOnly Standalone @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Standalone with TimePicker"" ReadOnly ShowTimePicker Standalone @bind-Value=""readOnlyDate"" />";
    private readonly string example10CsharpCode = @"
private DateTimeOffset? readOnlyDate = DateTimeOffset.Now;";

    private readonly string example11RazorCode = @"
<style>
    .day-cell {
        width: 28px;
        height: 28px;
        position: relative;
    }

    .weekend-cell {
        color: red;
    }

    .badge {
        top: 2px;
        right: 2px;
        width: 8px;
        height: 8px;
        position: absolute;
        border-radius: 50%;
        background-color: red;
    }

    .year-suffix {
        position: absolute;
        bottom: 10px;
        right: -12px;
        height: 12px;
        color: gray;
        font-size: 8px;
    }
</style>


<BitDatePicker>
    <LabelTemplate>
        Custom label <BitIcon IconName=""@BitIconName.Calendar"" />
    </LabelTemplate>
</BitDatePicker>

<BitDatePicker Label=""DayCellTemplate"">
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitDatePicker>

<BitDatePicker Label=""MonthCellTemplate"">
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitDatePicker>

<BitDatePicker Label=""YearCellTemplate"">
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitDatePicker>";
    private readonly string example11CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example12RazorCode = @"
<BitDatePicker Label=""Response DatePicker""
               Responsive
               ShowWeekNumbers
               Placeholder=""Select a date"" />";

    private readonly string example13RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>


<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitDatePicker @bind-Value=""validationModel.Date"" />
    <ValidationMessage For=""@(() => validationModel.Date)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset"" Variant=""BitVariant.Outline""
               OnClick=""() => { validationModel = new(); SuccessMessage = string.Empty; }"">
        Reset
    </BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class BitDatePickerValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private BitDatePickerValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example14RazorCode = @"
<style>
    .custom-class {
        overflow: hidden;
        margin-inline: 1rem;
        border-radius: 1rem;
        border: 2px solid tomato;
    }

    .custom-class *, .custom-class *:after {
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

    .custom-day-picker {
        border: 1px solid blueviolet;
        background-color: #211e1b;
        border-end-start-radius: 0.5rem;
        border-start-start-radius: 0.5rem;
    }

    .custom-day-month,
    .custom-next-month,
    .custom-prev-month {
        color: white;
    }

    .custom-day {
        color: blueviolet;
        margin: 0.15rem;
        border-radius: 50%;
        border: 1px solid blueviolet;
    }

    .custom-today-day {
        color: #211e1b;
        background-color: blueviolet;
    }

    .custom-selected-day {
        background-color: violet;
    }

    .custom-week-header {
        color: white;
        margin: 0.15rem;
    }

    .custom-day-header {
        height: 2rem;
        color: white;
        margin: 0.15rem;
        padding-bottom: 0.5rem;
        border-bottom: 1px solid blueviolet;
    }

    .custom-year-picker {
        border: 1px solid #211e1b;
        background-color: blueviolet;
        border-end-end-radius: 0.5rem;
        border-start-end-radius: 0.5rem;
    }
</style>


<BitDatePicker Style=""margin: 1rem; box-shadow: dodgerblue 0 0 1rem;"" />

<BitDatePicker Class=""custom-class"" />


<BitDatePicker ShowTimePicker
               Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                 Focused = ""--focused-background: #b2b2b25a;"",
                                 Input = ""padding: 0.5rem;"",
                                 InputContainer = ""background: var(--focused-background);"",
                                 Group = ""border: 1px solid mediumseagreen; background: #1c73324d;"",
                                 Divider = ""border-color: mediumseagreen;"",
                                 DayPickerMonth = ""color: darkgreen;"",
                                 TodayDayButton = ""background-color: green;"",
                                 SelectedDayButton = ""background-color: limegreen;"",
                                 TimePickerIncreaseHourButton = ""background-color: limegreen;"",
                                 TimePickerIncreaseMinuteButton = ""background-color: limegreen;"",
                                 TimePickerDecreaseHourButton = ""background-color: limegreen;"",
                                 TimePickerDecreaseMinuteButton = ""background-color: limegreen;"" })"" />

<BitDatePicker @bind-Value=""@classesValue""
               Label=""Select a date""
               Classes=""@(new() { Root = ""custom-root"",
                                  Focused = ""custom-focus"",
                                  Input = ""custom-input"",
                                  InputContainer = ""custom-input-container"",
                                  Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"",
                                  DayPickerWrapper = ""custom-day-picker"",
                                  DayButton = ""custom-day"",
                                  TodayDayButton = ""custom-today-day"",
                                  SelectedDayButton = ""custom-selected-day"",
                                  PrevMonthNavButton = ""custom-prev-month"",
                                  NextMonthNavButton = ""custom-next-month"",
                                  DayPickerMonth = ""custom-day-month"",
                                  DayPickerHeader = ""custom-day-header"",
                                  WeekNumbersHeader = ""custom-week-header"",
                                  YearMonthPickerWrapper = ""custom-year-picker"" })"" />";
    private readonly string example14CsharpCode = @"
private DateTimeOffset? classesValue;";

    private readonly string example15RazorCode = @"
<BitDatePicker Dir=""BitDir.Rtl"" />";
}
