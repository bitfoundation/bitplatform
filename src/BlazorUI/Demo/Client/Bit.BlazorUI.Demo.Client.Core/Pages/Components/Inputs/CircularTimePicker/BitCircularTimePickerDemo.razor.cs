using System.Globalization;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.CircularTimePicker;

public partial class BitCircularTimePickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AllowedHours",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The hours that can be selected, on top of what MinTime, MaxTime and HourStep already allow. The predicate receives an hour of the day (0-23) whichever TimeFormat the clock is in.",
        },
        new()
        {
            Name = "AllowedMinutes",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The minutes that can be selected, on top of what MinTime, MaxTime and MinuteStep already allow. The predicate receives a minute of the hour (0-59).",
        },
        new()
        {
            Name = "AllowedSeconds",
            Type = "Func<int, bool>?",
            DefaultValue = "null",
            Description = "The seconds that can be selected, on top of what MinTime, MaxTime and SecondStep already allow. The predicate receives a second of the minute (0-59).",
        },
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker allows input a time string directly or not. The text is parsed with the exact ValueFormat of the picker.",
        },
        new()
        {
            Name = "AmPmInClock",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the AM/PM pair under the clock instead of beside the time in the toolbar. Only the 12-hour format has a meridiem to place."
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "Closes the callout as soon as the selection is complete - the last part the dial offers is picked, or the \"now\" button is used - without waiting for the close button or a click outside of it."
        },
        new()
        {
            Name = "AutoFocus",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the input of the TimePicker automatically receives focus when the page renders."
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
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional attributes in addition to the main callout's parameters."
        },
        new()
        {
            Name = "Classes",
            Type = "BitCircularTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "ClearButtonText",
            Type = "string",
            DefaultValue = "Clear",
            Description = "The text of the button that clears the value of the TimePicker."
        },
        new()
        {
            Name = "CloseButtonIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon for the close button using external icon libraries. Takes precedence over CloseButtonIconName when both are set."
        },
        new()
        {
            Name = "CloseButtonIconName",
            Type = "string?",
            DefaultValue = "Cancel",
            Description = "The name of the icon for the close button from the built-in Fluent UI icons. For external icon libraries, use CloseButtonIcon instead."
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close time picker",
            Description = "The title of the close button (tooltip)."
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the TimePicker, applied to the toolbar, the dial pointer and the selected numbers.",
            Href = "#color-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo?",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the TimePicker."
        },
        new()
        {
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout."
        },
        new()
        {
            Name = "EditMode",
            Type = "BitCircularTimePickerEditMode",
            LinkType = LinkType.Link,
            Href = "#edit-mode-enum",
            DefaultValue = "BitCircularTimePickerEditMode.Normal",
            Description = "Choose the edition mode. By default, you can edit every part the picker shows."
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
            Name = "HourButtonTitle",
            Type = "string",
            DefaultValue = "Select hour",
            Description = "The title (and accessible name) of the button that switches the dial to the hours."
        },
        new()
        {
            Name = "HourStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in hours, the dial and the keyboard move the hour by. A step greater than 1 dims the hours in between."
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon to display using custom CSS classes for external icon libraries (e.g., FontAwesome, Bootstrap Icons). Takes precedence over IconName when both are set."
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
            Name = "IconName",
            Type = "string?",
            DefaultValue = "Clock",
            Description = "The name of the icon from the built-in Fluent UI icons. For external icon libraries, use Icon instead."
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
            Name = "InvalidErrorMessage",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom validation error message for the invalid value."
        },
        new()
        {
            Name = "InvertMouseWheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Reverses the direction the mouse wheel moves the dial in."
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this TimePicker is open. Supports two-way binding.",
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
            Description = "The latest time that can be selected. Later hours, and the minutes past it inside its own hour, are dimmed on the dial and refused by the pointer, the keyboard and the text input.",
        },
        new()
        {
            Name = "MinTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The earliest time that can be selected. Earlier hours, and the minutes before it inside its own hour, are dimmed on the dial and refused by the pointer, the keyboard and the text input.",
        },
        new()
        {
            Name = "MinuteButtonTitle",
            Type = "string",
            DefaultValue = "Select minute",
            Description = "The title (and accessible name) of the button that switches the dial to the minutes."
        },
        new()
        {
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in minutes, the dial and the keyboard move the minute by. A step greater than 1 snaps the pick to the nearest multiple of it."
        },
        new()
        {
            Name = "NoMouseWheel",
            Type = "bool",
            DefaultValue = "false",
            Description = "Disables moving the dial with the mouse wheel entirely. By default the wheel moves it by one step while scrolled over the focused dial with the Shift key held down."
        },
        new()
        {
            Name = "NowButtonText",
            Type = "string",
            DefaultValue = "Now",
            Description = "The text of the button that sets the TimePicker to the current time."
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
            Description = "Callback for when the callout of the TimePicker closes.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "Callback for when the TimePicker input receives focus. Unlike OnFocusIn it does not bubble, so it is the one to use when only the input itself receiving focus is of interest.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "Callback for when focus moves into the TimePicker input or any of its descendants, since unlike OnFocus it bubbles.",
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
            Description = "Callback for when the callout of the TimePicker opens.",
        },
        new()
        {
            Name = "OnSelectTime",
            Type = "EventCallback<TimeSpan?>",
            Description = "Callback for when the selected time changes.",
        },
        new()
        {
            Name = "OnViewChange",
            Type = "EventCallback<BitCircularTimePickerView>",
            Description = "Callback for when the dial switches between the hours, the minutes and the seconds.",
            Href = "#view-enum",
            LinkType = LinkType.Link
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
            Name = "SecondButtonTitle",
            Type = "string",
            DefaultValue = "Select second",
            Description = "The title (and accessible name) of the button that switches the dial to the seconds."
        },
        new()
        {
            Name = "SecondStep",
            Type = "int",
            DefaultValue = "1",
            Description = "The step, in seconds, the dial and the keyboard move the second by. A step greater than 1 snaps the pick to the nearest multiple of it."
        },
        new()
        {
            Name = "ShowClearButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a button that clears the value of the TimePicker under the clock."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker's close button should be shown or not."
        },
        new()
        {
            Name = "ShowNowButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a button that sets the TimePicker to the current time under the clock, snapped to the steps and clamped into the selectable range."
        },
        new()
        {
            Name = "ShowSeconds",
            Type = "bool",
            DefaultValue = "false",
            Description = "Adds the seconds to the picker: a third ring the dial moves on to after the minute, a third part in the toolbar, and the seconds of the value kept instead of zeroed."
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the TimePicker.",
            Href = "#size-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker is rendered standalone or with the input component and callout.",
        },
        new()
        {
            Name = "StartView",
            Type = "BitCircularTimePickerView",
            DefaultValue = "BitCircularTimePickerView.Hour",
            Description = "The part of the time the clock starts on when the picker opens. The EditMode wins over it, and so does a Second start view on a picker that does not show the seconds.",
            Href = "#view-enum",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "Styles",
            Type = "BitCircularTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
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
            Description = @"The format of the time in the TimePicker like ""HH:mm"". Left unset it follows the TimeFormat and ShowSeconds.",
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
            Name = "View",
            Type = "BitCircularTimePickerView",
            Description = "The part of the time the clock is currently editing.",
            Href = "#view-enum",
            LinkType = LinkType.Link
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
            Description = "Closes the callout of the TimePicker."
        },
        new()
        {
            Name = "SwitchView",
            Type = "Task SwitchView(BitCircularTimePickerView view)",
            Description = "Switches the dial to the hours, the minutes or the seconds, as far as the EditMode and ShowSeconds allow it."
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
            Id = "edit-mode-enum",
            Name = "BitCircularTimePickerEditMode",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Normal",
                    Description = "Every part the picker shows can be edited, and settling one moves the dial on to the next.",
                    Value = "0",
                },
                new()
                {
                    Name = "OnlyMinutes",
                    Description = "Only the minute can be edited; the rest of the current value is kept as it is.",
                    Value = "1",
                },
                new()
                {
                    Name = "OnlyHours",
                    Description = "Only the hour can be edited; the rest of the current value is kept as it is.",
                    Value = "2",
                },
                new()
                {
                    Name = "OnlySeconds",
                    Description = "Only the second can be edited; the rest of the current value is kept as it is. The picker deals in seconds in this mode whether or not ShowSeconds is set.",
                    Value = "3",
                }
            ]
        },
        new()
        {
            Id = "view-enum",
            Name = "BitCircularTimePickerView",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Hour",
                    Description = "The dial selects the hour.",
                    Value = "0",
                },
                new()
                {
                    Name = "Minute",
                    Description = "The dial selects the minute.",
                    Value = "1",
                },
                new()
                {
                    Name = "Second",
                    Description = "The dial selects the second, which only a picker that shows the seconds ever moves on to.",
                    Value = "2",
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
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new() { Name = "Small", Description = "The small size.", Value = "0" },
                new() { Name = "Medium", Description = "The medium size.", Value = "1" },
                new() { Name = "Large", Description = "The large size.", Value = "2" }
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "",
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
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "timepicker-class-styles",
            Title = "BitCircularTimePickerClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Focused",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focused state of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the Label of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "InputWrapper",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input wrapper of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "InputContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input container of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Input",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the input of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Icon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the icon of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Callout",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "CalloutContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the callout container of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "Toolbar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the toolbar of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "HourMinuteContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour and minute container of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "HourButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "MinuteButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the minute button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "SecondButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the second button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "HourMinuteSeparator",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour minute separator of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "HourMinuteText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hour/minute text rendered in the single-part edit modes of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "AmPmContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the AM/PM container of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "AmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the AM button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "PmButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the PM button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "SelectedButtons",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the selected buttons of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock container of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockFace",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock face of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockPin",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock pin of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock number of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockSelectedNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock selected number of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockDisabledNumber",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for a clock number that cannot be selected, because of the time bounds, the steps or the allowed-value predicates."
                },
                new()
                {
                    Name = "ClockPointer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock pointer of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockPointerThumb",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock pointer thumb of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "ClockPointerThumbMinute",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the clock pointer thumb of the BitCircularTimePicker when it does not rest on a number of the dial - between two marks, or on a part of the time that has not been set yet."
                },
                new()
                {
                    Name = "Actions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the row holding the \"now\" and \"clear\" buttons of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "NowButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button that sets the BitCircularTimePicker to the current time."
                },
                new()
                {
                    Name = "ClearButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the button that clears the value of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "CloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the BitCircularTimePicker."
                },
                new()
                {
                    Name = "CloseButtonIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button icon of the BitCircularTimePicker."
                }
            ]
        }
    ];



    private TimeSpan? selectedTime = new(5, 12, 0);
    private TimeSpan? changedTime;
    private TimeSpan? secondsTime = new(14, 5, 30);
    private TimeSpan? readOnlyTime = new(2, 50, 0);
    private TimeSpan? classesValue;
    private bool isCalloutOpen;
    private BitCircularTimePickerView? changedView;
    private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitCircularTimePicker circularTimePicker = default!;
    private readonly List<string> eventLogs = [];

    private void LogOpen() => Log("OnOpen");
    private void LogClose() => Log("OnClose");
    private void LogClick() => Log("OnClick");
    private void LogFocusIn() => Log("OnFocusIn");
    private void LogFocusOut() => Log("OnFocusOut");
    private void LogViewChange(BitCircularTimePickerView view) => Log($"OnViewChange: {view}");
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

    // The short time pattern of a culture spells the hour with an "h" where its readers expect a 12-hour clock
    // and with an "H" where they expect a 24-hour one.
    private static BitTimeFormat GetTimeFormatOf(CultureInfo culture)
    {
        return culture.DateTimeFormat.ShortTimePattern.Contains('h')
            ? BitTimeFormat.TwelveHours
            : BitTimeFormat.TwentyFourHours;
    }

    private async Task OpenCallout()
    {
        await circularTimePicker.OpenCallout();
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
}
