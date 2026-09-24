namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public partial class BitCalendarDemo
{
    private readonly string example1RazorCode = @"
<BitCalendar />

<BitCalendar IsEnabled=""false"" />

<BitCalendar ReadOnly />

<BitCalendar ShowGoToToday=""false"" />";

    private readonly string example2RazorCode = @"
<BitCalendar @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>

<BitCalendar OnSelectDate=""HandleOnSelectDate"" />
<div>Selected date: @(onSelectDate.HasValue ? onSelectDate.ToString() : ""-"")</div>

<BitCalendar AllowDeselect @bind-Value=""@deselectableDate"" />
<div>Selected date: @(deselectableDate.HasValue ? deselectableDate.ToString() : ""-"")</div>";
    private readonly string example2CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);

private DateTimeOffset? deselectableDate = DateTimeOffset.Now;

private DateTimeOffset? onSelectDate;

private void HandleOnSelectDate(DateTimeOffset? date)
{
    onSelectDate = date;
}";

    private readonly string example3RazorCode = @"
<BitCalendar StartingValue=""startingValue"" />

<BitCalendar Today=""customToday"" />

<BitCalendar OnMonthChange=""HandleOnMonthChange"" />
<div>Displayed month: @(displayedMonth.HasValue ? displayedMonth.Value.ToString(""MMMM yyyy"") : ""-"")</div>";
    private readonly string example3CsharpCode = @"
private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);
private DateTimeOffset? customToday = new DateTimeOffset(2021, 3, 15, 0, 0, 0, DateTimeOffset.Now.Offset);

private DateTimeOffset? displayedMonth;

private void HandleOnMonthChange(DateTimeOffset month)
{
    displayedMonth = month;
}";

    private readonly string example4RazorCode = @"
<BitCalendar MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />

<BitCalendar MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />

<BitCalendar DisablePast />

<BitCalendar DisableFuture />";

    private readonly string example5RazorCode = @"
<style>
    .sunday-cell {
        color: red;
    }
</style>

<BitCalendar DisabledDaysOfWeek=""@weekendDays"" />

<BitCalendar DisabledDates=""@disabledDates"" IsDateDisabled=""@(d => d.Day % 2 == 1)"" />

<BitCalendar HighlightedDates=""@highlightedDates""
             GetDayClass=""@(d => d.DayOfWeek == DayOfWeek.Sunday ? ""sunday-cell"" : null)"" />";
    private readonly string example5CsharpCode = @"
private DayOfWeek[] weekendDays = [DayOfWeek.Saturday, DayOfWeek.Sunday];

private DateTimeOffset[] disabledDates =
[
    DateTimeOffset.Now.AddDays(1),
    DateTimeOffset.Now.AddDays(2),
    DateTimeOffset.Now.AddDays(5)
];

private DateTimeOffset[] highlightedDates =
[
    DateTimeOffset.Now.AddDays(3),
    DateTimeOffset.Now.AddDays(7),
    DateTimeOffset.Now.AddDays(14)
];";

    private readonly string example6RazorCode = @"
<BitCalendar ShowWeekNumbers FirstDayOfWeek=""DayOfWeek.Monday""
             WeekNumberRule=""CalendarWeekRule.FirstFourDayWeek"" />

<BitCalendar ShowOutsideDays=""false"" />

<BitCalendar FixedWeeks />";

    private readonly string example7RazorCode = @"
<BitCalendar ShowMonthPicker=""@showMonthPicker"" />
<BitToggleButton OnText=""MonthPicker visible"" OffText=""MonthPicker invisible"" @bind-IsChecked=""@showMonthPicker"" />

<BitCalendar ShowMonthPickerAsOverlay=""@showMonthPickerAsOverlay"" />
<BitToggleButton OnText=""Position Overlay"" OffText=""Position Besides"" @bind-IsChecked=""@showMonthPickerAsOverlay"" />

<BitCalendar HighlightCurrentMonth HighlightSelectedMonth />";
    private readonly string example7CsharpCode = @"
private bool showMonthPicker = true;
private bool showMonthPickerAsOverlay;";

    private readonly string example8RazorCode = @"
<BitCalendar @bind-Value=""@selectedDateTime"" ShowTimePicker />
<div>Selected: @selectedDateTime.ToString()</div>

<BitCalendar ShowTimePicker ShowNowButton=""false"" TimeFormat=""BitTimeFormat.TwelveHours"" />

<BitCalendar ShowTimePicker ShowTimePickerAsOverlay />

<BitCalendar ShowTimePicker HourStep=""3"" MinuteStep=""15"" />

<BitCalendar ShowTimePicker @bind-Value=""@boundedDateTime""
             MinDate=""boundedMinDate"" MaxDate=""boundedMaxDate"" />
<div>Selected: @boundedDateTime.ToString()</div>";
    private readonly string example8CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;

private DateTimeOffset? boundedDateTime = DateTime.Today.AddHours(12);
private DateTimeOffset boundedMinDate = DateTime.Today.AddHours(9).AddMinutes(30);
private DateTimeOffset boundedMaxDate = DateTime.Today.AddDays(2).AddHours(17);";

    private readonly string example9RazorCode = @"
<BitCalendar Events=""@calendarEvents"" />

<BitCalendar Events=""@coloredEvents"" />

<BitCalendar Events=""@calendarEvents"" ShowEventDetails=""false"" OnSelectDate=""HandleOnEventDayClick"" />
<div>Events of the selected day: @eventsOfSelectedDay</div>

<BitCalendar Events=""@calendarEvents"">
    <EventTemplate>
        <BitStack Horizontal AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
            <b>@context.Title</b>
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text""
                       OnClick=""() => openedEvent = context.Title"">
                Open
            </BitButton>
        </BitStack>
    </EventTemplate>
</BitCalendar>
<div>Opened: @(openedEvent ?? ""-"")</div>";
    private readonly string example9CsharpCode = @"
private List<BitCalendarEvent> calendarEvents =
[
    new() { Title = ""Team standup"",
            Body = ""Daily sync with the engineering team."",
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30) },
    new() { Title = ""Product review"",
            Body = ""Quarterly product review - prepare slides beforehand."",
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0) },
    new() { Title = ""All-day workshop"",
            Body = ""Full-day frontend architecture workshop."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)) },
    new() { Title = ""Retro"",
            Body = ""Sprint retrospective."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(17, 0) },
    new() { Title = ""Client call"",
            Body = ""Introductory call with the new client."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            StartTime = new TimeOnly(11, 30) }
];

private List<BitCalendarEvent> coloredEvents =
[
    new() { Title = ""Release 2.4"",
            Body = ""Ship the release once the pipeline is green."",
            Color = BitColor.Success,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) },
    new() { Title = ""On-call handover"",
            Color = BitColor.Warning,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(10, 0) },
    new() { Title = ""Incident review"",
            Color = BitColor.Error,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            StartTime = new TimeOnly(15, 0),
            EndTime = new TimeOnly(16, 0) },
    new() { Title = ""Design sync"",
            Color = BitColor.Info,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            StartTime = new TimeOnly(13, 0) }
];

private string eventsOfSelectedDay = ""-"";

private void HandleOnEventDayClick(DateTimeOffset? date)
{
    if (date is null)
    {
        eventsOfSelectedDay = ""-"";
        return;
    }

    var day = DateOnly.FromDateTime(date.Value.DateTime);
    var titles = calendarEvents.Where(e => e.Date == day).Select(e => e.Title).ToArray();

    eventsOfSelectedDay = titles.Length == 0 ? ""none"" : string.Join("", "", titles);
}

private string? openedEvent;";

    private readonly string example10RazorCode = @"
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

    .month-cell {
        width: 28px;
        padding: 3px;
        color: black;
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
        <div class=""month-cell"" style=""background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitCalendar>

<BitCalendar @bind-Value=""@footerDate"">
    <HeaderTemplate>
        <b>Pick a delivery date</b>
    </HeaderTemplate>
    <FooterTemplate>
        <BitStack Horizontal AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text""
                       OnClick=""() => footerDate = DateTimeOffset.Now"">
                Today
            </BitButton>
            <BitButton Size=""BitSize.Small"" Variant=""BitVariant.Text""
                       Color=""BitColor.SecondaryForeground""
                       OnClick=""() => footerDate = null"">
                Clear
            </BitButton>
        </BitStack>
    </FooterTemplate>
</BitCalendar>
<div>Selected: @(footerDate.HasValue ? footerDate.Value.ToString(""d"") : ""-"")</div>";

    private readonly string example10CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;

private DateTimeOffset? footerDate;";

    private readonly string example11RazorCode = @"
<BitCalendar GoToTodayTitle=""برو به امروز"" Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitCalendar GoToTodayTitle=""Boro be emrouz"" Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example12RazorCode = @"
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
    <div>""@timeZoneInfo.Id"":</div><br/>
    <BitCalendar TimeZone=""timeZoneInfo"" @bind-Value=""@timeZoneDate2"" ShowTimePicker />
    <div>Selected date: @timeZoneDate2?.ToString()</div>
}";
    private readonly string example12CsharpCode = @"
private DateTimeOffset? timeZoneDate1;
private DateTimeOffset? timeZoneDate2;";

    private readonly string example13RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitCalendar Required @bind-Value=""validationModel.Date"" />
    <ValidationMessage For=""@(() => validationModel.Date)"" />

    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset"" Variant=""BitVariant.Outline""
               OnClick=""() => { validationModel = new(); SuccessMessage=string.Empty; }"">
        Reset
    </BitButton>
</EditForm>

@if (string.IsNullOrEmpty(SuccessMessage) is false)
{
    <BitMessage Color=""BitColor.Success"">@SuccessMessage</BitMessage>
}";
    private readonly string example13CsharpCode = @"
public class BitCalendarValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private string SuccessMessage = string.Empty;
private BitCalendarValidationModel validationModel = new();

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form was submitted successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private readonly string example14RazorCode = @"
<BitCalendar AriaLabel=""Departure date"" ShowTimePicker ShowWeekNumbers
             Events=""@calendarEvents"" TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example15RazorCode = @"
<BitCalendar MonthCount=""2"" ShowMonthPicker=""false"" @bind-Value=""@seasonDate"" />
<div>Selected: @(seasonDate.HasValue ? seasonDate.Value.ToString(""d"") : ""-"")</div>

<BitCalendar MonthCount=""3"" PagedNavigation ShowMonthPickerAsOverlay Events=""@calendarEvents"" />";
    private readonly string example15CsharpCode = @"
private DateTimeOffset? seasonDate;";

    private readonly string example16RazorCode = @"
@* The params object carries a default down to every calendar under it, and never overwrites what one set itself. *@
<BitParams Parameters=""@calendarParams"">
    <BitCalendar />

    <BitCalendar />

    <BitCalendar ShowMonthPicker=""false"" MinDate=""DateTimeOffset.Now.AddDays(-2)"" />
</BitParams>

<BitCalendar />

@code {
    private readonly BitCalendarParams[] calendarParams =
    [
        new()
        {
            ShowWeekNumbers = true,
            FirstDayOfWeek = DayOfWeek.Monday,
            WeekNumberRule = CalendarWeekRule.FirstFourDayWeek,
            HighlightCurrentMonth = true,
            MinDate = DateTimeOffset.Now.AddMonths(-1),
            MaxDate = DateTimeOffset.Now.AddMonths(1),
        }
    ];
}";

    private readonly string example17RazorCode = @"
<BitCalendar Color=""BitColor.Primary"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Secondary"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Tertiary"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Info"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Success"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Warning"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.SevereWarning"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.Error"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.PrimaryBackground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.SecondaryBackground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.TertiaryBackground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.PrimaryForeground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.SecondaryForeground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.TertiaryForeground"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.PrimaryBorder"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.SecondaryBorder"" HighlightCurrentMonth />

<BitCalendar Color=""BitColor.TertiaryBorder"" HighlightCurrentMonth />";

    private readonly string example18RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitCalendar GoToTodayIcon=""@BitIconInfo.Fa(""solid calendar-day"")""
             PrevMonthNavIcon=""@BitIconInfo.Fa(""solid chevron-left"")""
             NextMonthNavIcon=""@BitIconInfo.Fa(""solid chevron-right"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCalendar ShowTimePicker=""true""
             GoToTodayIcon=""@BitIconInfo.Bi(""calendar-check"")""
             NowButtonIcon=""@BitIconInfo.Bi(""clock"")""
             PrevMonthNavIcon=""@BitIconInfo.Bi(""chevron-left"")""
             NextMonthNavIcon=""@BitIconInfo.Bi(""chevron-right"")""
             TimePickerIncreaseHourIcon=""@BitIconInfo.Bi(""chevron-up"")""
             TimePickerDecreaseHourIcon=""@BitIconInfo.Bi(""chevron-down"")""
             TimePickerIncreaseMinuteIcon=""@BitIconInfo.Bi(""chevron-up"")""
             TimePickerDecreaseMinuteIcon=""@BitIconInfo.Bi(""chevron-down"")"" />";

    private readonly string example19RazorCode = @"
<BitCalendar Size=""BitSize.Small"" ShowWeekNumbers />

<BitCalendar Size=""BitSize.Medium"" ShowWeekNumbers />

<BitCalendar Size=""BitSize.Large"" ShowWeekNumbers />";

    private readonly string example20RazorCode = @"
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
                                YearMonthPickerWrapper = ""custom-year-picker"" })"" />


<BitCalendar @bind-Value=""@cssVarsDate""
             Style=""--bit-Calendar-selected-background: var(--bit-clr-pri);
                    --bit-Calendar-selected-color: var(--bit-clr-pri-text);
                    --bit-Calendar-selected-border-color: transparent;
                    --bit-Calendar-today-radius: var(--bit-shp-radius-sm);"" />

<BitCalendar ShowWeekNumbers
             Style=""--bit-Calendar-day-size: 2.75rem;
                    --bit-Calendar-day-font-size: 0.875rem;
                    --bit-Calendar-day-radius: 999px;
                    --bit-Calendar-week-number-background: transparent;
                    --bit-Calendar-week-number-color: var(--bit-clr-pri);"" />

<div style=""--bit-Calendar-background: var(--bit-clr-bg-sec);
            --bit-Calendar-radius: 1rem;
            --bit-Calendar-hover-background: var(--bit-clr-bg-ter);
            --bit-Calendar-event-color: var(--bit-clr-suc);
            --bit-Calendar-event-size: 0.375rem;
            --bit-Calendar-event-gap: 0.1875rem;"">
    <BitCalendar Events=""@calendarEvents"" />
</div>";

    private readonly string example20CsharpCode = @"
private DateTimeOffset? cssVarsDate = DateTimeOffset.Now.AddDays(2);

private List<BitCalendarEvent> calendarEvents =
[
    new() { Title = ""Team standup"",
            Body = ""Daily sync with the engineering team."",
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(9, 30) },
    new() { Title = ""Product review"",
            Body = ""Quarterly product review - prepare slides beforehand."",
            Date = DateOnly.FromDateTime(DateTime.Today),
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 0) },
    new() { Title = ""All-day workshop"",
            Body = ""Full-day frontend architecture workshop."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)) },
    new() { Title = ""Retro"",
            Body = ""Sprint retrospective."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            StartTime = new TimeOnly(16, 0),
            EndTime = new TimeOnly(17, 0) },
    new() { Title = ""Client call"",
            Body = ""Introductory call with the new client."",
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            StartTime = new TimeOnly(11, 30) }
];";

    private readonly string example21RazorCode = @"
<BitCalendar Dir=""BitDir.Rtl"" ShowTimePicker ShowWeekNumbers />";
}
