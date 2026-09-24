namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePicker;

public partial class BitTimePickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker allows input a time string directly or not.",
        },
        new()
        {
            Name = "AllowedHours",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The hours that can be selected, on top of what MinTime and MaxTime already allow. The predicate receives an hour of the day (0-23); the spin buttons skip over the hours it rejects, a typed one snaps to the nearest it accepts, and a time entered as text that lands on one fails validation.",
        },
        new()
        {
            Name = "AllowedMinutes",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The minutes that can be selected, on top of what MinTime and MaxTime already allow. The predicate receives a minute of the hour (0-59); the spin buttons skip over the minutes it rejects, a typed one snaps to the nearest it accepts, and a time entered as text that lands on one fails validation.",
        },
        new()
        {
            Name = "AllowedSeconds",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The seconds that can be selected, on top of what MinTime and MaxTime already allow. The predicate receives a second of the minute (0-59); the spin buttons skip over the seconds it rejects, a typed one snaps to the nearest it accepts, and a time entered as text that lands on one fails validation.",
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text tied to the TimePicker as its accessible description without being shown on the screen, which is what lets a field carry an instruction too long to put next to it. It is read after Description, so the two can be used together.",
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the input of the TimePicker gets the focus as soon as it renders for the first time.",
        },
        new()
        {
            Name = "CalloutAriaLabel",
            Type = "string",
            DefaultValue = "Clock",
            Description = "Aria label for time picker popup for screen reader users."
        },
        new()
        {
            Name = "CalloutFooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the bottom of the TimePicker's callout, below the time inputs and the action buttons."
        },
        new()
        {
            Name = "CalloutHeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template to render at the top of the TimePicker's callout, above the time inputs."
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<String, Object>()",
            Description = "Capture and render additional attributes in addition to the main callout's parameters."
        },
        new()
        {
            Name = "Classes",
            Type = "BitTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "ClearButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the clear button of the input, shown when ShowInputClearButton is set, using custom CSS classes for external icon libraries. Takes precedence over ClearButtonIconName when both are set.",
        },
        new()
        {
            Name = "ClearButtonIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the clear button of the input from the built-in Fluent UI icons.",
        },
        new()
        {
            Name = "ClearButtonText",
            Type = "string",
            DefaultValue = "Clear",
            Description = "The text of the clear button, shown when ShowClearButton is set.",
        },
        new()
        {
            Name = "ClearButtonTitle",
            Type = "string",
            DefaultValue = "Clear the selected time",
            Description = "The title of the clear button of the input (tooltip and aria-label), shown when ShowInputClearButton is set.",
        },
        new()
        {
            Name = "CloseButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the close button icon using custom CSS classes for external icon libraries. Takes precedence over CloseButtonIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "CloseButtonIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "Gets or sets the name of the close button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close time picker",
            Description = "The title of the close button (tooltip).",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the TimePicker, which applies to the selected AM/PM button, the now and clear action buttons, and the focus indicator of the input.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "ContinuousSpinDelay",
            Type = "int",
            DefaultValue = "400",
            Description = "The delay in milliseconds before the time part starts changing continuously while an increase/decrease button is held down.",
        },
        new()
        {
            Name = "ContinuousSpinInterval",
            Type = "int",
            DefaultValue = "75",
            Description = "The interval in milliseconds between two consecutive changes while an increase/decrease button is held down.",
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the TimePicker. It provides the AM/PM designators and the pattern the value is written in, and a culture that reads right to left lays the picker out that way without an explicit Dir."
        },
        new()
        {
            Name = "DecreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the decrease hour button icon using custom CSS classes for external icon libraries. Takes precedence over DecreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DecreaseHourIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the decrease hour button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DecreaseHourTitle",
            Type = "string",
            DefaultValue = "Decrease hour",
            Description = "The title of the decrease hour button (tooltip and aria-label).",
        },
        new()
        {
            Name = "DecreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the decrease minute button icon using custom CSS classes for external icon libraries. Takes precedence over DecreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DecreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the decrease minute button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DecreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Decrease minute",
            Description = "The title of the decrease minute button (tooltip and aria-label).",
        },
        new()
        {
            Name = "DecreaseSecondIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the decrease second button icon using custom CSS classes for external icon libraries. Takes precedence over DecreaseSecondIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "DecreaseSecondIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the decrease second button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "DecreaseSecondTitle",
            Type = "string",
            DefaultValue = "Decrease second",
            Description = "The title of the decrease second button (tooltip and aria-label).",
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "The helper text of the TimePicker, rendered under the field. The input references it through its aria-describedby, so a screen reader reads it along with the picker.",
        },
        new()
        {
            Name = "DescriptionTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template for the helper text of the TimePicker, which replaces Description. It is tied to the picker as its accessible description in the same way.",
        },
        new()
        {
            Name = "DisallowedTimeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a time entered as text that AllowedHours, AllowedMinutes or AllowedSeconds rejects.",
        },
        new()
        {
            Name = "DisableFuture",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables every time of day after the current time, exactly as a MaxTime of now would. When both are set, the earlier of the two bounds wins.",
        },
        new()
        {
            Name = "DisablePast",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables every time of day before the current time, exactly as a MinTime of now would. When both are set, the later of the two bounds wins.",
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
            Href = "#drop-direction-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "ErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The error message rendered under the field, which also marks the picker invalid. It is meant for a rejection the app itself knows about; a picker inside an EditForm already gets its messages from the cascading EditContext.",
        },
        new()
        {
            Name = "ErrorMessageTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the error message, which replaces the plain ErrorMessage text and marks the picker invalid in the same way.",
        },
        new()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "true",
            Description = "Determines if the TimePicker has a border.",
        },
        new()
        {
            Name = "HourInputAriaLabel",
            Type = "string",
            DefaultValue = "Hour",
            Description = "The aria-label of the hour input.",
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in hours, the spin buttons move the hour by. A step greater than 1 lays a grid over the day, starting at the hour of MinTime, and at midnight where there is none, that every hour the buttons produce sits on. A time entered as text is not held to it.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display using custom CSS classes for external icon libraries. Takes precedence over IconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "Clock",
            Description = "Gets or sets the name of the icon to display from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
            DefaultValue = "BitIconLocation.Right",
            Description = "TimePicker icon location."
        },
        new()
        {
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom TimePicker icon template."
        },
        new()
        {
            Name = "IncreaseHourIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the increase hour button icon using custom CSS classes for external icon libraries. Takes precedence over IncreaseHourIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IncreaseHourIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the increase hour button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IncreaseHourTitle",
            Type = "string",
            DefaultValue = "Increase hour",
            Description = "The title of the increase hour button (tooltip and aria-label).",
        },
        new()
        {
            Name = "IncreaseMinuteIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the increase minute button icon using custom CSS classes for external icon libraries. Takes precedence over IncreaseMinuteIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IncreaseMinuteIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the increase minute button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IncreaseMinuteTitle",
            Type = "string",
            DefaultValue = "Increase minute",
            Description = "The title of the increase minute button (tooltip and aria-label).",
        },
        new()
        {
            Name = "IncreaseSecondIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the increase second button icon using custom CSS classes for external icon libraries. Takes precedence over IncreaseSecondIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "IncreaseSecondIconName",
            Type = "string?",
            DefaultValue = "ChevronDownSmall",
            Description = "Gets or sets the name of the increase second button icon from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "IncreaseSecondTitle",
            Type = "string",
            DefaultValue = "Increase second",
            Description = "The title of the increase second button (tooltip and aria-label).",
        },
        new()
        {
            Name = "Invalid",
            Type = "bool",
            DefaultValue = "false",
            Description = "Marks the TimePicker as invalid without an EditContext having said so, giving it the same look and the same aria-invalid attribute an invalid bound value does. Setting ErrorMessage implies it.",
        },
        new()
        {
            Name = "InvalidErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for the invalid value.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this TimePicker is open. Supports two-way binding to open and close the callout from code.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label for the TimePicker.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the TimePicker."
        },
        new()
        {
            Name = "MaxTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The maximum time of day that can be selected. Stepping clamps the value into the bound, a typed time beyond it fails validation, and a bound outside of a day is clamped into one before it is applied.",
        },
        new()
        {
            Name = "MinTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The minimum time of day that can be selected. Stepping clamps the value into the bound, a typed time beyond it fails validation, and a bound outside of a day is clamped into one before it is applied.",
        },
        new()
        {
            Name = "MinuteInputAriaLabel",
            Type = "string",
            DefaultValue = "Minute",
            Description = "The aria-label of the minute input.",
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in minutes, the spin buttons move the minute by. A step greater than 1 lays a grid over the hour, starting at the minute of MinTime, and at the top of the hour where there is none, that every minute the buttons produce sits on - which is what turns it into a five-minute or quarter-hour picker. A time entered as text is not held to it.",
        },
        new()
        {
            Name = "NowButtonText",
            Type = "string",
            DefaultValue = "Now",
            Description = "The text of the now button, shown when ShowNowButton is set.",
        },
        new()
        {
            Name = "OnClear",
            Type = "EventCallback",
            Description = "Callback for when the value is cleared using the clear button.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            Description = "Callback for when clicking on TimePicker input.",
        },
        new()
        {
            Name = "OnClose",
            Type = "EventCallback",
            Description = "Callback for when the callout of the TimePicker is closed.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "Callback for when focus moves into the TimePicker input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "Callback for when focus moves into the TimePicker input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "Callback for when focus moves out of the TimePicker input.",
        },
        new()
        {
            Name = "OnOpen",
            Type = "EventCallback",
            Description = "Callback for when the callout of the TimePicker is opened.",
        },
        new()
        {
            Name = "OnSelectTime",
            Type = "EventCallback<TimeSpan?>",
            Description = "Callback for when the selected time changes.",
        },
        new()
        {
            Name = "OutOfRangeErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for a time entered as text that falls outside of MinTime and MaxTime.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder text for the TimePicker.",
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
            Name = "SecondInputAriaLabel",
            Type = "string",
            DefaultValue = "Second",
            Description = "The aria-label of the second input.",
        },
        new()
        {
            Name = "SecondStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in seconds, the spin buttons move the second by. A step greater than 1 lays a grid over the minute, starting at the second of MinTime, and at the top of the minute where there is none, that every second the buttons produce sits on. A time entered as text is not held to it.",
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the BitTimePicker's clear button should be shown or not."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the BitTimePicker's close button should be shown or not."
        },
        new()
        {
            Name = "ShowInputClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows a clear button inside the field once a time is selected, so the value can be taken back without opening the callout. It is not rendered while the picker is read-only or standalone.",
        },
        new()
        {
            Name = "ShowNowButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the BitTimePicker's now button should be shown or not. The button selects the current time of day, snapped to the steps and to the allowed values."
        },
        new()
        {
            Name = "ShowSeconds",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the BitTimePicker shows the seconds input or not."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the TimePicker, which scales the input, the label, the time inputs and the spin buttons.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the BitTimePicker is rendered standalone or with the input component and callout.",
        },
        new()
        {
            Name = "StartingValue",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The time an empty TimePicker starts from, instead of midnight. It is not a value: the picker stays empty until something is picked, but the first change made to it lands around the given time.",
        },
        new()
        {
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "The time format of the time-picker, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
        },
        new()
        {
            Name = "Underlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Text field of the TimePicker is underlined.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = @"The format of the time in the TimePicker like ""HH:mm"". Left unset it follows the time pattern of the culture, rewritten into the TimeFormat, extended with the seconds where ShowSeconds is set and padded with the leading zeros.",
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "InputId",
            Type = "string?",
            Description = "The id of the input element of the TimePicker."
        },
        new()
        {
            Name = "OpenCallout",
            Type = "Task OpenCallout()",
            Description = "Opens the callout of the TimePicker, doing nothing when it is already open or when the picker is standalone and has no callout to open."
        },
        new()
        {
            Name = "DismissCallout",
            Type = "Task DismissCallout()",
            Description = "Closes the callout of the TimePicker, leaving the focus wherever it is."
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
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
                    Value = "0",
                },
                new()
                {
                    Name = "Hidden",
                    Description = "Hide content of the component,though the space it takes on the page remains.",
                    Value = "1",
                },
                new()
                {
                    Name = "Collapsed",
                    Description = "Hide content of the component,though the space it takes on the page gone.",
                    Value = "2",
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
                    Value = "0",
                },
                new()
                {
                    Name = "Right",
                    Description = "Show the icon at the right side.",
                    Value = "1",
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
        },
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
                new() { Name = "Small", Description = "The small size TimePicker.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size TimePicker.", Value = "1" },
                new() { Name = "Large", Description = "The large size TimePicker.", Value = "2" }
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
                    Value = "0",
                },
                new()
                {
                    Name = "TopAndBottom",
                    Description = "Show the callout at the top or bottom side.",
                    Value = "1",
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "timepicker-class-styles",
            Title = "BitTimePickerClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitTimePicker."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused state of the BitTimePicker."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Label of the BitTimePicker."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input wrapper of the BitTimePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitTimePicker."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input of the BitTimePicker."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitTimePicker."
                },
                new()
                {
                    Name = "InputClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear button rendered inside the input of the BitTimePicker."
                },
                new()
                {
                    Name = "InputClearButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the clear button rendered inside the input of the BitTimePicker."
                },
                new()
                {
                    Name = "ErrorMessageContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the error message of the BitTimePicker."
                },
                new()
                {
                    Name = "ErrorMessage",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the error message of the BitTimePicker."
                },
                new()
                {
                    Name = "DescriptionContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the description (helper text) of the BitTimePicker."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description (helper text) of the BitTimePicker."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitTimePicker."
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout of the BitTimePicker."
                },
                new()
                {
                    Name = "CalloutContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout container of the BitTimePicker."
                },
                new()
                {
                    Name = "CalloutHeader",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout header of the BitTimePicker."
                },
                new()
                {
                    Name = "CalloutFooter",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout footer of the BitTimePicker."
                },
                new()
                {
                    Name = "TimeInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the time input container of the BitTimePicker."
                },
                new()
                {
                    Name = "HourInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour input container of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase hour button of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase hour icon of the BitTimePicker."
                },
                new()
                {
                    Name = "HourInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour input of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseHourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease hour button of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseHourIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease hour icon of the BitTimePicker."
                },
                new()
                {
                    Name = "HourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour minute separator of the BitTimePicker."
                },
                new()
                {
                    Name = "MinuteInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the minute input container of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase minute button of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase minute icon of the BitTimePicker."
                },
                new()
                {
                    Name = "MinuteInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the minute input of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseMinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease minute button of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseMinuteIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease minute icon of the BitTimePicker."
                },
                new()
                {
                    Name = "MinuteSecondSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the minute second separator of the BitTimePicker."
                },
                new()
                {
                    Name = "SecondInputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the second input container of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseSecondButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase second button of the BitTimePicker."
                },
                new()
                {
                    Name = "IncreaseSecondIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the increase second icon of the BitTimePicker."
                },
                new()
                {
                    Name = "SecondInput",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the second input of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseSecondButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease second button of the BitTimePicker."
                },
                new()
                {
                    Name = "DecreaseSecondIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the decrease second icon of the BitTimePicker."
                },
                new()
                {
                    Name = "AmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the AM/PM container of the BitTimePicker."
                },
                new()
                {
                    Name = "AmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the AM button of the BitTimePicker."
                },
                new()
                {
                    Name = "PmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the PM button of the BitTimePicker."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the actions container of the BitTimePicker."
                },
                new()
                {
                    Name = "NowButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the now button of the BitTimePicker."
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clear button of the BitTimePicker."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the BitTimePicker."
                },
                new()
                {
                    Name = "CloseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button icon of the BitTimePicker."
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



    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new()
        {
            Name = "--bit-TimePicker-color",
            DefaultValue = "The Color role's main color",
            Description = "Accent of the picker: the background of the selected AM/PM button and the text of the now and clear action buttons.",
        },
        new()
        {
            Name = "--bit-TimePicker-on-color",
            DefaultValue = "The Color role's on-color",
            Description = "Text color over that accent, which is what the selected AM/PM button is written in.",
        },
        new()
        {
            Name = "--bit-TimePicker-hover-color",
            DefaultValue = "The Color role's hover color",
            Description = "Accent of the selected AM/PM button while it is hovered (pointer devices only).",
        },
        new()
        {
            Name = "--bit-TimePicker-active-color",
            DefaultValue = "The Color role's active color",
            Description = "Accent of the selected AM/PM button while it is pressed.",
        },
        new()
        {
            Name = "--bit-TimePicker-focus-color",
            DefaultValue = "The Color role's focus color",
            Description = "Color of the keyboard focus ring drawn around the field and around every control of the callout.",
        },
        new()
        {
            Name = "--bit-TimePicker-invalid-color",
            DefaultValue = "--bit-clr-err",
            Description = "Border and underline of an invalid field, and the color of the ErrorMessage text. The focus ring keeps its own error color.",
        },
        new()
        {
            Name = "--bit-TimePicker-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-text-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-placeholder-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Text color of the placeholder.",
        },
        new()
        {
            Name = "--bit-TimePicker-border-color",
            DefaultValue = "--bit-clr-brd-pri",
            Description = "Border of the input field, and the rule of the underlined variant.",
        },
        new()
        {
            Name = "--bit-TimePicker-border-width",
            DefaultValue = "--bit-shp-border-width",
            Description = "Stroke of that border.",
        },
        new()
        {
            Name = "--bit-TimePicker-radius",
            DefaultValue = "--bit-shp-radius-control",
            Description = "Corner radius of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-height",
            DefaultValue = "per Size, --bit-siz-ctrl-*",
            Description = "Height of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-padding",
            DefaultValue = "spacing(1)",
            Description = "Inline padding of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the input field.",
        },
        new()
        {
            Name = "--bit-TimePicker-icon-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the field icon and of the clear button inside the field.",
        },
        new()
        {
            Name = "--bit-TimePicker-icon-size",
            DefaultValue = "per Size, --bit-siz-icon-*",
            Description = "Size of the field icon and of the clear button glyph.",
        },
        new()
        {
            Name = "--bit-TimePicker-label-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Color of the label.",
        },
        new()
        {
            Name = "--bit-TimePicker-label-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the label.",
        },
        new()
        {
            Name = "--bit-TimePicker-label-font-weight",
            DefaultValue = "--bit-tpg-fw-semibold",
            Description = "Weight of the label.",
        },
        new()
        {
            Name = "--bit-TimePicker-required-color",
            DefaultValue = "--bit-clr-req",
            Description = "Color of the asterisk of a required picker.",
        },
        new()
        {
            Name = "--bit-TimePicker-description-color",
            DefaultValue = "--bit-clr-fg-sec",
            Description = "Color of the helper text.",
        },
        new()
        {
            Name = "--bit-TimePicker-description-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the helper text and of the error message.",
        },
        new()
        {
            Name = "--bit-TimePicker-disabled-color",
            DefaultValue = "--bit-clr-fg-dis",
            Description = "Text, icon and glyph color of a disabled picker.",
        },
        new()
        {
            Name = "--bit-TimePicker-disabled-background",
            DefaultValue = "--bit-clr-bg-dis",
            Description = "Background of a disabled field.",
        },
        new()
        {
            Name = "--bit-TimePicker-disabled-border-color",
            DefaultValue = "--bit-clr-brd-dis",
            Description = "Border of a disabled field.",
        },
        new()
        {
            Name = "--bit-TimePicker-callout-background",
            DefaultValue = "--bit-clr-bg-pri",
            Description = "Background of the popup.",
        },
        new()
        {
            Name = "--bit-TimePicker-callout-radius",
            DefaultValue = "--bit-shp-radius-popup",
            Description = "Corner radius of the popup.",
        },
        new()
        {
            Name = "--bit-TimePicker-callout-padding",
            DefaultValue = "spacing(2)",
            Description = "Padding inside the popup.",
        },
        new()
        {
            Name = "--bit-TimePicker-callout-shadow",
            DefaultValue = "--bit-shd-popup",
            Description = "Elevation of the popup.",
        },
        new()
        {
            Name = "--bit-TimePicker-gap",
            DefaultValue = "spacing(1)",
            Description = "Space between the hour, the minute and the second groups.",
        },
        new()
        {
            Name = "--bit-TimePicker-cell-size",
            DefaultValue = "per Size",
            Description = "Width and height of a time input and of a spin button.",
        },
        new()
        {
            Name = "--bit-TimePicker-cell-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the time inputs.",
        },
        new()
        {
            Name = "--bit-TimePicker-cell-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Text color of the time inputs.",
        },
        new()
        {
            Name = "--bit-TimePicker-cell-font-weight",
            DefaultValue = "--bit-tpg-font-weight",
            Description = "Weight of the time inputs, the separators and the AM/PM buttons.",
        },
        new()
        {
            Name = "--bit-TimePicker-cell-radius",
            DefaultValue = "--bit-shp-radius-button",
            Description = "Corner radius of a spin button, of the AM/PM buttons and of the action buttons.",
        },
        new()
        {
            Name = "--bit-TimePicker-separator-color",
            DefaultValue = "inherited",
            Description = "Color of the \":\" between the parts of the time.",
        },
        new()
        {
            Name = "--bit-TimePicker-separator-font-size",
            DefaultValue = "per Size",
            Description = "Size of the \":\" between the parts of the time.",
        },
        new()
        {
            Name = "--bit-TimePicker-spin-color",
            DefaultValue = "--bit-clr-fg-pri",
            Description = "Glyph color of a spin button.",
        },
        new()
        {
            Name = "--bit-TimePicker-spin-font-size",
            DefaultValue = "per Size",
            Description = "Glyph size of a spin button.",
        },
        new()
        {
            Name = "--bit-TimePicker-spin-hover-background",
            DefaultValue = "--bit-clr-bg-pri-hover",
            Description = "Background of a hovered spin button, action button or clear button.",
        },
        new()
        {
            Name = "--bit-TimePicker-spin-active-background",
            DefaultValue = "--bit-clr-bg-pri-active",
            Description = "Background of a pressed spin button, action button or clear button.",
        },
        new()
        {
            Name = "--bit-TimePicker-meridiem-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the AM/PM buttons.",
        },
        new()
        {
            Name = "--bit-TimePicker-action-font-size",
            DefaultValue = "per Size",
            Description = "Text size of the now and clear action buttons.",
        },
    ];



    private readonly List<IBitComponentParams> timePickerParams =
    [
        new BitTimePickerParams
        {
            MinuteStep = 15,
            ShowNowButton = true,
            ShowClearButton = true,
            ShowInputClearButton = true,
            Placeholder = "Select a time...",
            TimeFormat = BitTimeFormat.TwelveHours,
        }
    ];

    private bool isCalloutOpen;
    private readonly List<string> eventLogs = [];
    private TimeSpan? actionsTime;
    private TimeSpan? classesValue;
    private TimeSpan? templateTime;
    private TimeSpan? readOnlyTime = new(2, 50, 0);
    private TimeSpan? selectedTime = new(5, 12, 15);
    private FormValidationTimePickerModel formValidationTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitTimePicker timePicker = default!;
    private BitTimePicker? programmaticPicker;

    private async Task OpenCallout()
    {
        await timePicker.OpenCallout();
    }

    private async Task HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        successMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }

    private void LogOpen() => Log("OnOpen");
    private void LogClose() => Log("OnClose");
    private void LogClick() => Log("OnClick");
    private void LogClear() => Log("OnClear");
    private void LogFocusIn() => Log("OnFocusIn");
    private void LogFocusOut() => Log("OnFocusOut");
    private void LogSelectTime(TimeSpan? time) => Log($"OnSelectTime: {time}");
    private void LogChange(TimeSpan? time) => Log($"OnChange: {time}");

    private void Log(string message)
    {
        eventLogs.Insert(0, message);

        if (eventLogs.Count > 8)
        {
            eventLogs.RemoveRange(8, eventLogs.Count - 8);
        }
    }
}
