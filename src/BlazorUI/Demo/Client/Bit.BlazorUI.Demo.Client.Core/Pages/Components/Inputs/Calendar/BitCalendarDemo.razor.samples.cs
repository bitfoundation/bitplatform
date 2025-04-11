namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public partial class BitCalendarDemo
{
    private readonly string example1RazorCode = @"
<BitCalendar />
<BitCalendar IsEnabled=""false"" />
<BitCalendar ShowWeekNumbers=""true"" />
<BitCalendar HighlightCurrentMonth=""true"" HighlightSelectedMonth=""true"" />
<BitCalendar ShowTimePicker=""true"" StartingValue=""startingValue"" />";
    private readonly string example1CsharpCode = @"
private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);";

    private readonly string example2RazorCode = @"
<BitCalendar MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitCalendar MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitCalendar MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />";

    private readonly string example3RazorCode = @"
<BitCalendar ShowTimePicker=""true"" HourStep=""2"" />
<BitCalendar ShowTimePicker=""true"" MinuteStep=""15"" />";

    private readonly string example4RazorCode = @"
<BitCalendar GoToToday=""برو به امروز"" Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />
<BitCalendar GoToToday=""Boro be emrouz"" Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example5RazorCode = @"
<BitCalendar @bind-Value=""@timeZoneDate1"" ShowTimePicker />
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
    <BitCalendar TimeZone=""timeZoneInfo"" @bind-Value=""@timeZoneDate2"" ShowTimePicker />
    <div>Selected date: @timeZoneDate2?.ToString()</div>
}";
    private readonly string example5CsharpCode = @"
private DateTimeOffset? timeZoneDate1;
private DateTimeOffset? timeZoneDate2;";

    private readonly string example6RazorCode = @"
<BitCalendar @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>";
    private readonly string example6CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example7RazorCode = @"
<BitCalendar ReadOnly @bind-Value=""readOnlyDate"" />
<BitCalendar ReadOnly ShowTimePicker @bind-Value=""readOnlyDate"" />";
    private readonly string example7CsharpCode = @"
private DateTimeOffset? readOnlyDate = DateTimeOffset.Now;";

    private readonly string example8RazorCode = @"
<BitCalendar ShowMonthPicker=""@showMonthPicker"" />
<BitToggleButton OnText=""MonthPicker visible"" OffText=""MonthPicker invisible"" @bind-IsChecked=""@showMonthPicker"" />

<BitCalendar ShowMonthPickerAsOverlay=""@showMonthPickerAsOverlay"" />
<BitToggleButton OnText=""Position Overlay"" OffText=""Position Besides"" @bind-IsChecked=""@showMonthPickerAsOverlay"" />";
    private readonly string example8CsharpCode = @"
private bool showMonthPicker = true;
private bool showMonthPickerAsOverlay;";

    private readonly string example9RazorCode = @"
<BitCalendar @bind-Value=""@selectedDateTime"" ShowTimePicker=""true"" />
<div>Selected DateTime: @selectedDateTime.ToString()</div>";
    private readonly string example9CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;";

    private readonly string example10RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitCalendar @bind-Value=""validationModel.Date"" />
    <ValidationMessage For=""@(() => validationModel.Date)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset"" Variant=""BitVariant.Outline""
               OnClick=""() => { validationModel = new(); SuccessMessage=string.Empty; }"">
        Reset
    </BitButton>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class BitCalendarValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private BitCalendarValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

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

<BitCalendar>
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitCalendar>

<BitCalendar>
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitCalendar>

<BitCalendar>
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitCalendar>";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        background: #8a2be270;
        border-radius: 1rem;
        box-shadow: blueviolet 0 0 1rem;
    }


    .custom-root {
        margin: 1rem;
        border-radius: 0.5rem;
        background-color: #211e1b;
    }

    .custom-day-picker {
        border: 1px solid #e9981e;
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
        color: #e9981e;
        margin: 0.15rem;
        border-radius: 50%;
        border: 1px solid #e9981e;
    }

    .custom-today-day {
        color: #211e1b;
        background-color: #e9981e;
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
        border-bottom: 1px solid #e9981e;
    }

    .custom-year-picker {
        border: 1px solid #211e1b;
        background-color: #e9981e;
        border-end-end-radius: 0.5rem;
        border-start-end-radius: 0.5rem;
    }
</style>


<BitCalendar Style=""margin: 1rem; border-radius: 1rem; background: #a5104457;"" />

<BitCalendar Class=""custom-class"" />


<BitCalendar ShowTimePicker=""true""
             Styles=""@(new() { Root = ""margin: 1rem; border: 1px solid mediumseagreen; background: #1c73324d;"",
                               Divider = ""border-color: mediumseagreen;"",
                               DayPickerMonth = ""color: darkgreen;"",
                               TodayDayButton = ""background-color: green;"",
                               SelectedDayButton = ""background-color: limegreen;"",
                               TimePickerIncreaseHourButton = ""background-color: limegreen;"",
                               TimePickerIncreaseMinuteButton = ""background-color: limegreen;"",
                               TimePickerDecreaseHourButton = ""background-color: limegreen;"",
                               TimePickerDecreaseMinuteButton = ""background-color: limegreen;"" })"" />

<BitCalendar Classes=""@(new() { Root = ""custom-root"",
                             DayPickerWrapper = ""custom-day-picker"",
                             DayButton = ""custom-day"",
                             TodayDayButton = ""custom-today-day"",
                             PrevMonthNavButton = ""custom-prev-month"",
                             NextMonthNavButton = ""custom-next-month"",
                             DayPickerMonth = ""custom-day-month"",
                             DayPickerHeader = ""custom-day-header"",
                             WeekNumbersHeader = ""custom-week-header"",
                             YearMonthPickerWrapper = ""custom-year-picker"" })"" />";

    private readonly string example13RazorCode = @"
<BitCalendar Dir=""BitDir.Rtl"" />";
}
