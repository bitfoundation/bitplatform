namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DateRangePicker allows string date inputs.",
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the DateRangePicker closes automatically after selecting the second value.",
        },
        new()
        {
            Name = "CalloutAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label of the DateRangePicker's callout for screen readers."
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional html attributes for the DateRangePicker's callout."
        },
        new()
        {
            Name = "Classes",
            Type = "BitDateRangePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDateRangePicker.",
            Href = "#daterangepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close date range picker",
            Description = "The title of the close button (tooltip)."
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the DateRangePicker."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the DateRangePicker.",
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the day cells of the DateRangePicker."
        },
        new()
        {
            Name = "GoToNextMonthTitle",
            Type = "string",
            DefaultValue = "Go to next month",
            Description = "The title of the Go to next month button (tooltip).",
        },
        new()
        {
            Name = "GoToNextYearRangeTitle",
            Type = "string",
            DefaultValue = "Next year range {0} - {1}",
            Description = "The title of the Go to next year range button (tooltip).",
        },
        new()
        {
            Name = "GoToNextYearTitle",
            Type = "string",
            DefaultValue = "Go to next year {0}",
            Description = "The title of the Go to next year button (tooltip).",
        },
        new()
        {
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button (tooltip).",
        },
        new()
        {
            Name = "GoToPrevYearRangeTitle",
            Type = "string",
            DefaultValue = "Previous year range {0} - {1}",
            Description = "The title of the Go to previous year range button (tooltip).",
        },
        new()
        {
            Name = "GoToPrevYearTitle",
            Type = "string",
            DefaultValue = "Go to previous year {0}",
            Description = "The title of the Go to previous year button (tooltip).",
        },
        new()
        {
            Name = "GoToTodayTitle",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "The title of the GoToToday button (tooltip).",
        },
        new()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines if the DateRangePicker has a border.",
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
            Description = "The title of the HideTimePicker button (tooltip).",
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DateRangePicker's icon."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            DefaultValue = "BitIconLocation.Right",
            Description = "Determines the location of the DateRangePicker's icon.",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the DateRangePicker's icon."
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
            Description = "Whether the month picker is shown or hidden.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DateRangePicker's callout is open.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the DateRangePicker's label.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DateRangePicker's label."
        },
        new()
        {
            Name = "MaxRange",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The maximum range of day and times allowed for selection in DateRangePicker.",
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DateRangePicker.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DateRangePicker.",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the month cells of the DateRangePicker."
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
            Description = "The callback for clicking on the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "The callback for focusing the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "The callback for when the focus moves into the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "The callback for when the focus moves out of the DateRangePicker's input.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The placeholder text of the DateRangePicker's input.",
        },
        new()
        {
            Name = "Responsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode in small screens.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the clear button should be shown or not when the DateRangePicker has a value."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker's close button should be shown or not."
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
            Description = "Show month picker on top of date range picker when visible.",
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not render the time-picker.",
        },
        new()
        {
            Name = "ShowTimePickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date range picker when visible.",
        },
        new()
        {
            Name = "ShowTimePickerTitle",
            Type = "string",
            DefaultValue = "Show time picker",
            Description = "The title of the ShowTimePicker button (tooltip).",
        },
        new()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the week number (weeks 1 to 53) should be shown before each week row.",
        },
        new()
        {
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker is rendered standalone or with the input component and callout.",
        },
        new()
        {
            Name = "StartingValue",
            Type = "BitDateRangePickerValue?",
            DefaultValue = "null",
            Description = "Specifies the date and time of the date and time picker when it is opened without any selected value.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitDateRangePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitDateRangePicker.",
            Href = "#daterangepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "TabIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The tabIndex of the DateRangePicker's input.",
        },
        new()
        {
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "Time format of the time-pickers, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Text field of the DateRangePicker is underlined.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string",
            DefaultValue = "Start: {0} - End: {1}",
            Description = "The string format used to show the DateRangePicker's value in its input.",
        },
        new()
        {
            Name = "WeekNumberTitle",
            Type = "string",
            DefaultValue = "Week number {0}",
            Description = "The title of the week number (tooltip).",
        },
        new()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template to render the year cells of the DateRangePicker."
        },
        new()
        {
            Name = "YearPickerToggleTitle",
            Type = "string",
            DefaultValue = "{0}, change year",
            Description = "The title of the year picker's toggle (tooltip).",
        },
        new()
        {
            Name = "YearRangePickerToggleTitle",
            Type = "string",
            DefaultValue = "{0} - {1}, change month",
            Description = "The title of the year range picker's toggle (tooltip).",
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for DateRangePicker's hour.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for DateRangePicker's minute.",
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "date-range-picker-value",
            Title = "BitDateRangePickerValue",
            Parameters = new()
            {
               new()
               {
                   Name = "StartDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "null",
                   Description = "Indicates the beginning of the date range.",
               },
               new()
               {
                   Name = "EndDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "null",
                   Description = "Indicates the end of the date range.",
               }
            }
        },
        new()
        {
            Id = "daterangepicker-class-styles",
            Title = "BitDateRangePickerClassStyles",
            Parameters = new()
            {
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused state of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Label of the BitDateRangePicker."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout of the BitDateRangePicker."
                },
                new()
                {
                    Name = "CalloutContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Group",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the group of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DayPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DayPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's header of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DayPickerMonth",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day-picker's month of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DayPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the day-picker's nav buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous month icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "GoToTodayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "GoToTodayIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to today icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "CloseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextMonthNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextMonthNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next month icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DaysHeaderRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header row of the days of the BitDateRangePicker."
                },
                new()
                {
                    Name = "WeekNumbersHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the week numbers of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DaysRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the days of the BitDateRangePicker."
                },
                new()
                {
                    Name = "WeekNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the week number of the BitDateRangePicker."
                },
                new()
                {
                    Name = "DayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each day button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TodayDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for today day button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected start day button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "SelectedDayButtons",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected day buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected end day button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartAndEndSelectionDays",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for selected start and end day buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's main container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerHourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerDivider",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's divider of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimePickerAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time-picker's minute input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimePickerAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time-picker's Am Pm container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Am Pm container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Am Pm container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerAmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Am button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerPmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's Pm button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "Divider",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main divider of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearMonthPickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-month-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's header of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's toggle button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the month-picker's nav buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextYearNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextYearNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the months container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the months of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each month button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearPickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the year-picker's header of the BitDateRangePicker."
                },
                new()
                {
                    Name = "MonthPickerToggleButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the month-picker's toggle button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearPickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the year-picker nav buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PrevYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to previous year-range icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextYearRangeNavButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "NextYearRangeNavIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to next year-range icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the years container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearsRow",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each row of the years of the BitDateRangePicker."
                },
                new()
                {
                    Name = "YearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each year button of the BitDateRangePicker."
                }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "icon-location-enum",
            Name = "BitIconLocation",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Left",
                    Description="Show the icon at the left side.",
                    Value="0",
                },
                new()
                {
                    Name= "Right",
                    Description="Show the icon at the right side.",
                    Value="1",
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
                    Value="0",
                },
                new()
                {
                    Name= "TwelveHours",
                    Description="Show time pickers in 12 hours format.",
                    Value="1",
                }
            }
        }
    };



    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private BitDateRangePickerValue? classesValue;

    private BitDateRangePickerValue? selectedDateRange = new()
    {
        StartDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset),
        EndDate = new DateTimeOffset(2020, 1, 25, 0, 0, 0, DateTimeOffset.Now.Offset),
    };

    private BitDateRangePickerValue? startingValue = new()
    {
        StartDate = new DateTimeOffset(2020, 12, 4, 10, 12, 0, DateTimeOffset.Now.Offset),
        EndDate = new DateTimeOffset(2020, 12, 4, 16, 59, 0, DateTimeOffset.Now.Offset),
    };



    private readonly string example1RazorCode = @"
<BitDateRangePicker Label=""Basic DateRangePicker"" />
<BitDateRangePicker Label=""Disabled"" IsEnabled=""false"" />
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
<BitDateRangePicker Label=""DateFormat: 'dd=MM(yy)'"" DateFormat=""dd=MM(yy)"" />
<BitDateRangePicker Label=""ValueFormat: 'Dep: {0}, Arr: {1}'"" ValueFormat=""Dep: {0}, Arr: {1}"" />";

    private readonly string example4RazorCode = @"
<BitDateRangePicker @bind-Value=""@selectedDateRange"" />
<div>From: <b>@selectedDateRange?.StartDate.ToString()</b></div>
<div>To: <b>@selectedDateRange?.EndDate.ToString()</b></div>";
    private readonly string example4CsharpCode = @"
private BitDateRangePickerValue? selectedDateRange = new()
{
    StartDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(2020, 1, 25, 0, 0, 0, DateTimeOffset.Now.Offset)
};";

    private readonly string example5RazorCode = @"
<BitDateRangePicker Label=""fa-IR culture with Farsi names""
                    GoToTodayTitle=""برو به امروز""
                    ValueFormat=""شروع: {0}, پایان: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDateRangePicker Label=""fa-IR culture with Fingilish names""
                    GoToTodayTitle=""Boro be emrouz""
                    ValueFormat=""Shoro: {0}, Payan: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example6RazorCode = @"
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
    private readonly string example6CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example7RazorCode = @"
<BitDateRangePicker Label=""Responsive DateRangePicker""
                    Responsive
                    ShowWeekNumbers
                    Placeholder=""Select a date range"" />";

    private readonly string example8RazorCode = @"
<BitDateRangePicker ShowTimePicker
                    Label=""HourStep = 2""
                    HourStep=""2"" />

<BitDateRangePicker ShowTimePicker
                    Label=""MinuteStep = 15""
                    MinuteStep=""15"" />";

    private readonly string example9CsharpCode = @"
<BitDateRangePicker Label=""Basic DatePicker"" Standalone />
<BitDateRangePicker Label=""Disabled"" IsEnabled=""false"" Standalone />
<BitDateRangePicker Label=""Week numbers"" ShowWeekNumbers Standalone />
<BitDateRangePicker Label=""Highlight months"" HighlightCurrentMonth HighlightSelectedMonth Standalone />
<BitDateRangePicker Label=""TimePicker"" ShowTimePicker Standalone />";

    private readonly string example10RazorCode = @"
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
    private readonly string example10CsharpCode = @"
private BitDateRangePickerValue? classesValue;";

    private readonly string example11RazorCode = @"
<BitDateRangePicker Dir=""BitDir.Rtl"" />";
}
