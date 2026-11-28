namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    /// <summary>The topic the address names (/components/daterangepicker/{Topic}); null for the overview.</summary>
    [Parameter] public string? Topic { get; set; }

    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DateRangePicker allows string date inputs. A typed range is validated against every restriction the calendar enforces (MinDate, MaxDate, MinRange, MaxRange, the disabled days and ExcludeDisabledDates), so an out-of-bounds range is rejected as an invalid value.",
        },
        new()
        {
            Name = "ApplyButtonText",
            Type = "string",
            DefaultValue = "Apply",
            Description = "The text of the button that commits the picked range, rendered while AutoApply is off.",
        },
        new()
        {
            Name = "AutoApply",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether every pick is applied to the value as it is made. Turning it off makes the callout a transaction: it renders a Cancel and an Apply button and rolls the range back unless Apply commits it. It has no effect on a Standalone picker and it overrides AutoClose.",
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the DateRangePicker closes automatically after selecting the second value, or after applying a preset. Ignored while AutoApply is off (the callout then waits for its Apply button), on a Standalone picker, and while IsOpen is bound one way.",
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
            Description = "Aria label of the DateRangePicker's callout for screen readers."
        },
        new()
        {
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the bottom of the callout, below everything it holds.",
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the top of the callout, above everything it holds.",
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
            Name = "CancelButtonText",
            Type = "string",
            DefaultValue = "Cancel",
            Description = "The text of the button that discards the picked range, rendered while AutoApply is off.",
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
            Description = "The general color of the DateRangePicker that applies to the today day button, the selected range, the highlighted current month and the selected AM/PM buttons. An explicit value wins over the --bit-DateRangePicker-* accent, text, hover, active, range and focus color variables; left unset, the DateRangePicker is primary unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
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
            Type = "CultureInfo?",
            DefaultValue = "null",
            Description = "CultureInfo for the DateRangePicker. If not set, CultureInfo.CurrentUICulture is used. It drives the calendar system of the grids (e.g. Persian for fa-IR), the month and day names, the first day of the week, the default DateFormat and the parsing of typed text, the AM/PM designators and the text direction. It does not translate the picker's own titles and labels, which are parameters of their own."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of each of the two dates in the DateRangePicker. Defaults to the short date pattern of the Culture, extended with the time pattern when ShowTimePicker is enabled. It is also the pattern typed text is parsed with when AllowTextInput is enabled; with the default format and the time picker on, a bare date is accepted too.",
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the day cells of the DateRangePicker. The context is the day at midnight in the TimeZone; the template replaces only the content of the day button, which still handles the selection, the states and the accessible name."
        },
        new()
        {
            Name = "DisabledDateErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a typed range that lands on a day blocked by DisabledDates, DisabledDaysOfWeek or IsDateDisabled (or covers one under ExcludeDisabledDates), told apart from a range that merely breaks the bounds. Defaults to \"The {DisplayName} field contains a date that is not available.\" It reaches the screen through the cascading EditContext.",
        },
        new()
        {
            Name = "DisabledDates",
            Type = "IEnumerable<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "The list of dates that are disabled (not selectable) in the DateRangePicker, in addition to MinDate and MaxDate. Compared by calendar day in the TimeZone, so the time of each entry does not matter. A range may still span over them unless ExcludeDisabledDates is set."
        },
        new()
        {
            Name = "DisabledDaysOfWeek",
            Type = "IEnumerable<DayOfWeek>?",
            DefaultValue = "null",
            Description = "The days of the week that are disabled (not selectable) in the DateRangePicker (e.g. weekends). A range may still span over them unless ExcludeDisabledDates is set."
        },
        new()
        {
            Name = "DisableFuture",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables every day after today, exactly as a MaxDate of today would. When both are set, the earlier of the two bounds wins. Today stays selectable, and is taken from the Today parameter when it is set.",
        },
        new()
        {
            Name = "DisablePast",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables every day before today, exactly as a MinDate of today would. When both are set, the later of the two bounds wins. Today stays selectable, and is taken from the Today parameter when it is set.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout: TopAndBottom (default) opens it below or above the field, All lets it open beside the field too when that is where the room is.",
            LinkType = LinkType.Link,
            Href = "#drop-direction-enum",
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
            Name = "EndTimeDecreaseHourTitle",
            Type = "string",
            DefaultValue = "Decrease end hour",
            Description = "The title and the aria-label of the end time-picker's decrease-hour button.",
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
            Name = "EndTimeDecreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Decrease end minute",
            Description = "The title and the aria-label of the end time-picker's decrease-minute button.",
        },
        new()
        {
            Name = "EndTimeHourInputAriaLabel",
            Type = "string",
            DefaultValue = "End hour",
            Description = "The aria-label of the end time-picker's hour input.",
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
            Name = "EndTimeIncreaseHourTitle",
            Type = "string",
            DefaultValue = "Increase end hour",
            Description = "The title and the aria-label of the end time-picker's increase-hour button.",
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
            Name = "EndTimeIncreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Increase end minute",
            Description = "The title and the aria-label of the end time-picker's increase-minute button.",
        },
        new()
        {
            Name = "EndTimeMinuteInputAriaLabel",
            Type = "string",
            DefaultValue = "End minute",
            Description = "The aria-label of the end time-picker's minute input.",
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The message shown below the DateRangePicker for a rejection the app decided on its own (a server response, a cross-field rule). It marks the DateRangePicker invalid, is referenced by the input's aria-describedby and is announced as it appears. Standalone, it follows the calendar and describes it instead."
        },
        new()
        {
            Name = "ErrorMessageAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text a screen reader is given for a rejection that has no text of its own: an ErrorMessageTemplate, which is free to render anything at all, or the forced Invalid state. It is announced through the live region the moment the rejection shows up - in place of the default \"Invalid input\" for a template, and only when it is set for the forced state - and the input references it through aria-describedby, so the same reason is read out on every later focus too. A plain ErrorMessage is its own text and never uses it. Give it the reason the template shows, in the language of the page: a template switching to another reason is only announced again when this text switches along with it. Cascaded through BitParams it reaches every DateRangePicker under it, so set it there only as a generic replacement for \"Invalid input\"."
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the error message, which replaces ErrorMessage and marks the DateRangePicker invalid the same way. It replaces the message for a screen reader as well: a template is free to render anything at all, so the live region announces ErrorMessageAriaLabel for it, or a bare \"Invalid input\" when that is not set."
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
            Description = "Whether the day picker should always render six weeks, filling the extra rows with the days of the adjacent months, to keep the calendar height fixed while navigating between months. It is always on when MonthCount renders more than one month, so the months keep an even height next to each other."
        },
        new()
        {
            Name = "GetDayClass",
            Type = "Func<DateTimeOffset, string?>?",
            DefaultValue = "null",
            Description = "Custom function to provide additional CSS classes for each day button of the DateRangePicker. It is handed the day at midnight in the TimeZone, returns null for no class, and runs for every day cell on every render. The buttons are inside the callout, which a component-scoped stylesheet only reaches through ::deep."
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
            Name = "HighlightCurrentMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the current month (today's month, while the month picker shows today's year)."
        },
        new()
        {
            Name = "HighlightedDates",
            Type = "IEnumerable<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "The list of dates that are highlighted (marked) in the day picker of the DateRangePicker. Compared by calendar day in the TimeZone; a highlighted day stays selectable."
        },
        new()
        {
            Name = "HighlightSelectedMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the selected month, which is the month the day picker is currently showing."
        },
        new()
        {
            Name = "HighlightToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the day picker highlights today's cell. It only affects the paint: the cell still reports itself as the current date to assistive technologies.",
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in hours, the spin buttons move the hour by. A step greater than 1 lays a grid over the day, starting at midnight, that every hour the buttons produce sits on. A time entered as text is not held to it. Values below 1 are treated as 1.",
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
            Name = "IconPlacement",
            Type = "BitPlacement",
            DefaultValue = "BitPlacement.End",
            Description = "Determines the side of the input the DateRangePicker's icon is rendered on. Only Start and End are honoured; any other value leaves the icon on the end side. The side follows the reading direction: Start is the left of an LTR input and the right of an RTL one.",
            LinkType = LinkType.Link,
            Href = "#placement-enum",
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
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the DateRangePicker invalid without an EditContext having said so: the same look and aria-invalid a failing validation gives it. ErrorMessage implies it. It says nothing more to a screen reader unless ErrorMessageAriaLabel gives it a reason to announce."
        },
        new()
        {
            Name = "InvalidErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a typed text that cannot be read as a range with ValueFormat and DateFormat. Defaults to \"The {DisplayName} field is not valid.\" It reaches the screen through the cascading EditContext (see ShowValidationMessage)."
        },
        new()
        {
            Name = "IsDateDisabled",
            Type = "Func<DateTimeOffset, bool>?",
            DefaultValue = "null",
            Description = "Custom function to determine if a specific date is disabled (not selectable) in the DateRangePicker. It is handed each day at midnight in the TimeZone and is called for every day cell on every render, so keep it cheap."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DateRangePicker's callout is open. Two-way bindable (@bind-IsOpen); bound one way, the picker never closes the callout itself and the page decides. Ignored on a Standalone picker.",
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
            Description = "The maximum date allowed for the DateRangePicker. The days after it are disabled and the navigation stops at its month; a day counts from its midnight, so the day of MaxDate itself stays selectable.",
        },
        new()
        {
            Name = "MaxRange",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The longest span the selected range is allowed to cover. The day grid is bounded by it in whole days, equally far either side of the picked start date, and the time picker refuses a time that would push the span past it - so a MaxRange shorter than a day leaves the day the range started on as the only one it can end on.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DateRangePicker. The days before it are disabled and the navigation stops at its month. A day counts from its midnight, so a MinDate later than midnight disables its own day too: pass a midnight (e.g. DateTimeOffset.Now.Date) to keep it.",
        },
        new()
        {
            Name = "MinRange",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The minimum number of days that the selected range must span in the DateRangePicker. Only the whole days of the provided TimeSpan are considered, counted between the start and the end day: TimeSpan.FromDays(3) puts the end at least three days after the start, and less than a day has no effect. Once the start is picked, the days closer to it on either side are disabled.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in minutes, the spin buttons move the minute by. A step greater than 1 lays a grid over the hour, starting at the top of it, that every minute the buttons produce sits on - which is what turns it into a five-minute or quarter-hour picker. A time entered as text is not held to it. Values below 1 are treated as 1.",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the month cells of the DateRangePicker. The context is the first day of the month at midnight in the TimeZone; the template replaces only the content of the month button."
        },
        new()
        {
            Name = "MonthCount",
            Type = "int",
            DefaultValue = "1",
            Description = "The number of consecutive months rendered side by side in the day picker (1 to 3), which makes picking a range that spans two months a single move. It falls back to fewer months, down to one, whenever the viewport is not wide enough to fit them all when the callout opens. Values outside 1 to 3 are clamped. With more than one month, FixedWeeks is always on and ShowOutsideDays always off."
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
            Name = "OnApply",
            Type = "EventCallback<BitDateRangePickerValue?>",
            DefaultValue = "",
            Description = "The callback for when the picked range is committed by the Apply button of a picker that does not AutoApply.",
        },
        new()
        {
            Name = "OnCancel",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "The callback for when the picked range is discarded by the Cancel button of a picker that does not AutoApply. A callout dismissed any other way rolls the range back just the same, but reports itself through OnClose alone.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback for when the value is cleared using the clear button, raised after the value has become null and the focus has returned to the input.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            Description = "The callback for clicking on the DateRangePicker's input. It is raised after the callout has opened, also when OpenCallout opened it.",
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
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Callback for when the callout is opened.",
        },
        new()
        {
            Name = "OnPresetSelect",
            Type = "EventCallback<BitDateRangePickerPreset>",
            Description = "The callback for when a preset is selected. The argument is the selected preset. It is not raised for a preset the calendar's restrictions refuse.",
            Href = "#date-range-picker-preset",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<BitDateRangePickerValue?>",
            DefaultValue = "",
            Description = "The callback for when the user picks a day in the calendar, reporting the range as it stands after the pick - so a range with only its start date set, while the second day is still to be chosen, is reported too. It is raised for day picks only: presets, typed text, the time picker and the clear button change the value without it (OnChange follows them all).",
        },
        new()
        {
            Name = "OutOfRangeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a range entered as text that breaks the bounds (MinDate, MaxDate, DisablePast, DisableFuture) or MinRange and MaxRange. Text that does not read as a range at all reports InvalidErrorMessage instead, and a range touching a day blocked by DisabledDates, DisabledDaysOfWeek or IsDateDisabled reports DisabledDateErrorMessage. Defaults to \"The {DisplayName} field is out of the allowed range.\" It reaches the screen through the cascading EditContext.",
        },
        new()
        {
            Name = "PagedNavigation",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the previous and next navigation buttons move the calendar by all of its rendered months instead of one, so consecutive pages of a multi-month calendar never overlap. It has no effect when MonthCount renders a single month.",
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
            Description = "The list of shortcuts, rendered next to the calendar, that fill the DateRangePicker with a predefined range (e.g. \"Last 7 days\"). A preset's range is applied exactly as given, times included. One whose ends fall outside MinDate/MaxDate or on a disabled day, that is shorter than MinRange, or that covers a disabled day under ExcludeDisabledDates is ignored; one longer than MaxRange is cut to it.",
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
            Description = "Enables the responsive mode in small screens: below the small-screen breakpoint the callout becomes a full-width panel that slides down from the top of the screen and is dismissed by swiping it up. It has no effect on a Standalone picker.",
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
            Description = "Whether the clear button should be shown or not when the DateRangePicker has a value. It is never shown while the picker is Disabled or ReadOnly."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker's close button should be shown or not. It is never rendered on a Standalone picker."
        },
        new()
        {
            Name = "ShowGoToToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the GoToToday button should be shown or not. The button navigates back to today's month without selecting anything, and is disabled while that month is already shown."
        },
        new()
        {
            Name = "ShowMonthPicker",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the month picker is shown or hidden. Hidden, the calendar is navigated with the month arrows alone.",
        },
        new()
        {
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date range picker when visible: instead of beside the day grid, it opens over it from the month title. The picker does this on its own on a narrow viewport and whenever the time picker is shown beside the calendar.",
        },
        new()
        {
            Name = "ShowOutsideDays",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the days of the previous and next months, filling the first and last week rows, should be rendered. They can be picked like any other day. It has no effect when MonthCount renders more than one month, since those days would then show up in two grids at once.",
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not render the time-picker: a start time for StartDate and an end time for EndDate. It also adds the time to the default DateFormat. Without it, a picked range runs from 00:00 on its first day to 23:59 on its last (or the times of StartingValue).",
        },
        new()
        {
            Name = "ShowTimePickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show the time picker on top of the date range picker when visible: instead of beside the calendar, it is opened over it by a toggle button (ShowTimePickerIcon / HideTimePickerIcon). The picker does this on its own on a narrow viewport.",
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
            Name = "ShowValidationMessage",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the messages the cascading EditContext holds for the bound field (the InvalidErrorMessage, DisabledDateErrorMessage or OutOfRangeErrorMessage of a typed text among them) under the DateRangePicker, where ErrorMessage goes, so an EditForm needs no ValidationMessage component beside it. The input references them through aria-describedby, so they are read out when it takes the focus; they are not announced as they appear, since a submit validates every field at once. An ErrorMessage or ErrorMessageTemplate of the app's own wins the place while it is set.",
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
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "Sets the preset size (Small, Medium, Large) of the field, the calendar cells and the label. The callout takes the size with it. An explicit value wins over the --bit-DateRangePicker-* size variables (input height and font size, label font size, day size and font size); left unset, the DateRangePicker is medium unless they say otherwise.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker is rendered standalone or with the input component and callout. Standalone, the calendar is part of the page and every pick applies at once; IsOpen, AutoClose, AutoApply, ShowCloseButton, Responsive, DropDirection and AutoFocus have no effect, and the value is carried by a hidden input.",
        },
        new()
        {
            Name = "StartingValue",
            Type = "BitDateRangePickerValue?",
            DefaultValue = "null",
            Description = "Specifies the date and time of the date and time picker when it is opened without any selected value: the month of its StartDate is the one shown, and its start and end times are the times the time picker starts from and the picked days take. It never becomes the value itself.",
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
            Name = "StartTimeDecreaseHourTitle",
            Type = "string",
            DefaultValue = "Decrease start hour",
            Description = "The title and the aria-label of the start time-picker's decrease-hour button.",
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
            Name = "StartTimeDecreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Decrease start minute",
            Description = "The title and the aria-label of the start time-picker's decrease-minute button.",
        },
        new()
        {
            Name = "StartTimeHourInputAriaLabel",
            Type = "string",
            DefaultValue = "Start hour",
            Description = "The aria-label of the start time-picker's hour input.",
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
            Name = "StartTimeIncreaseHourTitle",
            Type = "string",
            DefaultValue = "Increase start hour",
            Description = "The title and the aria-label of the start time-picker's increase-hour button.",
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
            Name = "StartTimeIncreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Increase start minute",
            Description = "The title and the aria-label of the start time-picker's increase-minute button.",
        },
        new()
        {
            Name = "StartTimeMinuteInputAriaLabel",
            Type = "string",
            DefaultValue = "Start minute",
            Description = "The aria-label of the start time-picker's minute input.",
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
            Description = "Time format of the time-pickers, 24H or 12H. TwelveHours adds AM/PM buttons to each time and changes the time pattern of the default DateFormat.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
        },
        new()
        {
            Name = "TimeZone",
            Type = "TimeZoneInfo?",
            DefaultValue = "null",
            Description = "TimeZone for the DateRangePicker. If not set, TimeZoneInfo.Local is used, which is the browser's zone under WebAssembly but the server's under Blazor Server and prerendering. Picked dates carry its offset, and today, the bounds, the disabled and highlighted dates and the times are all read in it."
        },
        new()
        {
            Name = "Today",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Overrides the date considered as today by the DateRangePicker, which is DateTimeOffset.Now by default. It moves the highlighted today cell, the GoToToday button, DisablePast/DisableFuture and the month an empty picker opens on."
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
            Description = "The string format used to show the DateRangePicker's value in its input, where {0} is the start date and {1} is the end date. It is also the template used to parse the typed text back when AllowTextInput is enabled, for which it must hold {0} before {1} with some text between them; its literal parts are matched ignoring case.",
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
            Name = "WeekNumbersHeaderTitle",
            Type = "string",
            DefaultValue = "Week",
            Description = "The accessible name of the header cell above the week numbers column, which shows a number sign (#).",
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
            Description = "Custom template to render the year cells of the DateRangePicker. The context is the year, counted in the Culture's calendar; the template replaces only the content of the year button."
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

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-DateRangePicker-color",
            DefaultValue = "--bit-clr-pri",
            Description = "The accent: the two ends of the selected range, today's cell, the selected preset, the active AM/PM button and the border of the field while the calendar is open. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-text-color",
            DefaultValue = "--bit-clr-pri-text",
            Description = "Foreground of whatever the accent fills. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-hover-color",
            DefaultValue = "--bit-clr-pri-hover",
            Description = "The accent while hovered. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-active-color",
            DefaultValue = "--bit-clr-pri-active",
            Description = "The accent while pressed. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-range-background",
            DefaultValue = "--bit-clr-pri-light",
            Description = "Background of the days between the two ends of the selected range. The Color parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-preview-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of the range previewed while the pointer moves over the grid with only the start date picked.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-focus-color",
            DefaultValue = "--bit-clr-pri-focus",
            Description = "Color of the focus ring, around the field and around every cell and button of the calendar. An invalid value overrides it with the error focus color, so the field never shows two states at once. The Color parameter wins over it. While it is unset, every focus ring held off its element is the library's own --bit-shd-focus-ring; the ones drawn flush against a cell or a field keep this color.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-invalid-color",
            DefaultValue = "--bit-clr-err",
            Description = "Border and underline of the field while the value is invalid, and the error message.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-required-color",
            DefaultValue = "--bit-clr-req",
            Description = "Color of the asterisk after a required label.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text and glyphs of every disabled part, in the field and in the calendar alike.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-height",
            DefaultValue = "--bit-siz-ctrl-md",
            Description = "Height of the field. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of the field. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the field.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-placeholder-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of the placeholder. It replaces the grey the browser picks on its own, which is not derived from the theme and drops below the contrast floor on most surfaces.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the field.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Border of the field, and its underline in the Underlined variant.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-border-width",
            DefaultValue = "--bit-shp-border-width",
            Description = "Border stroke of the field.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-input-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the field.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-icon-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the calendar glyph inside the field.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the label.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-label-font-size",
            DefaultValue = "--bit-tpg-fs-sm",
            Description = "Text size of the label. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-label-font-weight",
            DefaultValue = "--bit-tpg-field-label-font-weight",
            Description = "Weight of the label.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-callout-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the calendar surface.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the callout.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the callout.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-calendar-padding",
            DefaultValue = "12px",
            Description = "Padding around each pane of the callout: a month, the month/year picker and the presets column.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-day-size",
            DefaultValue = "28px",
            Description = "Width and height of a day cell. The week numbers and the navigation buttons track it, so the whole calendar scales with this one value. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-day-font-size",
            DefaultValue = "--bit-tpg-fs-xs",
            Description = "Text size of a day cell and of the weekday initials above it. The Size parameter wins over it.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-day-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of a day cell.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-day-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of a day cell.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-day-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of a hovered day and of a hovered navigation button.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-today-radius",
            DefaultValue = "--bit-shp-radius-full",
            Description = "Corner radius of today's cell, which is a circle by default while the other days take the day radius.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-range-radius",
            DefaultValue = "--bit-shp-radius-lg",
            Description = "Corner radius at the two ends of the selected range, which is what gives the range its pill shape. A range of a single day takes it on all four corners.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-outside-day-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the days of the adjacent months rendered by ShowOutsideDays.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-highlighted-background",
            DefaultValue = "--bit-clr-bg-ter",
            Description = "Background of a day listed in HighlightedDates.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-week-day-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the row of weekday initials.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-week-number-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the week number column.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-week-number-background",
            DefaultValue = "--bit-clr-bg-sec",
            Description = "Background of the week number column.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-header-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the month and year titles above the grids.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-divider-color",
            DefaultValue = "--bit-clr-brd-sec",
            Description = "Color of the rules between the panes of the callout and beside the week numbers.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-presets-width",
            DefaultValue = "112px",
            Description = "Width of the presets column.",
        },
        new()
        {
            Name = "--bit-DateRangePicker-presets-max-height",
            DefaultValue = "320px",
            Description = "Height the presets column scrolls in before it starts scrolling.",
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
                   Description = "Indicates the beginning of the date range. A day picked in the calendar carries the start time: 00:00 unless the time picker or StartingValue sets another.",
               },
               new()
               {
                   Name = "EndDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "null",
                   Description = "Indicates the end of the date range, null while only the start is picked. A day picked in the calendar carries the end time: 23:59 unless the time picker or StartingValue sets another.",
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
                    Description = "The range applied when the preset is selected, exactly as given, times included. ValueProvider takes precedence over this when both are set.",
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
                    Name = "IsDisabled",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the preset's button is disabled.",
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
                    Name = "ActionsContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions row the Apply and Cancel buttons of a picker that does not AutoApply are laid out in."
                },
                new()
                {
                    Name = "ApplyButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Apply button of a picker that does not AutoApply."
                },
                new()
                {
                    Name = "CancelButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Cancel button of a picker that does not AutoApply."
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
                    Name = "ErrorMessageContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the error message container of the BitDateRangePicker."
                },
                new()
                {
                    Name = "ErrorMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the error message of the BitDateRangePicker."
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
                    Description = "Custom CSS classes/styles for the callout of the BitDateRangePicker. A --bit-DateRangePicker-* variable set here wins over one set in Style or Styles.Root."
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
                    Name = "CalloutHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout header, the container of the CalloutHeaderTemplate."
                },
                new()
                {
                    Name = "CalloutFooter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout footer, the container of the CalloutFooterTemplate."
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
        DemoSharedEnums.BitColor(),
        DemoSharedEnums.BitDropDirection(),
        DemoSharedEnums.BitPlacement(),
        DemoSharedEnums.BitSize(),
        DemoSharedEnums.BitTimeFormat()
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
            IsDisabled = true,
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
    private FormValidationDateRangePickerModel customMessagesModel = new();

    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }

    private BitDateRangePickerValue? stayRange = new()
    {
        StartDate = new DateTimeOffset(DateTime.Today.AddDays(7)),
        EndDate = new DateTimeOffset(DateTime.Today.AddDays(8)),
    };

    private string? GetStayError()
    {
        if (stayRange?.StartDate is not { } start || stayRange.EndDate is not { } end) return null;

        return (end - start).TotalDays < 2 ? "A stay must be at least two nights." : null;
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

    private bool isOpen;
    private BitDateRangePickerValue? isOpenValue;
    private BitDateRangePicker? calloutPicker;

    private async Task OpenFromCode()
    {
        if (calloutPicker is not null)
        {
            await calloutPicker.OpenCallout();
        }
    }

    private async Task CloseFromCode()
    {
        if (calloutPicker is not null)
        {
            await calloutPicker.CloseCallout();
        }
    }

    private string? applyOutcome;
    private BitDateRangePickerValue? applyValue;

    private BitDateRangePickerValue? timeZoneDateRange1 = new();
    private BitDateRangePickerValue? timeZoneDateRange2 = new();

    private BitDateRangePickerValue? eventsValue;
    private readonly List<string> eventLog = [];

    private void Log(string message)
    {
        eventLog.Insert(0, message);

        if (eventLog.Count > 8)
        {
            eventLog.RemoveAt(eventLog.Count - 1);
        }
    }

    private static string Fmt(BitDateRangePickerValue? value)
    {
        return $"{value?.StartDate?.ToString("d") ?? "-"} to {value?.EndDate?.ToString("d") ?? "-"}";
    }

    private void LogSelectDate(BitDateRangePickerValue? value) => Log($"OnSelectDate: {Fmt(value)}");
    private void LogChange(BitDateRangePickerValue? value) => Log($"OnChange: {Fmt(value)}");
    private void LogMonthChange(DateTimeOffset date) => Log($"OnMonthChange: {date:yyyy/MM}");
    private void LogClear() => Log("OnClear");
    private void LogOpen() => Log("OnOpen");
    private void LogClose() => Log("OnClose");

    private readonly IBitComponentParams[] dateRangePickerParams =
    [
        new BitDateRangePickerParams
        {
            MonthCount = 2,
            ShowClearButton = true,
            FirstDayOfWeek = DayOfWeek.Monday,
            MinDate = DateTimeOffset.Now.Date,
            MaxRange = TimeSpan.FromDays(30),
            Placeholder = "Pick your dates",
            Presets =
            [
                new() { Text = "Next 7 days", ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date, EndDate = DateTimeOffset.Now.Date.AddDays(6) } },
                new() { Text = "Next 14 days", ValueProvider = () => new() { StartDate = DateTimeOffset.Now.Date, EndDate = DateTimeOffset.Now.Date.AddDays(13) } },
            ]
        }
    ];
}
