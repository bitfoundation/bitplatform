namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public partial class BitCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Classes",
            Type = "BitCalendarClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitCalendar.",
            Href = "#calendar-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "System.Globalization.CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the Calendar."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the Calendar."
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the day cell is rendered."
        },
        new()
        {
            Name = "GoToNextMonthTitle",
            Type = "string",
            DefaultValue = "Go to next month",
            Description = "The title of the Go to next month button (tooltip)."
        },
        new()
        {
            Name = "GoToNextYearRangeTitle",
            Type = "string",
            DefaultValue = "Next year range {0} - {1}",
            Description = "The title of the Go to next year range button (tooltip)."
        },
        new()
        {
            Name = "GoToNextYearTitle",
            Type = "string",
            DefaultValue = "Go to next year {0}",
            Description = "The title of the Go to next year button (tooltip)."
        },
        new()
        {
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button (tooltip)."
        },
        new()
        {
            Name = "GoToPrevYearRangeTitle",
            Type = "string",
            DefaultValue = "Previous year range {0} - {1}",
            Description = "The title of the Go to previous year range button (tooltip)."
        },
        new()
        {
            Name = "GoToPrevYearTitle",
            Type = "string",
            DefaultValue = "Go to previous year {0}",
            Description = "The title of the Go to previous year button (tooltip)."
        },
        new()
        {
            Name = "GoToTodayTitle",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "The title of the GoToToday button (tooltip)."
        },
        new()
        {
            Name = "GoToNowTitle",
            Type = "string",
            DefaultValue = "Go to now",
            Description = "The title of the GoToNow button (tooltip)."
        },
        new()
        {
            Name = "HighlightCurrentMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the current month."
        },
        new()
        {
            Name = "HighlightSelectedMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the selected month."
        },
        new()
        {
            Name = "HideTimePickerTitle",
            Type = "string",
            DefaultValue = "Hide time picker",
            Description = "The title of the HideTimePicker button (tooltip)."
        },
        new()
        {
            Name = "InvalidErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for the invalid value."
        },
        new()
        {
            Name = "ShowMonthPicker",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the month picker is shown or hidden."
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "null",
            Description = "The maximum allowable date of the calendar."
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum allowable date of the calendar."
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the month cell is rendered."
        },
        new()
        {
            Name = "MonthPickerToggleTitle",
            Type = "string",
            DefaultValue = "{0}, change month",
            Description = "The title of the month picker's toggle (tooltip)."
        },
        new()
        {
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date picker when visible."
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "Callback for when the user selects a date."
        },
        new()
        {
            Name = "SelectedDateAriaAtomic",
            Type = "string",
            DefaultValue = "Selected date {0}",
            Description = "The text of selected date aria-atomic of the calendar."
        },
        new()
        {
            Name = "ShowGoToToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the GoToToday button should be shown or not."
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the time picker should be shown or not."
        },
        new()
        {
            Name = "ShowTimePickerTitle",
            Type = "string",
            DefaultValue = "Show time picker",
            Description = "The title of the ShowTimePicker button (tooltip)."
        },
        new()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the week number (weeks 1 to 53) should be shown before each week row."
        },
        new()
        {
            Name = "ShowGoToNow",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the GoToNow button should be shown or not."
        },
        new()
        {
            Name = "StartingValue",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Specifies the date and time of the calendar when it is showing without any selected value.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitCalendarClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitCalendar.",
            Href = "#calendar-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "The time format of the time-picker, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum"
        },
        new()
        {
            Name = "WeekNumberTitle",
            Type = "string",
            DefaultValue = "Week number {0}",
            Description = "The title of the week number (tooltip)."
        },
        new()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the year cell is rendered."
        },
        new()
        {
            Name = "YearPickerToggleTitle",
            Type = "string",
            DefaultValue = "{0}, change year",
            Description = "The title of the year picker's toggle (tooltip)."
        },
        new()
        {
            Name = "YearRangePickerToggleTitle",
            Type = "string",
            DefaultValue = "{0} - {1}, change month",
            Description = "The title of the year range picker's toggle (tooltip)."
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for calendar's hour.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for calendar's minute.",
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "component-visibility-enum",
            Name = "BitVisibility",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name = "Visible",
                    Description = "Show content of the component.",
                    Value = "0"
                },
                new()
                {
                    Name = "Hidden",
                    Description = "Hide content of the component,though the space it takes on the page remains.",
                    Value = "1"
                },
                new()
                {
                    Name = "Collapsed",
                    Description = "Hide content of the component,though the space it takes on the page gone.",
                    Value = "2"
                }
            }
        },
        new()
        {
            Id = "time-format-enum",
            Name = "BitTimeFormat",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "TwentyFourHours",
                    Description="Show time pickers in 24 hours format.",
                    Value="0"
                },
                new()
                {
                    Name= "TwelveHours",
                    Description="Show time pickers in 12 hours format.",
                    Value="1"
                }
            }
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "calendar-class-styles",
            Title = "BitCalendarClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCalendar."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main container of the BitCalendar."
                },
                new()
                {
                    Name = "DayPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's wrapper of the BitCalendar."
                },
                new()
                {
                    Name = "DayPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's header of the BitCalendar."
                },
                new()
                {
                    Name = "DayPickerMonth",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's month of the BitCalendar."
                },
                new()
                {
                    Name = "DayPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the day-picker's nav buttons of the BitCalendar."
                },
                new()
                {
                    Name = "PrevMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month button of the BitCalendar."
                },
                new()
                {
                    Name = "PrevMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month icon of the BitCalendar."
                },
                new()
                {
                    Name = "GoToTodayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today button of the BitCalendar."
                },
                new()
                {
                    Name = "GoToTodayIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today icon of the BitCalendar."
                },
                new()
                {
                    Name = "NextMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month button of the BitCalendar."
                },
                new()
                {
                    Name = "NextMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month icon of the BitCalendar."
                },
                new()
                {
                    Name = "DaysHeaderRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header row of the days of the BitCalendar."
                },
                new()
                {
                    Name = "WeekNumbersHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the week numbers of the BitCalendar."
                },
                new()
                {
                    Name = "DaysRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the days of the BitCalendar."
                },
                new()
                {
                    Name = "WeekNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the week number of the BitCalendar."
                },
                new()
                {
                    Name = "DayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each day button of the BitCalendar."
                },
                new()
                {
                    Name = "TodayDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for today day button of the BitCalendar."
                },
                new()
                {
                    Name = "SelectedDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected day button of the BitCalendar."
                },
                new()
                {
                    Name = "TimePickerContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's main container of the BitCalendar."
                },
                new()
                {
                    Name = "TimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's wrapper of the BitCalendar."
                },
                new()
                {
                    Name = "TimePickerHourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour input of the BitCalendar."
                },
                new()
                {
                    Name = "TimePickerHourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour/minute separator of the BitCalendar."
                },
                new()
                {
                    Name = "TimePickerMinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's minute input of the BitCalendar."
                },
                new()
                {
                    Name = "Divider",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main divider of the BitCalendar."
                },
                new()
                {
                    Name = "YearMonthPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-month-picker's wrapper of the BitCalendar."
                },
                new()
                {
                    Name = "MonthPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's header of the BitCalendar."
                },
                new()
                {
                    Name = "YearPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's toggle button of the BitCalendar."
                },
                new()
                {
                    Name = "MonthPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the month-picker's nav buttons of the BitCalendar."
                },
                new()
                {
                    Name = "PrevYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year button of the BitCalendar."
                },
                new()
                {
                    Name = "PrevYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year icon of the BitCalendar."
                },
                new()
                {
                    Name = "NextYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year button of the BitCalendar."
                },
                new()
                {
                    Name = "NextYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year icon of the BitCalendar."
                },
                new()
                {
                    Name = "MonthsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the months of the BitCalendar."
                },
                new()
                {
                    Name = "MonthButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each month button of the BitCalendar."
                },
                new()
                {
                    Name = "YearPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's header of the BitCalendar."
                },
                new()
                {
                    Name = "MonthPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's toggle button of the BitCalendar."
                },
                new()
                {
                    Name = "YearPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the year-picker nav buttons of the BitCalendar."
                },
                new()
                {
                    Name = "PrevYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range button of the BitCalendar."
                },
                new()
                {
                    Name = "PrevYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range icon of the BitCalendar."
                },
                new()
                {
                    Name = "NextYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range button of the BitCalendar."
                },
                new()
                {
                    Name = "NextYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range icon of the BitCalendar."
                },
                new()
                {
                    Name = "YearsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the years of the BitCalendar."
                },
                new()
                {
                    Name = "YearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each year button of the BitCalendar."
                }
            }
        }
    };




    private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);

    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private bool showMonthPicker = true;
    private bool showMonthPickerAsOverlay;

    private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;
    private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);

    private string SuccessMessage = string.Empty;
    private BitCalendarValidationModel validationModel = new();

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form was submitted successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }



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

    private readonly string example4RazorCode = @"
<BitCalendar @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>";
    private readonly string example4CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example5RazorCode = @"
<BitCalendar GoToToday=""برو به امروز"" Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />
<BitCalendar GoToToday=""Boro be emrouz"" Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example6RazorCode = @"
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

    private readonly string example7RazorCode = @"
<BitCalendar ShowMonthPicker=""@showMonthPicker"" />
<BitToggleButton OnText=""MonthPicker visible"" OffText=""MonthPicker invisible"" @bind-IsChecked=""@showMonthPicker"" />

<BitCalendar ShowMonthPickerAsOverlay=""@showMonthPickerAsOverlay"" />
<BitToggleButton OnText=""Position Overlay"" OffText=""Position Besides"" @bind-IsChecked=""@showMonthPickerAsOverlay"" />";
    private readonly string example7CsharpCode = @"
private bool showMonthPicker = true;
private bool showMonthPickerAsOverlay;";

    private readonly string example8RazorCode = @"
<BitCalendar @bind-Value=""@selectedDateTime"" ShowTimePicker=""true"" />
<div>Selected DateTime: @selectedDateTime.ToString()</div>";
    private readonly string example8CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;";

    private readonly string example9RazorCode = @"
<BitCalendar ShowTimePicker=""true"" HourStep=""2"" />
<BitCalendar ShowTimePicker=""true"" MinuteStep=""15"" />";

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
<BitCalendar Dir=""BitDir.Rtl"" />";
}
