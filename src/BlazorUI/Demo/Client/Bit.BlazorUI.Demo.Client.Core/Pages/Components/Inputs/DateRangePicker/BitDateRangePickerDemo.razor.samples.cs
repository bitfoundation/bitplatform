namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    private readonly string example1RazorCode = @"
<BitDateRangePicker Label=""Basic DateRangePicker"" />
<BitDateRangePicker Label=""Disabled"" IsEnabled=""false"" />
<BitDateRangePicker Label=""Required"" Required />
<BitDateRangePicker Label=""PlaceHolder"" Placeholder=""Select a date range"" />
<BitDateRangePicker Label=""Week numbers"" ShowWeekNumbers />
<BitDateRangePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth />
<BitDateRangePicker Label=""TimePicker"" ShowTimePicker />
<BitDateRangePicker Label=""Custom Icon"" IconName=""@BitIconName.Airplane"" />
<BitDateRangePicker Label=""Disabled AutoClose"" AutoClose=""false"" />
<BitDateRangePicker Label=""Show clear button when has a value"" ShowClearButton />
<BitDateRangePicker Label=""StartingValue: December 2020, Start Time: 10:12, End Time: 16:59"" ShowTimePicker StartingValue=""startingValue"" />";
    private readonly string example1CsharpCode = @"
private BitDateRangePickerValue? startingValue = new()
{
    StartDate = new DateTimeOffset(2020, 12, 4, 10, 12, 0, DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(2020, 12, 4, 16, 59, 0, DateTimeOffset.Now.Offset),
};";

    private readonly string example2RazorCode = @"
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />
<BitDateRangePicker MaxRange=""new TimeSpan(2, 4, 30, 0)"" ShowTimePicker />";

    private readonly string example3RazorCode = @"
<BitDateRangePicker ShowTimePicker
                    Label=""HourStep = 2""
                    HourStep=""2"" />

<BitDateRangePicker ShowTimePicker
                    Label=""MinuteStep = 15""
                    MinuteStep=""15"" />";

    private readonly string example4RazorCode = @"
<BitDateRangePicker Label=""DateFormat: 'dd=MM(yy)'"" DateFormat=""dd=MM(yy)"" />
<BitDateRangePicker Label=""ValueFormat: 'Dep: {0}, Arr: {1}'"" ValueFormat=""Dep: {0}, Arr: {1}"" />";

    private readonly string example5RazorCode = @"
<BitDateRangePicker @bind-Value=""@selectedDateRange"" />
<div>From: <b>@selectedDateRange?.StartDate.ToString()</b></div>
<div>To: <b>@selectedDateRange?.EndDate.ToString()</b></div>";
    private readonly string example5CsharpCode = @"
private BitDateRangePickerValue? selectedDateRange = new()
{
    StartDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(2020, 1, 25, 0, 0, 0, DateTimeOffset.Now.Offset)
};";

    private readonly string example6RazorCode = @"
<BitDateRangePicker Label=""fa-IR culture with Farsi names""
                    GoToTodayTitle=""برو به امروز""
                    ValueFormat=""شروع: {0}, پایان: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDateRangePicker Label=""fa-IR culture with Fingilish names""
                    GoToTodayTitle=""Boro be emrouz""
                    ValueFormat=""Shoro: {0}, Payan: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example7RazorCode = @"
<BitDateRangePicker @bind-Value=""@timeZoneDateRange1"" ShowTimePicker />
<div>Selected date range: from @(timeZoneDateRange1?.StartDate?.ToString() ?? ""-"") to @(timeZoneDateRange1?.EndDate?.ToString() ?? ""-"")</div>

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
    <BitDateRangePicker TimeZone=""timeZoneInfo"" @bind-Value=""@timeZoneDateRange2"" ShowTimePicker />
    <div>Selected date range: from @(timeZoneDateRange2?.StartDate?.ToString() ?? ""-"") to @(timeZoneDateRange2?.EndDate?.ToString() ?? ""-"")</div>
}";
    private readonly string example7CsharpCode = @"
private BitDateRangePickerValue? timeZoneDateRange1 = new();
private BitDateRangePickerValue? timeZoneDateRange2 = new();";

    private readonly string example8RazorCode = @"
<BitDateRangePicker Label=""Basic DatePicker"" Standalone />
<BitDateRangePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitDateRangePicker Label=""Week numbers"" ShowWeekNumbers Standalone />
<BitDateRangePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth Standalone />
<BitDateRangePicker Label=""TimePicker"" ShowTimePicker Standalone />";

    private readonly string example9RazorCode = @"
<BitDateRangePicker Label=""Basic"" ReadOnly @bind-Value=""readOnlyDateRange"" />
<BitDateRangePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""readOnlyDateRange"" />
<BitDateRangePicker Label=""Standalone"" ReadOnly Standalone @bind-Value=""readOnlyDateRange"" />
<BitDateRangePicker Label=""Standalone with TimePicker"" ReadOnly ShowTimePicker Standalone @bind-Value=""readOnlyDateRange"" />";
    private readonly string example9CsharpCode = @"
private BitDateRangePickerValue? readOnlyDateRange = new()
{
    StartDate = new DateTimeOffset(2024, 12, 8, 12, 15, 0, DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(2024, 12, 12, 16, 45, 0, DateTimeOffset.Now.Offset),
};";

    private readonly string example10RazorCode = @"
<BitDateRangePicker>
    <LabelTemplate>
        Custom label <BitIcon IconName=""@BitIconName.Calendar"" />
    </LabelTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""DayCellTemplate"">
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""MonthCellTemplate"">
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""YearCellTemplate"">
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitDateRangePicker>";
    private readonly string example10CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example11RazorCode = @"
<BitDateRangePicker Label=""Responsive DateRangePicker""
                    Responsive
                    ShowWeekNumbers
                    Placeholder=""Select a date range"" />";

    private readonly string example12RazorCode = @"
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

    .custom-start-end {
        color: black;
        background-color: blueviolet;
    }

    .custom-selected-days {
        background-color: #9726ff5e;
    }

    .custom-year-picker {
        border: 1px solid #9726ff5e;
        background-color: blueviolet;
        border-end-end-radius: 0.5rem;
        border-start-end-radius: 0.5rem;
    }
</style>


<BitDateRangePicker Style=""margin: 1rem; box-shadow: dodgerblue 0 0 1rem;"" />

<BitDateRangePicker Class=""custom-class"" />


<BitDateRangePicker ShowTimePicker
                    Styles=""@(new() { Root = ""margin-inline: 1rem;"",
                                      Focused = ""--focused-background: #b2b2b25a;"",
                                      Input = ""padding: 0.5rem;"",
                                      InputContainer = ""background: var(--focused-background);"",
                                      Group = ""border: 1px solid mediumseagreen; background: #1c73324d;"",
                                      Divider = ""border-color: mediumseagreen;"",
                                      DayPickerMonth = ""color: darkgreen;"",
                                      TodayDayButton = ""background-color: green;"",
                                      SelectedDayButtons = ""background-color: #36fa368c;"",
                                      EndTimeInputContainer = ""margin-top: 0.5rem;"",
                                      StartAndEndSelectionDays = ""background-color: limegreen;"",
                                      StartTimeIncreaseHourButton = ""background-color: limegreen;"",
                                      StartTimeIncreaseMinuteButton = ""background-color: limegreen;"",
                                      StartTimeDecreaseHourButton = ""background-color: limegreen;"",
                                      StartTimeDecreaseMinuteButton = ""background-color: limegreen;"",
                                      EndTimeIncreaseHourButton = ""background-color: limegreen;"",
                                      EndTimeIncreaseMinuteButton = ""background-color: limegreen;"",
                                      EndTimeDecreaseHourButton = ""background-color: limegreen;"",
                                      EndTimeDecreaseMinuteButton = ""background-color: limegreen;"" })"" />

<BitDateRangePicker @bind-Value=""@classesValue""
                    Label=""Select a date""
                    Classes=""@(new() { Root = ""custom-root"",
                                       Focused = ""custom-focus"",
                                       Input = ""custom-input"",
                                       InputContainer = ""custom-input-container"",
                                       Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"",
                                       Callout = ""custom-callout"",
                                       DayPickerWrapper = ""custom-day-picker"",
                                       DayButton = ""custom-day"",
                                       TodayDayButton = ""custom-today-day"",
                                       StartAndEndSelectionDays = ""custom-start-end"",
                                       SelectedDayButtons = ""custom-selected-days"",
                                       PrevMonthNavButton = ""custom-prev-month"",
                                       NextMonthNavButton = ""custom-next-month"",
                                       DayPickerMonth = ""custom-day-month"",
                                       DayPickerHeader = ""custom-day-header"",
                                       WeekNumbersHeader = ""custom-week-header"",
                                       YearMonthPickerWrapper = ""custom-year-picker"" })"" />";
    private readonly string example12CsharpCode = @"
private BitDateRangePickerValue? classesValue;";

    private readonly string example13RazorCode = @"
<BitDateRangePicker Dir=""BitDir.Rtl"" />";
}
