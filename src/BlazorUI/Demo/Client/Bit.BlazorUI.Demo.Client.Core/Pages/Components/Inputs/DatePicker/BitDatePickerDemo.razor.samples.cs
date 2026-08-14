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
<BitDatePicker Label=""Show close button"" ShowCloseButton />
<BitDatePicker Label=""AutoClose (false)"" AutoClose=""false"" />
<BitDatePicker Label=""AllowDeselect (re-select the selected day to clear)"" AllowDeselect AutoClose=""false"" />
<BitDatePicker Label=""StartingValue: December 2020, 20:45"" ShowTimePicker StartingValue=""startingValue"" />
<BitDatePicker Label=""Custom Today (March 2021)"" Today=""customToday"" />";

    private readonly string example1CsharpCode = @"
private DateTimeOffset? customToday = new DateTimeOffset(2021, 3, 15, 0, 0, 0, DateTimeOffset.Now.Offset);
private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);";

    private readonly string example2RazorCode = @"
<BitDatePicker MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />

<BitDatePicker Label=""DisablePast (from today on)"" DisablePast />
<BitDatePicker Label=""DisableFuture (up to today)"" DisableFuture />";

    private readonly string example3RazorCode = @"
<BitDatePicker Label=""DisabledDaysOfWeek (weekends)"" DisabledDaysOfWeek=""@weekendDays"" />

<BitDatePicker Label=""DisabledDates (a few upcoming dates)"" DisabledDates=""@disabledDates"" />

<BitDatePicker Label=""IsDateDisabled (odd days)"" IsDateDisabled=""@(d => d.Day % 2 == 1)"" />";
    private readonly string example3CsharpCode = @"
private readonly DayOfWeek[] weekendDays = [DayOfWeek.Friday, DayOfWeek.Saturday];

private readonly DateTimeOffset[] disabledDates =
[
    DateTimeOffset.Now.AddDays(2),
    DateTimeOffset.Now.AddDays(3),
    DateTimeOffset.Now.AddDays(7)
];";

    private readonly string example4RazorCode = @"
<style>
    .sunday-cell {
        color: red;
    }
</style>


<BitDatePicker Label=""HighlightedDates"" HighlightedDates=""@highlightedDates"" />

<BitDatePicker Label=""GetDayClass (Sundays)""
               GetDayClass='@(d => d.DayOfWeek == DayOfWeek.Sunday ? ""sunday-cell"" : null)' />

<BitDatePicker Label=""HighlightToday (false)"" HighlightToday=""false"" />";
    private readonly string example4CsharpCode = @"
private readonly DateTimeOffset[] highlightedDates =
[
    DateTimeOffset.Now.AddDays(1),
    DateTimeOffset.Now.AddDays(5),
    DateTimeOffset.Now.AddDays(10)
];";

    private readonly string example5RazorCode = @"
<BitDatePicker Label=""FirstDayOfWeek (Monday)"" FirstDayOfWeek=""DayOfWeek.Monday"" />

<BitDatePicker Label=""ISO 8601 week numbers""
               ShowWeekNumbers
               FirstDayOfWeek=""DayOfWeek.Monday""
               WeekNumberRule=""CalendarWeekRule.FirstFourDayWeek"" />

<BitDatePicker Label=""ShowOutsideDays (false)"" ShowOutsideDays=""false"" />

<BitDatePicker Label=""FixedWeeks (always six weeks)"" FixedWeeks />";

    private readonly string example6RazorCode = @"
<BitDatePicker Label=""IsMonthPickerVisible"" IsMonthPickerVisible=""@isMonthPickerVisible"" />
<BitToggleButton OnText=""MonthPicker visible"" OffText=""MonthPicker hidden"" @bind-IsChecked=""@isMonthPickerVisible"" />

<BitDatePicker Label=""ShowMonthPickerAsOverlay"" ShowMonthPickerAsOverlay=""@showMonthPickerAsOverlay"" />
<BitToggleButton OnText=""Position overlay"" OffText=""Position besides"" @bind-IsChecked=""@showMonthPickerAsOverlay"" />";
    private readonly string example6CsharpCode = @"
private bool isMonthPickerVisible = true;
private bool showMonthPickerAsOverlay;";

    private readonly string example7RazorCode = @"
<BitDatePicker Label=""Basic time picker (24-hour)"" ShowTimePicker @bind-Value=""@selectedDateTime"" />
<div>Selected DateTime: @selectedDateTime.ToString()</div>

<BitDatePicker Label=""TimeFormat (12-hour with AM/PM)"" ShowTimePicker TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitDatePicker Label=""ShowTimePickerAsOverlay"" ShowTimePicker ShowTimePickerAsOverlay />

<BitDatePicker Label=""Without the now button"" ShowTimePicker ShowNowButton=""false"" />";
    private readonly string example7CsharpCode = @"
private DateTimeOffset? selectedDateTime;";

    private readonly string example8RazorCode = @"
<BitDatePicker ShowTimePicker
               Label=""HourStep = 2""
               HourStep=""2"" />

<BitDatePicker ShowTimePicker
               Label=""MinuteStep = 15""
               MinuteStep=""15"" />";

    private readonly string example9RazorCode = @"
<BitDatePicker Label=""Formatted Date""
               DateFormat=""dd=MM(yy)""
               Placeholder=""Select a date"" />";

    private readonly string example10RazorCode = @"
<BitDatePicker Label=""Text input allowed""
               AllowTextInput
               DateFormat=""dd/MM/yyyy""
               Placeholder=""Enter a date (dd/MM/yyyy)"" />

<BitDatePicker Label=""With custom error messages""
               AllowTextInput
               DateFormat=""dd/MM/yyyy""
               MinDate=""DateTimeOffset.Now.AddDays(-5)""
               MaxDate=""DateTimeOffset.Now.AddDays(5)""
               InvalidErrorMessage=""The date format must be dd/MM/yyyy.""
               OutOfRangeErrorMessage=""Only the ±5 days around today are allowed.""
               Placeholder=""Enter a date (dd/MM/yyyy)"" />";

    private readonly string example11RazorCode = @"
<BitDatePicker Label=""Two-way binding"" @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>

<BitDatePicker Label=""DefaultValue & OnChange""
               DefaultValue=""DateTimeOffset.Now""
               OnChange=""v => changedDate = v"" />
<div>Changed date: @(changedDate?.ToString() ?? ""-"")</div>";
    private readonly string example11CsharpCode = @"
private DateTimeOffset? changedDate;
private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example12RazorCode = @"
<BitDatePicker Label=""fa-IR culture with Farsi names""
               GoToTodayTitle=""برو به امروز""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDatePicker Label=""fa-IR culture with Fingilish names""
               GoToTodayTitle=""Boro be emrouz""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example13RazorCode = @"
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
    private readonly string example13CsharpCode = @"
private DateTimeOffset? timeZoneDate1;
private DateTimeOffset? timeZoneDate2;";

    private readonly string example14RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" Standalone />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitDatePicker Label=""Week numbers"" ShowWeekNumbers Standalone />
<BitDatePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth Standalone />
<BitDatePicker Label=""TimePicker"" ShowTimePicker Standalone />";

    private readonly string example15RazorCode = @"
<BitDatePicker Label=""Basic MonthPicker""
               Placeholder=""Select a month""
               Mode=""BitDatePickerMode.MonthPicker"" />

<BitDatePicker @bind-Value=""monthPickerDate""
               Placeholder=""Select a month""
               Label=""MonthPicker with binding""
               Mode=""BitDatePickerMode.MonthPicker"" />
<div>Selected Date: @(monthPickerDate?.ToString(""yyyy/MM/dd HH:mm:ss"") ?? ""None"")</div>

<BitDatePicker Placeholder=""Select a month""
               Label=""MonthPicker with Min/Max""
               Mode=""BitDatePickerMode.MonthPicker""
               MaxDate=""DateTimeOffset.Now.AddMonths(6)""
               MinDate=""DateTimeOffset.Now.AddMonths(-6)"" />

<BitDatePicker HighlightCurrentMonth
               HighlightSelectedMonth
               Placeholder=""Select a month""
               Mode=""BitDatePickerMode.MonthPicker""
               Label=""MonthPicker with highlighting"" />

<BitDatePicker Standalone
               Label=""Standalone MonthPicker""
               Mode=""BitDatePickerMode.MonthPicker"" />";
    private readonly string example15CsharpCode = @"
private DateTimeOffset? monthPickerDate;";

    private readonly string example16RazorCode = @"
<BitDatePicker Label=""Basic"" ReadOnly @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Text input allowed"" ReadOnly AllowTextInput @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Standalone"" ReadOnly Standalone @bind-Value=""readOnlyDate"" />
<BitDatePicker Label=""Standalone with TimePicker"" ReadOnly ShowTimePicker Standalone @bind-Value=""readOnlyDate"" />";
    private readonly string example16CsharpCode = @"
private DateTimeOffset? readOnlyDate = DateTimeOffset.Now;";

    private readonly string example17RazorCode = @"
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

    .callout-header {
        font-weight: 600;
        text-align: center;
        padding: 0.5rem 1rem;
        border-bottom: 1px solid gray;
    }

    .callout-footer {
        display: flex;
        padding: 0.25rem;
        justify-content: space-around;
        border-top: 1px solid gray;
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
        <div style=""padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
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
</BitDatePicker>

<BitDatePicker @ref=""presetsPicker"" @bind-Value=""presetsValue"" Label=""Callout header & footer (presets)"">
    <CalloutHeaderTemplate>
        <div class=""callout-header"">Pick your appointment</div>
    </CalloutHeaderTemplate>
    <CalloutFooterTemplate>
        <div class=""callout-footer"">
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => SelectPreset(0)"">Today</BitButton>
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => SelectPreset(1)"">Tomorrow</BitButton>
            <BitButton Variant=""BitVariant.Text"" OnClick=""() => SelectPreset(7)"">In a week</BitButton>
        </div>
    </CalloutFooterTemplate>
</BitDatePicker>";
    private readonly string example17CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;

private DateTimeOffset? presetsValue;
private BitDatePicker? presetsPicker;

private async Task SelectPreset(int days)
{
    presetsValue = DateTimeOffset.Now.Date.AddDays(days);

    if (presetsPicker is not null)
    {
        await presetsPicker.CloseCalloutAndFocus();
    }
}";

    private readonly string example18RazorCode = @"
<BitDatePicker Label=""Responsive DatePicker""
               Responsive
               ShowWeekNumbers
               Placeholder=""Select a date"" />";

    private readonly string example19RazorCode = @"
<BitDatePicker Label=""OnSelectDate & OnMonthChange""
               OnSelectDate=""v => selectedDateEvent = v""
               OnMonthChange=""v => displayedMonth = v"" />
<div>Selected date: @(selectedDateEvent?.ToString() ?? ""-"")</div>
<div>Displayed month: @(displayedMonth?.ToString(""MMMM yyyy"") ?? ""-"")</div>

<BitDatePicker Label=""OnClick & OnFocusIn & OnFocusOut & OnClear""
               ShowClearButton
               OnClick=""() => clickCount++""
               OnFocusIn=""() => focusInCount++""
               OnFocusOut=""() => focusOutCount++""
               OnClear=""() => clearCount++"" />
<div>Clicked: @clickCount times</div>
<div>Focused in: @focusInCount times</div>
<div>Focused out: @focusOutCount times</div>
<div>Cleared: @clearCount times</div>

<BitDatePicker Label=""OnOpen & OnClose""
               OnOpen=""() => openCount++""
               OnClose=""() => closeCount++"" />
<div>Opened: @openCount times</div>
<div>Closed: @closeCount times</div>";
    private readonly string example19CsharpCode = @"
private int clickCount;
private int clearCount;
private int focusInCount;
private int focusOutCount;
private DateTimeOffset? displayedMonth;
private DateTimeOffset? selectedDateEvent;
private int openCount;
private int closeCount;";

    private readonly string example20RazorCode = @"
<BitDatePicker Label=""Try the keyboard"" ShowWeekNumbers />

<BitDatePicker Label=""Try the keyboard (month picker)"" Mode=""BitDatePickerMode.MonthPicker"" />";

    private readonly string example21RazorCode = @"
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
    private readonly string example21CsharpCode = @"
public class BitDatePickerValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private string SuccessMessage = string.Empty;
private BitDatePickerValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example22RazorCode = @"
<BitDatePicker @ref=""programmaticPicker"" @bind-IsOpen=""isCalloutOpen"" Label=""Controlled callout"" />

<div>IsOpen: @isCalloutOpen</div>

<BitButton OnClick=""() => programmaticPicker?.OpenCallout()"">OpenCallout()</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => programmaticPicker?.CloseCalloutAndFocus()"">
    CloseCalloutAndFocus()
</BitButton>";
    private readonly string example22CsharpCode = @"
private bool isCalloutOpen;
private BitDatePicker? programmaticPicker;";

    private readonly string example23RazorCode = @"
<BitDatePicker Label=""Underlined"" Underlined />

<BitDatePicker Label=""HasBorder (false)"" HasBorder=""false"" />

<BitDatePicker Label=""IconLocation (Left)"" IconLocation=""BitIconLocation.Left"" />

<BitDatePicker Label=""IconName"" IconName=""@BitIconName.Calendar"" />";

    private readonly string example24RazorCode = @"
<BitDatePicker Label=""Primary"" Color=""BitColor.Primary"" HighlightCurrentMonth />
<BitDatePicker Label=""Secondary"" Color=""BitColor.Secondary"" HighlightCurrentMonth />
<BitDatePicker Label=""Tertiary"" Color=""BitColor.Tertiary"" HighlightCurrentMonth />
<BitDatePicker Label=""Info"" Color=""BitColor.Info"" HighlightCurrentMonth />
<BitDatePicker Label=""Success"" Color=""BitColor.Success"" HighlightCurrentMonth />
<BitDatePicker Label=""Warning"" Color=""BitColor.Warning"" HighlightCurrentMonth />
<BitDatePicker Label=""SevereWarning"" Color=""BitColor.SevereWarning"" HighlightCurrentMonth />
<BitDatePicker Label=""Error"" Color=""BitColor.Error"" HighlightCurrentMonth />
<BitDatePicker Label=""PrimaryBackground"" Color=""BitColor.PrimaryBackground"" HighlightCurrentMonth />
<BitDatePicker Label=""SecondaryBackground"" Color=""BitColor.SecondaryBackground"" HighlightCurrentMonth />
<BitDatePicker Label=""TertiaryBackground"" Color=""BitColor.TertiaryBackground"" HighlightCurrentMonth />
<BitDatePicker Label=""PrimaryForeground"" Color=""BitColor.PrimaryForeground"" HighlightCurrentMonth />
<BitDatePicker Label=""SecondaryForeground"" Color=""BitColor.SecondaryForeground"" HighlightCurrentMonth />
<BitDatePicker Label=""TertiaryForeground"" Color=""BitColor.TertiaryForeground"" HighlightCurrentMonth />
<BitDatePicker Label=""PrimaryBorder"" Color=""BitColor.PrimaryBorder"" HighlightCurrentMonth />
<BitDatePicker Label=""SecondaryBorder"" Color=""BitColor.SecondaryBorder"" HighlightCurrentMonth />
<BitDatePicker Label=""TertiaryBorder"" Color=""BitColor.TertiaryBorder"" HighlightCurrentMonth />";

    private readonly string example25RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitDatePicker Label=""Icon (string)"" Icon=""@(""fa-solid fa-calendar-days"")"" />

<BitDatePicker Label=""BitIconInfo.Css"" Icon=""@BitIconInfo.Css(""fa-solid fa-calendar-days"")"" />

<BitDatePicker Label=""BitIconInfo.Fa"" Icon=""@BitIconInfo.Fa(""solid calendar"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitDatePicker Label=""Icon (string)"" Icon=""@(""bi bi-calendar3"")"" />

<BitDatePicker Label=""BitIconInfo.Css"" Icon=""@BitIconInfo.Css(""bi bi-calendar3-event"")"" />

<BitDatePicker Label=""BitIconInfo.Bi"" Icon=""@BitIconInfo.Bi(""calendar3"")"" />


<BitDatePicker Label=""Custom nav icons (FA)""
               PrevMonthNavIcon=""@BitIconInfo.Fa(""solid chevron-left"")""
               NextMonthNavIcon=""@BitIconInfo.Fa(""solid chevron-right"")""
               PrevYearNavIcon=""@BitIconInfo.Fa(""solid angles-left"")""
               NextYearNavIcon=""@BitIconInfo.Fa(""solid angles-right"")""
               PrevYearRangeNavIcon=""@BitIconInfo.Fa(""solid angles-left"")""
               NextYearRangeNavIcon=""@BitIconInfo.Fa(""solid angles-right"")""
               GoToTodayIcon=""@BitIconInfo.Fa(""solid calendar-day"")"" />

<BitDatePicker Label=""TimePicker icons (FA)""
               ShowTimePicker
               ShowClearButton
               NowButtonIcon=""@BitIconInfo.Fa(""solid clock"")""
               HideTimePickerIcon=""@BitIconInfo.Fa(""solid calendar"")""
               TimePickerIncreaseHourIcon=""@BitIconInfo.Fa(""solid chevron-up"")""
               TimePickerDecreaseHourIcon=""@BitIconInfo.Fa(""solid chevron-down"")""
               TimePickerIncreaseMinuteIcon=""@BitIconInfo.Fa(""solid chevron-up"")""
               TimePickerDecreaseMinuteIcon=""@BitIconInfo.Fa(""solid chevron-down"")""
               ClearButtonIcon=""@BitIconInfo.Fa(""solid xmark"")"" />

<BitDatePicker Label=""Custom nav icons (Bootstrap)""
               PrevMonthNavIcon=""@BitIconInfo.Bi(""chevron-left"")""
               NextMonthNavIcon=""@BitIconInfo.Bi(""chevron-right"")""
               PrevYearNavIcon=""@BitIconInfo.Bi(""chevron-double-left"")""
               NextYearNavIcon=""@BitIconInfo.Bi(""chevron-double-right"")""
               PrevYearRangeNavIcon=""@BitIconInfo.Bi(""chevron-double-left"")""
               NextYearRangeNavIcon=""@BitIconInfo.Bi(""chevron-double-right"")""
               GoToTodayIcon=""@BitIconInfo.Bi(""calendar-event"")"" />";

    private readonly string example26RazorCode = @"
<BitDatePicker Label=""Small"" Size=""BitSize.Small"" />
<BitDatePicker Label=""Medium"" Size=""BitSize.Medium"" />
<BitDatePicker Label=""Large"" Size=""BitSize.Large"" />";

    private readonly string example27RazorCode = @"
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

    .custom-highlighted-day {
        border-color: violet;
        background-color: #ee82ee40;
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
               HighlightedDates=""@highlightedDates""
               Classes=""@(new() { Root = ""custom-root"",
                                  Focused = ""custom-focus"",
                                  Input = ""custom-input"",
                                  InputContainer = ""custom-input-container"",
                                  Label = $""custom-label{(classesValue is null ? string.Empty : "" custom-label-top"")}"",
                                  DayPickerWrapper = ""custom-day-picker"",
                                  DayButton = ""custom-day"",
                                  TodayDayButton = ""custom-today-day"",
                                  SelectedDayButton = ""custom-selected-day"",
                                  HighlightedDayButton = ""custom-highlighted-day"",
                                  PrevMonthNavButton = ""custom-prev-month"",
                                  NextMonthNavButton = ""custom-next-month"",
                                  DayPickerMonth = ""custom-day-month"",
                                  DayPickerHeader = ""custom-day-header"",
                                  DayNameHeader = ""custom-week-header"",
                                  YearMonthPickerWrapper = ""custom-year-picker"" })"" />";
    private readonly string example27CsharpCode = @"
private DateTimeOffset? classesValue;

private readonly DateTimeOffset[] highlightedDates =
[
    DateTimeOffset.Now.AddDays(1),
    DateTimeOffset.Now.AddDays(5),
    DateTimeOffset.Now.AddDays(10)
];";

    private readonly string example28RazorCode = @"
<BitDatePicker Dir=""BitDir.Rtl"" />";
}
