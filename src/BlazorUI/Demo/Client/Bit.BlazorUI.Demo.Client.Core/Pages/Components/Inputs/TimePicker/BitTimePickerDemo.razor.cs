namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePicker;

public partial class BitTimePickerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker allows input a time string directly or not.",
        },
        new()
        {
            Name = "TabIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The tabIndex of the TextField.",
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
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom TimePicker icon template."
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
            Type = "string",
            DefaultValue = "Clock",
            Description = "Optional TimePicker icon."
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
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<String, Object>()",
            Description = "Capture and render additional attributes in addition to the main callout's parameters."
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
            Name = "IsResponsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode in small screens.",
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
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Text field of the TimePicker is underlined.",
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
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the TimePicker."
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
            Name = "ValueFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = @"The format of the time in the TimePicker like ""HH:mm"".",
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
            Name = "OnSelectTime",
            Type = "EventCallback<TimeSpan?>",
            Description = "Callback for when the on selected time changed.",
        },
        new()
        {
            Name = "Value",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "The value of TimePicker.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<TimeSpan?>",
            Description = "Callback for when the on time value changed.",
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
            Name = "MinuteStep",
            Type = "int",
            DefaultValue = "1",
            Description = "Determines increment/decrement steps for time-picker's minute.",
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
            Name = "Classes",
            Type = "BitTimePickerClassStyles",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the TimePicker.",
            Href = "#timepicker-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "CloseButtonTitle",
            Type = "string",
            DefaultValue = "Close time picker",
            Description = "The title of the close button (tooltip).",
        },
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
            Name = "BitTimePickerEditMode",
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
                },
                new()
                {
                    Name = "ShowCloseButton",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Whether the BitTimePicker's close button should be shown or not."
                }
            ]
        }
    ];



    private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);
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



    private readonly string example1RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example2RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time...""
               TimeFormat=""BitTimeFormat.TwelveHours"" />";

    private readonly string example3RazorCode = @"
<BitTimePicker IsEnabled=""false""
               Style=""max-width: 175px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example4RazorCode = @"
<BitTimePicker IsEnabled=""false""
               Style=""max-width: 175px""
               Label=""Start time""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        box-shadow: aqua 0 0 1rem;
    }

    .custom-root {
        margin: 1rem;
        border-radius: 0.5rem;
        background-color: #211e1b;
    }

    .custom-inc-h, .custom-inc-m {
        background-color: red;
    }

    .custom-dec-h,
    .custom-dec-m {
        background-color: blue;
    }
</style>
<BitTimePicker Style=""padding: 1rem; background: purple;"" />

<BitTimePicker Class=""custom-class"" />

<BitTimePicker Styles=""@(new() { Root = ""margin: 1rem; border: 1px solid gold;"",
                                 HourMinuteSeparator = ""color: green;"",
                                 IncreaseHourButton = ""color: red;"",
                                 IncreaseMinuteButton = ""color: red;"",
                                 DecreaseHourButton = ""color: blue;"",
                                 DecreaseMinuteButton = ""color: blue;"",
                                 HourInput = ""color: purple;"",
                                 MinuteInput = ""color: purple;"" })"" />

<BitTimePicker Classes=""@(new() { Root = ""custom-root"",
                                  IncreaseHourButton = ""custom-inc-h"",
                                  IncreaseMinuteButton = ""custom-inc-m"",
                                  DecreaseHourButton = ""custom-dec-h"",
                                  DecreaseMinuteButton = ""custom-dec-m"" })"" />
";

    private readonly string example6RazorCode = @"
<EditForm Model=""formValidationTimePickerModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitTimePicker @bind-Value=""formValidationTimePickerModel.Time""
                        AllowTextInput=""true""
                        Style=""max-width: 175px""
                        AriaLabel=""Select a time""
                        Placeholder=""Select a time...""
                        Label=""Time required"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>";
    private readonly string example6CsharpCode = @"
public class FormValidationTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCircularTimePickerModel formValidationTimePickerModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(3000);

    formValidationTimePickerModel = new();

    StateHasChanged();
}";

    private readonly string example7RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AllowTextInput=""true""
               Label=""Start time""
               AriaLabel=""Select a time"" />";

    private readonly string example8RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AriaLabel=""Select a time.""
               Placeholder=""Select a time...""
               ValueFormat=""hh-mm.ss"" />";

    private readonly string example9RazorCode = @"
<BitTimePicker @ref=""timePicker""
               Style=""max-width: 175px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."">
   <LabelTemplate>
       Custom label <BitButton Variant=""BitVariant.Text"" IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitButton>
   </LabelTemplate>
</BitTimePicker>";
    private readonly string example9CsharpCode = @"
private BitTimePicker timePicker;

private async Task OpenCallout()
{
    await timePicker.OpenCallout();
}";

    private readonly string example10RazorCode = @"
<BitTimePicker @bind-Value=""@selectedTime""
               Style=""max-width: 175px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />
<BitLabel>Selected time: @selectedTime.ToString()</BitLabel>";
    private readonly string example10CsharpCode = @"
private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);";

    private readonly string example11RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AriaLabel=""Select a time""
               IconLocation=""BitIconLocation.Left""
                laceholder=""Select a time..."">
   <IconTemplate>
       <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
   </IconTemplate>
</BitTimePicker>";

    private readonly string example12RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               AriaLabel=""Select a time""
               IconName=""@BitIconName.Airplane""
               Placeholder=""Select a time..."" />";

    private readonly string example13RazorCode = @"
<EditForm Model=""formValidationTimePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitTimePicker @bind-Value=""formValidationTimePickerModel.Time""
                        Style=""max-width: 175px""
                        AllowTextInput=""true""
                        InvalidErrorMessage=""Invalid Time!!!"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class FormValidationTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationTimePickerModel formValidationTimePickerModel = new();";

    private readonly string example14RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               IsResponsive=""true""
               ShowCloseButton=""true""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example15RazorCode = @"
<BitTimePicker Style=""max-width: 175px""
               Label=""HourStep = 2""
               HourStep=""2""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />

<BitTimePicker Style=""max-width: 175px""
               Label=""MinuteStep = 15""
               MinuteStep=""15""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";
}
