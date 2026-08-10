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
            Type = "BitDateRangePickerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitDateRangePicker.",
            Href = "#daterangepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the clear button. Takes precedence over ClearButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the clear button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "ClearButtonTitle",
            Type = "string",
            DefaultValue = "Clear the selected date range",
            Description = "The title and the aria-label of the clear button.",
        },
        new()
        {
            Name = "CloseButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the close button. Takes precedence over CloseButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CloseButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the close button's icon from the built-in Fluent UI icon set.",
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
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the DateRangePicker that applies to the today day button, the selected range, the highlighted current month and the selected AM/PM buttons.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
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
            Description = "The format of each of the two dates in the DateRangePicker. Defaults to the short date pattern of the Culture, extended with the time pattern when ShowTimePicker is enabled.",
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
            Name = "DisabledDates",
            Type = "IEnumerable<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "The list of dates that are disabled (not selectable) in the DateRangePicker, in addition to MinDate and MaxDate."
        },
        new()
        {
            Name = "DisabledDaysOfWeek",
            Type = "IEnumerable<DayOfWeek>?",
            DefaultValue = "null",
            Description = "The days of the week that are disabled (not selectable) in the DateRangePicker (e.g. weekends)."
        },
        new()
        {
            Name = "EndTimeDecreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the end time-picker's decrease-hour button. Takes precedence over EndTimeDecreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "EndTimeDecreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the end time-picker's decrease-hour button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "EndTimeDecreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the end time-picker's decrease-minute button. Takes precedence over EndTimeDecreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "EndTimeDecreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the end time-picker's decrease-minute button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "EndTimeIncreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the end time-picker's increase-hour button. Takes precedence over EndTimeIncreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "EndTimeIncreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the end time-picker's increase-hour button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "EndTimeIncreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the end time-picker's increase-minute button. Takes precedence over EndTimeIncreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "EndTimeIncreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the end time-picker's increase-minute button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "ExcludeDisabledDates",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the disabled days are excluded from the selected range. By default a range simply spans over the disabled days between its two ends. When enabled, once the start date is picked every day whose range would contain a disabled day becomes unselectable."
        },
        new()
        {
            Name = "FirstDayOfWeek",
            Type = "DayOfWeek?",
            DefaultValue = "null",
            Description = "Overrides the first day of the week in the day picker. If not set, the first day of the week of the Culture is used."
        },
        new()
        {
            Name = "FixedWeeks",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months, to keep the calendar height fixed while navigating between months."
        },
        new()
        {
            Name = "GetDayClass",
            Type = "Func<DateTimeOffset, string?>?",
            DefaultValue = "null",
            Description = "Custom function to provide additional CSS classes for each day button of the DateRangePicker."
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
            Name = "GoToTodayIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the GoToToday button. Takes precedence over GoToTodayIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "GoToTodayIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the GoToToday button's icon from the built-in Fluent UI icon set.",
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
            Name = "HighlightedDates",
            Type = "IEnumerable<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "The list of dates that are highlighted (marked) in the day picker of the DateRangePicker."
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
            Name = "HideTimePickerIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the HideTimePicker button. Takes precedence over HideTimePickerIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "HideTimePickerIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the HideTimePicker button's icon from the built-in Fluent UI icon set.",
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
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for DateRangePicker's hour.",
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
            Type = "string?",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the icon from the built-in Fluent UI icon set. For external icon libraries, use Icon instead."
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
            Name = "InvalidErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for the invalid value."
        },
        new()
        {
            Name = "IsDateDisabled",
            Type = "Func<DateTimeOffset, bool>?",
            DefaultValue = "null",
            Description = "Custom function to determine if a specific date is disabled (not selectable) in the DateRangePicker."
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
            Name = "MaxDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DateRangePicker.",
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
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DateRangePicker.",
        },
        new()
        {
            Name = "MinRange",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The minimum number of days that the selected range must span in the DateRangePicker. Only the days part of the provided TimeSpan is considered.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for DateRangePicker's minute.",
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
            Name = "MonthCount",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of consecutive months rendered side by side in the day picker (1 to 3), which makes picking a range that spans two months a single move. It falls back to a single month whenever the viewport is not wide enough to fit them all."
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
            Name = "NextMonthNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the next-month navigation button. Takes precedence over NextMonthNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextMonthNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the next-month navigation button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "NextYearNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the next-year navigation button. Takes precedence over NextYearNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextYearNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the next-year navigation button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "NextYearRangeNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the next-year-range navigation button. Takes precedence over NextYearRangeNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextYearRangeNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the next-year-range navigation button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "NoDateText",
            Type = "string",
            DefaultValue = "---",
            Description = "The text rendered in place of a date that has not been picked yet, which is also the token accepted back for an open-ended range when AllowTextInput is enabled."
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
            Name = "OnMonthChange",
            Type = "EventCallback<DateTimeOffset>",
            Description = "Callback for when the displayed month of the day picker changes. The argument is the first day of the newly displayed month.",
        },
        new()
        {
            Name = "OnPresetSelect",
            Type = "EventCallback<BitDateRangePickerPreset>",
            Description = "The callback for when a preset is selected. The argument is the selected preset.",
            Href = "#date-range-picker-preset",
            LinkType = LinkType.Link
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
            Name = "Presets",
            Type = "IEnumerable<BitDateRangePickerPreset>?",
            DefaultValue = "null",
            Description = "The list of shortcuts, rendered next to the calendar, that fill the DateRangePicker with a predefined range (e.g. \"Last 7 days\").",
            LinkType = LinkType.Link,
            Href = "#date-range-picker-preset",
        },
        new()
        {
            Name = "PresetsAriaLabel",
            Type = "string",
            DefaultValue = "Predefined date ranges",
            Description = "The aria label of the presets' container for screen readers.",
        },
        new()
        {
            Name = "PrevMonthNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the previous-month navigation button. Takes precedence over PrevMonthNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PrevMonthNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the previous-month navigation button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "PrevYearNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the previous-year navigation button. Takes precedence over PrevYearNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PrevYearNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the previous-year navigation button's icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "PrevYearRangeNavIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the previous-year-range navigation button. Takes precedence over PrevYearRangeNavIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PrevYearRangeNavIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the previous-year-range navigation button's icon from the built-in Fluent UI icon set.",
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
            Name = "SelectedDateAriaAtomic",
            Type = "string",
            DefaultValue = "Selected date range {0}",
            Description = "The aria-atomic live text announcing the currently selected date range, formatted with the value of the input."
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
            Name = "ShowOutsideDays",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the days of the previous and next months, filling the first and last week rows, should be rendered.",
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
            Description = "Show the time picker on top of the date range picker when visible.",
        },
        new()
        {
            Name = "ShowTimePickerIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the ShowTimePicker button. Takes precedence over ShowTimePickerIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "ShowTimePickerIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the ShowTimePicker button's icon from the built-in Fluent UI icon set.",
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
            Name = "StartTimeDecreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the start time-picker's decrease-hour button. Takes precedence over StartTimeDecreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "StartTimeDecreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the start time-picker's decrease-hour button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "StartTimeDecreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the start time-picker's decrease-minute button. Takes precedence over StartTimeDecreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "StartTimeDecreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the start time-picker's decrease-minute button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "StartTimeIncreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the start time-picker's increase-hour button. Takes precedence over StartTimeIncreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "StartTimeIncreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the start time-picker's increase-hour button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "StartTimeIncreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the start time-picker's increase-minute button. Takes precedence over StartTimeIncreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "StartTimeIncreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the start time-picker's increase-minute button icon from the built-in Fluent UI icon set.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitDateRangePickerClassStyles?",
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
            Name = "Today",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Overrides the date considered as today by the DateRangePicker, which is DateTimeOffset.Now by default."
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
            Description = "The string format used to show the DateRangePicker's value in its input, where {0} is the start date and {1} is the end date. It is also the template used to parse the typed text back when AllowTextInput is enabled.",
        },
        new()
        {
            Name = "WeekNumberRule",
            Type = "CalendarWeekRule?",
            DefaultValue = "null",
            Description = "The rule used to calculate the week numbers. If not set, CalendarWeekRule.FirstFullWeek is used.",
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
            Id = "date-range-picker-preset",
            Title = "BitDateRangePickerPreset",
            Parameters =
            [
                new()
                {
                    Name = "Text",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The text of the preset's button.",
                },
                new()
                {
                    Name = "Value",
                    Type = "BitDateRangePickerValue?",
                    DefaultValue = "null",
                    Description = "The range applied when the preset is selected. ValueProvider takes precedence over this when both are set.",
                    Href = "#date-range-picker-value",
                    LinkType = LinkType.Link
                },
                new()
                {
                    Name = "ValueProvider",
                    Type = "Func<BitDateRangePickerValue?>?",
                    DefaultValue = "null",
                    Description = "Custom function providing the range applied when the preset is selected. Unlike Value it is evaluated on each selection, which keeps relative ranges (e.g. \"Last 7 days\") correct no matter how long the page has been open.",
                },
                new()
                {
                    Name = "IsEnabled",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether the preset's button is enabled.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The title of the preset's button (tooltip).",
                },
                new()
                {
                    Name = "Class",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS class for the preset's button.",
                },
                new()
                {
                    Name = "Style",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS style for the preset's button.",
                },
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
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear button icon of the BitDateRangePicker."
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
                    Name = "PresetsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the presets' container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "PresetButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each preset button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "SelectedPresetButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the currently selected preset button of the BitDateRangePicker."
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
                    Name = "DaysGrid",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the days grid of the BitDateRangePicker."
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
                    Description = "Custom CSS classes/styles for the header cell of the week numbers column of the BitDateRangePicker."
                },
                new()
                {
                    Name = "WeekDayHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each day of the week header cell of the BitDateRangePicker."
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
                    Name = "HighlightedDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the highlighted day buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "HoveredDayButtons",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the day buttons inside the prospective range being hovered in the BitDateRangePicker."
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
                    Name = "TimePickerWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's wrapper of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's header of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimePickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the time-picker's nav buttons of the BitDateRangePicker."
                },
                new()
                {
                    Name = "ShowTimePickerButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the show time-picker button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "ShowTimePickerIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the show time-picker icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "HideTimePickerButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hide time-picker button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "HideTimePickerIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hide time-picker icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "TimeInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time's input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeHourInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's hour input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeHourInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's hour input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeMinuteInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's minute input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeMinuteInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's minute input container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeHourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's hour input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeHourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's hour input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeMinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's minute input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeMinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's minute input of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeHourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's hour/minute separator of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeHourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's hour/minute separator of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeIncreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's increase hour button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeIncreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's increase hour icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeDecreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's decrease hour button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeDecreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's decrease hour icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeIncreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's increase minute button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeIncreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's increase minute icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeDecreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's decrease minute button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeDecreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's decrease minute icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeIncreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's increase hour button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeIncreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's increase hour icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeDecreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's decrease hour button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeDecreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's decrease hour icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeIncreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's increase minute button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeIncreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's increase minute icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeDecreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's decrease minute button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeDecreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's decrease minute icon of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's Am Pm container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeAmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's Am Pm container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimeAmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's Am button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "StartTimePmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the start time's Pm button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimeAmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's Am button of the BitDateRangePicker."
                },
                new()
                {
                    Name = "EndTimePmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the end time's Pm button of the BitDateRangePicker."
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
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Primary", Description = "Primary general color.", Value = "0" },
                new() { Name = "Secondary", Description = "Secondary general color.", Value = "1" },
                new() { Name = "Tertiary", Description = "Tertiary general color.", Value = "2" },
                new() { Name = "Info", Description = "Info general color.", Value = "3" },
                new() { Name = "Success", Description = "Success general color.", Value = "4" },
                new() { Name = "Warning", Description = "Warning general color.", Value = "5" },
                new() { Name = "SevereWarning", Description = "SevereWarning general color.", Value = "6" },
                new() { Name = "Error", Description = "Error general color.", Value = "7" },
                new() { Name = "PrimaryBackground", Description = "Primary background color.", Value = "8" },
                new() { Name = "SecondaryBackground", Description = "Secondary background color.", Value = "9" },
                new() { Name = "TertiaryBackground", Description = "Tertiary background color.", Value = "10" },
                new() { Name = "PrimaryForeground", Description = "Primary foreground color.", Value = "11" },
                new() { Name = "SecondaryForeground", Description = "Secondary foreground color.", Value = "12" },
                new() { Name = "TertiaryForeground", Description = "Tertiary foreground color.", Value = "13" },
                new() { Name = "PrimaryBorder", Description = "Primary border color.", Value = "14" },
                new() { Name = "SecondaryBorder", Description = "Secondary border color.", Value = "15" },
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" },
            ]
        },
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

    private BitDateRangePickerValue? presetsValue;

    private string? lastSelectedPreset;

    private BitDateRangePickerValue? monthCountValue;

    private readonly BitDateRangePickerPreset[] presets =
    [
        new()
        {
            Text = "Today",
            ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date, EndDate = DateTimeOffset.Now.Date }
        },
        new()
        {
            Text = "Yesterday",
            ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date.AddDays(-1), EndDate = DateTimeOffset.Now.Date.AddDays(-1) }
        },
        new()
        {
            Text = "Last 7 days",
            ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date.AddDays(-6), EndDate = DateTimeOffset.Now.Date }
        },
        new()
        {
            Text = "Last 30 days",
            ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date.AddDays(-29), EndDate = DateTimeOffset.Now.Date }
        },
        new()
        {
            Text = "This month",
            ValueProvider = () =>
            {
                var now = DateTimeOffset.Now.Date;
                var firstDay = new DateTime(now.Year, now.Month, 1);
                return new() { StartDate = firstDay, EndDate = firstDay.AddMonths(1).AddDays(-1) };
            }
        },
        new()
        {
            Text = "Coming soon",
            IsEnabled = false,
            Title = "This preset is not available yet"
        },
    ];

    private readonly DateTimeOffset[] disabledDates =
    [
        DateTimeOffset.Now.AddDays(2),
        DateTimeOffset.Now.AddDays(3),
        DateTimeOffset.Now.AddDays(8),
    ];

    private readonly DateTimeOffset[] highlightedDates =
    [
        DateTimeOffset.Now.AddDays(1),
        DateTimeOffset.Now.AddDays(2),
        DateTimeOffset.Now.AddDays(3),
    ];

    private string successMessage = string.Empty;
    private FormValidationDateRangePickerModel validationModel = new();

    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }

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
