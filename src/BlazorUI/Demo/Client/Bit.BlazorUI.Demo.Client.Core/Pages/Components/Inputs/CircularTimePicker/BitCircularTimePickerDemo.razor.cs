namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.CircularTimePicker;

public partial class BitCircularTimePickerDemo
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
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "If AutoClose is set to true and PickerActions are defined, the hour and the minutes can be defined without any action."
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
            Type = "BitCircularTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
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
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the TimePicker."
        },
        new()
        {
            Name = "EditMode",
            Type = "BitCircularTimePickerEditMode",
            LinkType = LinkType.Link,
            Href = "#edit-mode-enum",
            DefaultValue = "BitCircularTimePickerEditMode.Normal",
            Description = "Choose the edition mode. By default, you can edit hours and minutes."
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
            Name = "IconLocation",
            Type = "BitIconLocation",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
            DefaultValue = "BitIconLocation.Right",
            Description = "TimePicker icon location."
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
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this TimePicker is open.",
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
            Name = "OnClick",
            Type = "EventCallback",
            Description = "Callback for when clicking on TimePicker input.",
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
            Description = "Callback for when clicking on TimePicker input.",
        },
        new()
        {
            Name = "OnSelectTime",
            Type = "EventCallback<TimeSpan?>",
            Description = "Callback for when the on selected time changed.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string?",
            DefaultValue = "null",
            Description = "Placeholder text for the DatePicker.",
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
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker's close button should be shown or not."
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
            Name = "Standalone",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker is rendered standalone or with the input component and callout.",
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
                    Description = "Can edit hours and minutes.",
                    Value = "0",
                },
                new()
                {
                    Name = "OnlyMinutes",
                    Description = "Can edit only minutes.",
                    Value = "1",
                },
                new()
                {
                    Name = "OnlyHours",
                    Description = "Can edit only hours.",
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
                    Description = "Custom CSS classes/styles for the hour/minute text of the BitCircularTimePicker."
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
                    Description = "Custom CSS classes/styles for the close button of the BitCircularTimePicker."
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



    private TimeSpan? selectedTime = new(5, 12, 15);
    private TimeSpan? readOnlyTime = new(2, 50, 0);
    private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitCircularTimePicker circularTimePicker = default!;

    private TimeSpan? classesValue;

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
