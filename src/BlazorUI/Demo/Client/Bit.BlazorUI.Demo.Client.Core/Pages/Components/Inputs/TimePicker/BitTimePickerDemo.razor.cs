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
            Name = "ClearButtonText",
            Type = "string",
            DefaultValue = "Clear",
            Description = "The text of the clear button, shown when ShowClearButton is set.",
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
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the TimePicker."
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
            Name = "DropDirection",
            Type = "BitDropDirection",
            DefaultValue = "BitDropDirection.TopAndBottom",
            Description = "Determines the allowed drop directions of the callout.",
            Href = "#drop-direction-enum",
            LinkType = LinkType.Link
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
            Description = "Determines increment/decrement steps for time-picker's hour.",
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
            Description = "Determines increment/decrement steps for time-picker's minute.",
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
            Description = "Determines increment/decrement steps for time-picker's second.",
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
            Description = @"The format of the time in the TimePicker like ""HH:mm"".",
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



    private bool isOpen;
    private readonly List<string> eventLogs = [];
    private TimeSpan? boundTime;
    private TimeSpan? actionsTime;
    private TimeSpan? classesValue;
    private TimeSpan? readOnlyTime = new(2, 50, 0);
    private TimeSpan? selectedTime = new(5, 12, 15);
    private FormValidationTimePickerModel formValidationTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitTimePicker timePicker = default!;

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
