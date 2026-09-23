namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public partial class BitDatePickerDemo
{
    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "OpenCallout",
            Type = "Task",
            Description = "Opens the callout of the DatePicker exactly as clicking its input would."
        },
        new()
        {
            Name = "CloseCalloutAndFocus",
            Type = "Task",
            Description = "Closes the callout of the DatePicker and moves the focus back to its input."
        },
    ];

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowDeselect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether selecting the already selected date deselects it, clearing the value. The callout stays open after a deselection, so another date can be picked right away."
        },
        new()
        {
            Name = "AllowedHours",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The hours the time picker can be set to, on top of what MinTime, MaxTime and the bounds of the day already allow. The spin buttons skip over the hours it rejects, a typed one snaps to the nearest it accepts, and a date entered as text whose time lands on one fails validation."
        },
        new()
        {
            Name = "AllowedMinutes",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The minutes the time picker can be set to, on top of what MinTime, MaxTime and the bounds of the day already allow."
        },
        new()
        {
            Name = "AllowedSeconds",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The seconds the time picker can be set to, on top of what MinTime, MaxTime and the bounds of the day already allow. It only has an effect while ShowSeconds is set."
        },
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
            Description = "Whether the DatePicker closes automatically after selecting the date. It has no effect while the time picker is shown, where the callout stays open so the time of the selected day can be set as well."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the input of the picker gets the focus as soon as it renders for the first time. A standalone picker carries its value in a hidden input nobody is meant to land on, so it has nothing to place the focus on.",
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
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the bottom of the DatePicker's callout, below the pickers (e.g. preset buttons that set the value from the code)."
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the top of the DatePicker's callout, above the pickers."
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
            Description = "The name of the clear button's icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "ClearButtonTitle",
            Type = "string",
            DefaultValue = "Clear date",
            Description = "The title (tooltip) and the accessible name of the clear button."
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
            Description = "The name of the close button's icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close date picker",
            Description = "The title of the close button (tooltip).",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the DatePicker that applies to the today day button, the highlighted current month, and the selected AM/PM button.",
            LinkType = LinkType.Link,
            Href = "#color-enum"
        },
        new()
        {
            Name = "ContinuousSpinDelay",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay in milliseconds before the hour/minute starts changing continuously while an increase/decrease button of the time picker is held down.",
        },
        new()
        {
            Name = "ContinuousSpinInterval",
            Type = "int",
            DefaultValue = "75",
            Description = "The interval in milliseconds between two consecutive changes while an increase/decrease button of the time picker is held down.",
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
            Description = "The format the date is written with, and the only one a typed date is then read with. Without it the short date pattern of the culture is used (plus the time pattern where the time picker is on), and a typed date is read more freely: the day with the time left off, with the seconds left off or spelled out, and in either clock format, are all accepted."
        },
        new()
        {
            Name = "DateFormatAriaDescription",
            Type = "string",
            DefaultValue = "Expected format: {0}",
            Description = "The accessible description of the input of a picker that accepts a typed date, which the pattern the date is read with is formatted into. A placeholder is gone as soon as the first character is typed, so the pattern is carried by a description of the input instead. Set it to an empty string to leave the input without one."
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
            Name = "DisabledDateErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a typed value that the DatePicker does not allow to be selected, through DisabledDates, DisabledDaysOfWeek or IsDateDisabled."
        },
        new()
        {
            Name = "DisabledDates",
            Type = "IEnumerable<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "The list of dates that are disabled (not selectable) in the DatePicker, in addition to MinDate and MaxDate."
        },
        new()
        {
            Name = "DisabledDaysOfWeek",
            Type = "IEnumerable<DayOfWeek>?",
            DefaultValue = "null",
            Description = "The days of the week that are disabled (not selectable) in the DatePicker (e.g. weekends)."
        },
        new()
        {
            Name = "DisableFuture",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables all days after today, exactly as a MaxDate of today would. When both are set, the earlier of the two bounds wins."
        },
        new()
        {
            Name = "DisablePast",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables all days before today, exactly as a MinDate of today would. When both are set, the later of the two bounds wins."
        },
        new()
        {
            Name = "DisallowedTimeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a date entered as text whose time of day the DatePicker does not allow, through MinTime, MaxTime, AllowedHours, AllowedMinutes or AllowedSeconds."
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum"
        },
        new()
        {
            Name = "FirstDayOfWeek",
            Type = "DayOfWeek?",
            DefaultValue = "null",
            Description = "Overrides the first day of the week of the day picker. If not set, the first day of the week of the Culture is used."
        },
        new()
        {
            Name = "FixedWeeks",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months, to keep the height of the calendar fixed while navigating between the months."
        },
        new()
        {
            Name = "GetDayClass",
            Type = "Func<DateTimeOffset, string?>?",
            DefaultValue = "null",
            Description = "Custom function to provide additional CSS classes for each day button of the DatePicker."
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
            Description = "The name of the GoToToday button's icon from the built-in Fluent UI icon set."
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
            DefaultValue = "true",
            Description = "Determines if the DatePicker has a border."
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
            Description = "The name of the HideTimePicker button's icon from the built-in Fluent UI icon set."
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
            Description = "The list of dates that are highlighted (marked) in the day picker."
        },
        new()
        {
            Name = "HighlightedDateAriaLabel",
            Type = "string",
            DefaultValue = "{0}, highlighted",
            Description = "The accessible name of a day of HighlightedDates, which its full date is formatted into. A highlighted day is marked by a background alone, so the mark is said in the name of the day as well. Set it to an empty string to name a highlighted day like any other."
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
            Name = "HighlightToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the day picker should highlight today's day. It only affects the visual style of the day cell; the accessibility attributes still report the day as the current date."
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in hours, the spin buttons move the hour by. A step greater than 1 lays a grid over the day that every hour the picker produces sits on, starting at the hour of MinTime and at midnight where there is none - the buttons, the PageUp/PageDown keys and what is typed into the hour alike.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display in the DatePicker input using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
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
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the DatePicker's icon from the built-in Fluent UI icon set. For external icon libraries, use Icon instead."
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
            Description = "Custom function to determine if a specific date is disabled (not selectable) in the DatePicker."
        },
        new()
        {
            Name = "IsMonthPickerVisible",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the month picker is shown next to the day picker or hidden. It has no effect in the MonthPicker mode, where the month picker is the only view."
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
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DatePicker. The days after it are ruled out as a whole and the day it falls on stays selectable; where a time picker is on screen, the time it carries bounds the hours of that day too."
        },
        new()
        {
            Name = "MaxTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The latest time of day the time picker can be set to, on every day the DatePicker offers - the time-of-day bound MaxDate is not. Where both bound the same day, the earlier of the two wins."
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DatePicker. The days before it are ruled out as a whole and the day it falls on stays selectable; where a time picker is on screen, the time it carries bounds the hours of that day too."
        },
        new()
        {
            Name = "MinTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The earliest time of day the time picker can be set to, on every day the DatePicker offers - the time-of-day bound MinDate is not. Where both bound the same day, the later of the two wins."
        },
        new()
        {
            Name = "MonthCount",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of consecutive months rendered side by side in the day picker (1 to 3). Such a strip always draws six week rows, never draws the days of the adjacent months, and falls back to fewer months on a viewport too narrow to hold them."
        },
        new()
        {
            Name = "PagedNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the previous and next navigation buttons move the day picker by all of its rendered months instead of one. It has no effect when MonthCount renders a single month."
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in minutes, the spin buttons move the minute by. A step greater than 1 lays a grid over the hour that every minute the picker produces sits on, starting at the minute of MinTime and at the top of the hour where there is none - which is what turns it into a five-minute or quarter-hour picker. The buttons, the PageUp/PageDown keys and what is typed into the minute are all held to it.",
        },
        new()
        {
            Name = "Mode",
            Type = "BitDatePickerMode",
            DefaultValue = "BitDatePickerMode.DatePicker",
            Description = "The selection mode of the DatePicker (DatePicker or MonthPicker).",
            LinkType = LinkType.Link,
            Href = "#datepicker-mode-enum"
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
            Description = "The name of the next-month navigation button's icon from the built-in Fluent UI icon set."
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
            Description = "The name of the next-year navigation button's icon from the built-in Fluent UI icon set."
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
            Description = "The name of the next-year-range navigation button's icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "NowButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the now button. Takes precedence over NowButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NowButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the now button's icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "NowButtonTitle",
            Type = "string",
            DefaultValue = "Go to now",
            Description = "The title of the now button (tooltip). The button sets the time of the selected day to the current time - and, on a picker with no value yet, picks today along with it."
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "The callback that is called when the value gets cleared by the clear button."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            Description = "The callback for clicking on the DatePicker's input."
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "Callback for when the callout is closed.",
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
            Name = "OnMonthChange",
            Type = "EventCallback<DateTimeOffset>",
            Description = "The callback for when the displayed month of the day picker changes. The argument is the first day of the newly displayed month."
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Callback for when the callout is opened.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "The callback for when the user selects a date."
        },
        new()
        {
            Name = "OutOfRangeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a typed value that falls outside of the MinDate and MaxDate range."
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
            Description = "The name of the previous-month navigation button's icon from the built-in Fluent UI icon set."
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
            Description = "The name of the previous-year navigation button's icon from the built-in Fluent UI icon set."
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
            Description = "The name of the previous-year-range navigation button's icon from the built-in Fluent UI icon set."
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
            Name = "SecondStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in seconds, the spin buttons move the second by. The grid it lays over the minute starts at the second of MinTime, and at the top of the minute where there is none."
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
            Name = "ShowNowButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the now button should be shown or not."
        },
        new()
        {
            Name = "ShowOutsideDays",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the days of the previous and next months should be shown in the day picker. It has no effect when MonthCount renders more than one month, since those days would then appear in two panes at once."
        },
        new()
        {
            Name = "ShowSeconds",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the time picker shows a seconds field beside the hour and the minute, which adds the second to the value and to the default date format."
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
            Name = "ShowTimePickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show the time picker as an overlay on top of the date picker when visible."
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
            Description = "The name of the ShowTimePicker button's icon from the built-in Fluent UI icon set."
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
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the DatePicker.",
            LinkType = LinkType.Link,
            Href = "#size-enum"
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
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "The time format of the time-picker, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum"
        },
        new()
        {
            Name = "TimePickerDecreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's decrease-hour button. Takes precedence over TimePickerDecreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerDecreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's decrease-hour button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerDecreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's decrease-minute button. Takes precedence over TimePickerDecreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerDecreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's decrease-minute button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerDecreaseSecondIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's decrease-second button. Takes precedence over TimePickerDecreaseSecondIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerDecreaseSecondIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's decrease-second button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerDecreaseHourTitle",
            Type = "string",
            DefaultValue = "Decrease hour",
            Description = "The title (tooltip) and the accessible name of the time-picker's decrease-hour button."
        },
        new()
        {
            Name = "TimePickerDecreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Decrease minute",
            Description = "The title (tooltip) and the accessible name of the time-picker's decrease-minute button."
        },
        new()
        {
            Name = "TimePickerHourTitle",
            Type = "string",
            DefaultValue = "Hour",
            Description = "The title (tooltip) and the accessible name of the time-picker's hour input."
        },
        new()
        {
            Name = "TimePickerIncreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's increase-hour button. Takes precedence over TimePickerIncreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerIncreaseHourIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's increase-hour button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerIncreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's increase-minute button. Takes precedence over TimePickerIncreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerIncreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's increase-minute button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerIncreaseHourTitle",
            Type = "string",
            DefaultValue = "Increase hour",
            Description = "The title (tooltip) and the accessible name of the time-picker's increase-hour button."
        },
        new()
        {
            Name = "TimePickerIncreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Increase minute",
            Description = "The title (tooltip) and the accessible name of the time-picker's increase-minute button."
        },
        new()
        {
            Name = "TimePickerDecreaseSecondTitle",
            Type = "string",
            DefaultValue = "Decrease second",
            Description = "The title (tooltip) and the accessible name of the time-picker's decrease-second button."
        },
        new()
        {
            Name = "TimePickerIncreaseSecondIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display inside the time-picker's increase-second button. Takes precedence over TimePickerIncreaseSecondIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "TimePickerIncreaseSecondIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the time-picker's increase-second button icon from the built-in Fluent UI icon set."
        },
        new()
        {
            Name = "TimePickerIncreaseSecondTitle",
            Type = "string",
            DefaultValue = "Increase second",
            Description = "The title (tooltip) and the accessible name of the time-picker's increase-second button."
        },
        new()
        {
            Name = "TimePickerMinuteTitle",
            Type = "string",
            DefaultValue = "Minute",
            Description = "The title (tooltip) and the accessible name of the time-picker's minute input."
        },
        new()
        {
            Name = "TimePickerSecondTitle",
            Type = "string",
            DefaultValue = "Second",
            Description = "The title (tooltip) and the accessible name of the time-picker's second input."
        },
        new()
        {
            Name = "TimeZone",
            Type = "TimeZoneInfo?",
            DefaultValue = "null",
            Description = "TimeZone for the DatePicker."
        },
        new()
        {
            Name = "Today",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Overrides the current date and time considered as \"today\" and \"now\" in the DatePicker (useful for testing or custom time providers)."
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
            Name = "WeekNumberRule",
            Type = "CalendarWeekRule?",
            DefaultValue = "null",
            Description = "The rule used to calculate the week numbers. Defaults to the FirstFullWeek rule."
        },
        new()
        {
            Name = "WeekNumbersHeaderTitle",
            Type = "string",
            DefaultValue = "Week",
            Description = "The accessible name of the empty column header above the week numbers."
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
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "datepicker-class-styles",
            Title = "BitDatePickerClassStyles",
            Parameters =
            [
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
                    Description = "Custom CSS classes/styles for the label of the BitDatePicker."
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
                    Name = "CalloutHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header wrapper of the callout of the BitDatePicker, rendered when a CalloutHeaderTemplate is provided."
                },
                new()
                {
                    Name = "CalloutFooter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the footer wrapper of the callout of the BitDatePicker, rendered when a CalloutFooterTemplate is provided."
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
                    Name = "NowButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to now button of the BitDatePicker."
                },
                new()
                {
                    Name = "HideTimePickerButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hide time-picker button of the BitDatePicker."
                },
                new()
                {
                    Name = "HideTimePickerIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hide time-picker icon of the BitDatePicker."
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
                    Name = "NowButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Go to now icon of the BitDatePicker."
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
                    Name = "DaysGrid",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the grid of the days of the BitDatePicker."
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
                    Name = "DayNameHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header cells of the day names of the BitDatePicker."
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
                    Name = "HighlightedDayButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the highlighted day buttons of the BitDatePicker."
                },
                new()
                {
                    Name = "TimeInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's input container of the BitDatePicker."
                },
                new()
                {
                    Name = "HourInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour input container of the BitDatePicker."
                },
                new()
                {
                    Name = "MinuteInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's minute input container of the BitDatePicker."
                },
                new()
                {
                    Name = "SecondInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's second input container of the BitDatePicker."
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
                    Name = "TimePickerHourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's hour input of the BitDatePicker."
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
                    Name = "TimePickerMinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's minute input of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerMinuteSecondSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's minute/second separator of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerSecondInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's second input of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase hour button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase hour icon of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease hour button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease hour icon of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase minute button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase minute icon of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease minute button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease minute icon of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseSecondButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase second button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerIncreaseSecondIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's increase second icon of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseSecondButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease second button of the BitDatePicker."
                },
                new()
                {
                    Name = "TimePickerDecreaseSecondIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's decrease second icon of the BitDatePicker."
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
                    Name = "TimePickerHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time-picker's header of the BitDatePicker."
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
                    Name = "ShowTimePickerButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the show time-picker button of the BitDatePicker."
                },
                new()
                {
                    Name = "ShowTimePickerIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the show time-picker icon of the BitDatePicker."
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
                    Name = "TimePickerNavWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the wrapper of the time-picker's nav buttons of the BitDatePicker."
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
                    Description = "Custom CSS classes/styles of the years container of the BitDatePicker."
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
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitDatePicker's clear button."
                },
                new()
                {
                    Name = "ClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the BitDatePicker's clear button icon."
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
        },
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
                new() { Name = "TertiaryBorder", Description = "Tertiary border color.", Value = "16" }
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new() { Name = "Small", Description = "The small size DatePicker.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size DatePicker.", Value = "1" },
                new() { Name = "Large", Description = "The large size DatePicker.", Value = "2" }
            ]
        },
        new()
        {
            Id = "component-visibility-enum",
            Name = "BitVisibility",
            Description = "",
            Items =
            [
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
                    Value="0"
                },
                new()
                {
                    Name= "TwelveHours",
                    Description="Show time pickers in 12 hours format.",
                    Value="1"
                }
            ]
        },
        new()
        {
            Id = "drop-direction-enum",
            Name = "BitDropDirection",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "All",
                    Description = "The direction determined automatically based on the available spaces in all directions.",
                    Value = "0"
                },
                new()
                {
                    Name = "TopAndBottom",
                    Description = "Show the callout at the top or bottom side.",
                    Value = "1"
                }
            ]
        },
        new()
        {
            Id = "datepicker-mode-enum",
            Name = "BitDatePickerMode",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "DatePicker",
                    Description = "Standard date picker mode allowing selection of a specific day.",
                    Value = "0"
                },
                new()
                {
                    Name = "MonthPicker",
                    Description = "Month picker mode allowing selection of only month and year. The day is automatically set to the 1st of the selected month.",
                    Value = "1"
                }
            ]
        }
    ];



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-DatePicker-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the label above the field.",
        },
        new()
        {
            Name = "--bit-DatePicker-label-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the label.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-height",
            DefaultValue = "per Size",
            Description = "Height of the field.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the field.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the field.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the field.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Border color of the field at rest, which the open state (the Color role) and the invalid state override.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the field and of its focus ring.",
        },
        new()
        {
            Name = "--bit-DatePicker-input-padding",
            DefaultValue = "0 8px (the spacing unit)",
            Description = "Inline padding of the field, between its border and the text and icons inside it.",
        },
        new()
        {
            Name = "--bit-DatePicker-icon-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the calendar icon and of the clear button's icon.",
        },
        new()
        {
            Name = "--bit-DatePicker-focus-color",
            DefaultValue = "the Color role's focus color",
            Description = "Color of every focus ring the component draws - on the field and on the cells and buttons inside the callout.",
        },
        new()
        {
            Name = "--bit-DatePicker-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text color of anything disabled: the label, the field, a day, a month, a year, a navigation button.",
        },
        new()
        {
            Name = "--bit-DatePicker-callout-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the callout, which the time inputs match.",
        },
        new()
        {
            Name = "--bit-DatePicker-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the callout.",
        },
        new()
        {
            Name = "--bit-DatePicker-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the callout. A standalone picker has none by default and takes one from here.",
        },
        new()
        {
            Name = "--bit-DatePicker-padding",
            DefaultValue = "8px * 1.5 (the spacing unit)",
            Description = "Padding of each picker pane - the day grid, the month and year grids, the time picker.",
        },
        new()
        {
            Name = "--bit-DatePicker-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the cells, the headers and the navigation buttons. The selected day, today and a disabled cell paint their own through the variables below.",
        },
        new()
        {
            Name = "--bit-DatePicker-divider-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the rule between the panes and of the one beside the week numbers.",
        },
        new()
        {
            Name = "--bit-DatePicker-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of a cell, a navigation button or the clear button on hover.",
        },
        new()
        {
            Name = "--bit-DatePicker-active-background",
            DefaultValue = "--bit-clr-bg-pri-active",
            Description = "Background of a cell or a button while it is pressed.",
        },
        new()
        {
            Name = "--bit-DatePicker-day-size",
            DefaultValue = "per Size",
            Description = "Width and height of a day cell, which the week numbers, the weekday headers and the navigation buttons line up with.",
        },
        new()
        {
            Name = "--bit-DatePicker-day-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the day cells and of the headers that line up with them.",
        },
        new()
        {
            Name = "--bit-DatePicker-day-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of a day cell, which the month, year, navigation and time buttons follow.",
        },
        new()
        {
            Name = "--bit-DatePicker-outside-day-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of a day of an adjacent month (ShowOutsideDays).",
        },
        new()
        {
            Name = "--bit-DatePicker-selected-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of the selected day, and of the selected or highlighted month.",
        },
        new()
        {
            Name = "--bit-DatePicker-selected-color",
            DefaultValue = "the cell color",
            Description = "Text color of the selected day.",
        },
        new()
        {
            Name = "--bit-DatePicker-selected-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Color of the ring drawn around the selected day.",
        },
        new()
        {
            Name = "--bit-DatePicker-today-background",
            DefaultValue = "the Color role's main color",
            Description = "Background of today, of the highlighted current month and of the selected AM/PM button.",
        },
        new()
        {
            Name = "--bit-DatePicker-today-color",
            DefaultValue = "the Color role's on-color",
            Description = "Text color of today, of the highlighted current month and of the selected AM/PM button.",
        },
        new()
        {
            Name = "--bit-DatePicker-today-hover-background",
            DefaultValue = "the Color role's hover color",
            Description = "Background of those same three on hover.",
        },
        new()
        {
            Name = "--bit-DatePicker-today-active-background",
            DefaultValue = "the Color role's active color",
            Description = "Background of today and of the selected AM/PM button while pressed.",
        },
        new()
        {
            Name = "--bit-DatePicker-today-radius",
            DefaultValue = "--bit-shp-radius-full",
            Description = "Corner radius of today, which is a full circle by default.",
        },
        new()
        {
            Name = "--bit-DatePicker-highlighted-background",
            DefaultValue = "--bit-clr-bg-ter",
            Description = "Background of a HighlightedDates day.",
        },
        new()
        {
            Name = "--bit-DatePicker-week-number-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of a week number.",
        },
        new()
        {
            Name = "--bit-DatePicker-week-number-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of a week number.",
        },
        new()
        {
            Name = "--bit-DatePicker-header-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the month, year and year-range titles above the grids.",
        },
        new()
        {
            Name = "--bit-DatePicker-time-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the hour, minute and second fields and of the colons between them.",
        },
    ];


    private DateTimeOffset? readOnlyDate = DateTimeOffset.Now;
    private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);
    private DateTimeOffset? startingValue = new DateTimeOffset(2020, 12, 4, 20, 45, 0, DateTimeOffset.Now.Offset);
    private DateTimeOffset? customToday = new DateTimeOffset(2021, 3, 15, 0, 0, 0, DateTimeOffset.Now.Offset);

    private DateTimeOffset? timeZoneDate1;
    private DateTimeOffset? timeZoneDate2;

    private DateTimeOffset? classesValue;
    private DateTimeOffset? monthPickerDate;
    private DateTimeOffset? selectedDateTime;
    private DateTimeOffset? changedDate;

    private bool isMonthPickerVisible = true;
    private bool showMonthPickerAsOverlay;

    private bool isCalloutOpen;
    private BitDatePicker? programmaticPicker;

    private DateTimeOffset? presetsValue;
    private BitDatePicker? presetsPicker;

    private int openCount;
    private int closeCount;
    private int clickCount;
    private int clearCount;
    private int focusInCount;
    private int focusOutCount;
    private DateTimeOffset? displayedMonth;
    private DateTimeOffset? selectedDateEvent;

    private readonly DayOfWeek[] weekendDays = [DayOfWeek.Friday, DayOfWeek.Saturday];

    private readonly DateTimeOffset[] disabledDates =
    [
        DateTimeOffset.Now.AddDays(2),
        DateTimeOffset.Now.AddDays(3),
        DateTimeOffset.Now.AddDays(7)
    ];

    private readonly DateTimeOffset[] highlightedDates =
    [
        DateTimeOffset.Now.AddDays(1),
        DateTimeOffset.Now.AddDays(5),
        DateTimeOffset.Now.AddDays(10)
    ];

    private readonly List<IBitComponentParams> datePickerParams =
    [
        new BitDatePickerParams
        {
            ShowWeekNumbers = true,
            FirstDayOfWeek = DayOfWeek.Monday,
            WeekNumberRule = CalendarWeekRule.FirstFourDayWeek,
            DisabledDaysOfWeek = [DayOfWeek.Saturday, DayOfWeek.Sunday],
            MinDate = DateTimeOffset.Now.AddMonths(-1),
            MaxDate = DateTimeOffset.Now.AddMonths(1),
            Placeholder = "Select a working day",
            ShowClearButton = true,
        }
    ];

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

    private async Task SelectPreset(int days)
    {
        presetsValue = DateTimeOffset.Now.Date.AddDays(days);

        if (presetsPicker is not null)
        {
            await presetsPicker.CloseCalloutAndFocus();
        }
    }
}
