namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public partial class BitDatePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DatePicker allows a string date input."
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the DatePicker closes automatically after selecting the date."
        },
        new()
        {
            Name = "CalloutAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label of the DatePicker's callout for screen readers."
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional html attributes for the DatePicker's callout."
        },
        new()
        {
            Name = "Classes",
            Type = "BitDatePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDatePicker.",
            Href = "#datepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the DatePicker."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the DatePicker."
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the day cells of the DatePicker."
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
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the DatePicker has a border."
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
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DatePicker's icon."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            DefaultValue = "BitIconLocation.Right",
            Description = "Determines the location of the DatePicker's icon.",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum"
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the DatePicker's icon."
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
            Name = "IsMonthPickerVisible",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the month picker is shown or hidden."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this DatePicker is open."
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the DatePicker's label."
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DatePicker's label."
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DatePicker."
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DatePicker."
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the month cells of the DatePicker."
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
            Name = "OnClick",
            Type = "EventCallback",
            Description = "The callback for clicking on the DatePicker's input."
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "The callback for focusing the DatePicker's input."
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "The callback for when the focus moves into the DatePicker's input."
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "The callback for when the focus moves out of the DatePicker's input."
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The placeholder text of the DatePicker's input."
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode in small screens."
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
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the clear button should be shown or not when the BitDatePicker has a value."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DatePicker's close button should be shown or not."
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
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date picker when visible."
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not render the time-picker."
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
            Name = "StartingValue",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Specifies the date and time of the date-picker when it is opened without any selected value.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitDatePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDatePicker.",
            Href = "#datepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the date-picker is rendered standalone or with the input component and callout.",
        },
        new()
        {
            Name = "TabIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The tabIndex of the DatePicker's input."
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
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Text field of the DatePicker is underlined."
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
            Description = "Custom template to render the year cells of the DatePicker."
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
            Description = "Determines increment/decrement steps for date-picker's hour.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for date-picker's minute.",
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
            Id = "icon-location-enum",
            Name = "BitIconLocation",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name = "Left",
                    Description = "Show the icon at the left side.",
                    Value = "0"
                },
                new()
                {
                    Name = "Right",
                    Description = "Show the icon at the right side.",
                    Value = "1"
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
            Id = "datepicker-class-styles",
            Title = "BitDatePickerClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDatePicker."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused state of the BitDatePicker."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Label of the BitDatePicker."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input wrapper of the BitDatePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitDatePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitDatePicker."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input of the BitDatePicker."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitDatePicker."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitDatePicker."
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout of the BitDatePicker."
                },
                new()
                {
                    Name = "CalloutContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout container of the BitDatePicker."
                },
                new()
                {
                    Name = "Group",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the group of the BitDatePicker."
                },
                new()
                {
                    Name = "DayPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's wrapper of the BitDatePicker."
                },
                new()
                {
                    Name = "DayPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's header of the BitDatePicker."
                },
                new()
                {
                    Name = "DayPickerMonth",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's month of the BitDatePicker."
                },
                new()
                {
                    Name = "DayPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the day-picker's nav buttons of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month button of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month icon of the BitDatePicker."
                },
                new()
                {
                    Name = "GoToTodayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today button of the BitDatePicker."
                },
                new()
                {
                    Name = "GoToTodayIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today icon of the BitDatePicker."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the BitDatePicker."
                },
                new()
                {
                    Name = "CloseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button icon of the BitDatePicker."
                },
                new()
                {
                    Name = "NextMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month button of the BitDatePicker."
                },
                new()
                {
                    Name = "NextMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month icon of the BitDatePicker."
                },
                new()
                {
                    Name = "DaysHeaderRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header row of the days of the BitDatePicker."
                },
                new()
                {
                    Name = "WeekNumbersHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the week numbers of the BitDatePicker."
                },
                new()
                {
                    Name = "DaysRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the days of the BitDatePicker."
                },
                new()
                {
                    Name = "WeekNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the week number of the BitDatePicker."
                },
                new()
                {
                    Name = "DayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each day button of the BitDatePicker."
                },
                new()
                {
                    Name = "TodayDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for today day button of the BitDatePicker."
                },
                new()
                {
                    Name = "SelectedDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected day button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's main container of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's wrapper of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerHourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour/minute separator of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDivider",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's divider of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerMinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's minute input of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Am Pm container of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerAmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Am button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerPmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Pm button of the BitDatePicker."
                },
                new()
                {
                    Name = "Divider",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main divider of the BitDatePicker."
                },
                new()
                {
                    Name = "YearMonthPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-month-picker's wrapper of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's header of the BitDatePicker."
                },
                new()
                {
                    Name = "YearPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's toggle button of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the month-picker's nav buttons of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year button of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year icon of the BitDatePicker."
                },
                new()
                {
                    Name = "NextYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year button of the BitDatePicker."
                },
                new()
                {
                    Name = "NextYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year icon of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the months container of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the months of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each month button of the BitDatePicker."
                },
                new()
                {
                    Name = "YearPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's header of the BitDatePicker."
                },
                new()
                {
                    Name = "MonthPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's toggle button of the BitDatePicker."
                },
                new()
                {
                    Name = "YearPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the year-picker nav buttons of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range button of the BitDatePicker."
                },
                new()
                {
                    Name = "PrevYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range icon of the BitDatePicker."
                },
                new()
                {
                    Name = "NextYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range button of the BitDatePicker."
                },
                new()
                {
                    Name = "NextYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range icon of the BitDatePicker."
                },
                new()
                {
                    Name = "YearsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the years container of the BitDatePicker."
                },
                new()
                {
                    Name = "YearsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the years of the BitDatePicker."
                },
                new()
                {
                    Name = "YearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each year button of the BitDatePicker."
                }
            }
        }
    };



    private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);
    private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);

    private DateTimeOffset? classesValue;

    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private BitDatePickerValidationModel validationModel = new();
    private string SuccessMessage = string.Empty;


    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }



    private readonly string example1RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" />
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
<BitDatePicker Label=""Text input allowed""
               AllowTextInput=true
               DateFormat=""dd/MM/yyyy""
               Placeholder=""Enter a date (dd/MM/yyyy)"" />";

    private readonly string example4RazorCode = @"
<BitDatePicker Label=""Formatted Date""
               DateFormat=""dd=MM(yy)""
               Placeholder=""Select a date"" />";

    private readonly string example5RazorCode = @"
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
    private readonly string example5CsharpCode = @"
private DateTimeOffset? classesValue;";

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
    private readonly string example8CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example9RazorCode = @"
<BitDatePicker Label=""Response DatePicker""
               Responsive
               ShowWeekNumbers
               Placeholder=""Select a date"" />";

    private readonly string example10RazorCode = @"
<BitDatePicker ShowTimePicker
               Label=""HourStep = 2""
               HourStep=""2"" />

<BitDatePicker ShowTimePicker
               Label=""MinuteStep = 15""
               MinuteStep=""15"" />";

    private readonly string example11RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" Standalone />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitDatePicker Label=""Week numbers"" ShowWeekNumbers Standalone />
<BitDatePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth Standalone />
<BitDatePicker Label=""TimePicker"" ShowTimePicker Standalone />";

    private readonly string example12RazorCode = @"
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
    private readonly string example12CsharpCode = @"
public class BitDatePickerValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private BitDatePickerValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";

    private readonly string example13RazorCode = @"
<BitDatePicker Dir=""BitDir.Rtl"" />";
}
