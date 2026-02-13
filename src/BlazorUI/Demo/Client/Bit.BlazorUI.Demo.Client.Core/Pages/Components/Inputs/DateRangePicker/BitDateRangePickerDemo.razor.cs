namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display. Takes precedence over IconName when both are set. Use for external libraries (e.g. BitIconInfo.Fa(\"solid calendar\"), BitIconInfo.Bi(\"calendar3\"), BitIconInfo.Css(\"my-class\")).",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the icon from the built-in Fluent UI icon set. For external icon libraries, use Icon instead."
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
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "Time format of the time-pickers, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
        },
        new()
        {
            Name = "TimeZone",
            Type = "TimeZoneInfo?",
            DefaultValue = "null",
            Description = "TimeZone for the DateRangePicker."
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
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "date-range-picker-value",
            Title = "BitDateRangePickerValue",
            Parameters =
            [
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
            ]
        },
        new()
        {
            Id = "daterangepicker-class-styles",
            Title = "BitDateRangePickerClassStyles",
            Parameters =
            [
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
            ]
        },
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
                new()
                {
                    Name = "Name",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the name of the icon."
                },
                new()
                {
                    Name = "BaseClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
                },
                new()
                {
                    Name = "Prefix",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
                },
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "icon-location-enum",
            Name = "BitIconLocation",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "time-format-enum",
            Name = "BitTimeFormat",
            Description = "",
            Items =
            [
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
            ]
        }
    ];



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

    private BitDateRangePickerValue? readOnlyDateRange = new()
    {
        StartDate = new DateTimeOffset(2024, 12, 8, 12, 15, 0, DateTimeOffset.Now.Offset),
        EndDate = new DateTimeOffset(2024, 12, 12, 16, 45, 0, DateTimeOffset.Now.Offset),
    };

    private BitDateRangePickerValue? timeZoneDateRange1 = new();
    private BitDateRangePickerValue? timeZoneDateRange2 = new();
}
