namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TimePickers.CircularTimePicker;

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
            Name = "IconName",
            Type = "string",
            DefaultValue = "Clock",
            Description = "Optional TimePicker icon."
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
            Name = "IsResponsive",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the responsive mode in small screens.",
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
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the TimePicker's close button should be shown or not."
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
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "The time format of the time-picker, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
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



    private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);
    private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitCircularTimePicker circularTimePicker = default!;

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


    private readonly string example1RazorCode = @"
<BitCircularTimePicker Label=""Basic CircularTimePicker"" />
<BitCircularTimePicker Label=""Disabled"" IsEnabled=""false"" />
<BitCircularTimePicker Label=""PlaceHolder"" Placeholder=""Select a time"" />
<BitCircularTimePicker Label=""TimeFormat (AM/PM)"" TimeFormat=""BitTimeFormat.TwelveHours"" />
<BitCircularTimePicker Label=""Custom icon"" IconName=""@BitIconName.Airplane"" />";

    private readonly string example2RazorCode = @"
<BitCircularTimePicker Label=""Text input allowed""
                       AllowTextInput=""true""
                       ValueFormat=""hh:mm""
                       Placeholder=""Enter a time (hh:mm)"" />";

    private readonly string example3RazorCode = @"
<BitCircularTimePicker Label=""Formatted time""
                       ValueFormat=""hh-mm.ss""
                       Placeholder=""Select a time"" />";

    private readonly string example4RazorCode = @"
<BitCircularTimePicker @bind-Value=""@selectedTime"" />
<div>Selected time: @selectedTime.ToString()</div>";
    private readonly string example4CsharpCode = @"
private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);";

    private readonly string example5RazorCode = @"
<BitCircularTimePicker Label=""fa-IR culture""
                       TimeFormat=""BitTimeFormat.TwelveHours""
                       Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />";

    private readonly string example6RazorCode = @"
<BitCircularTimePicker @ref=""circularTimePicker"">
    <LabelTemplate>
        Custom label <BitIconButton IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitIconButton>
    </LabelTemplate>
</BitCircularTimePicker>

<BitCircularTimePicker Label=""Custom left-handed icon""
                       IconLocation=""BitIconLocation.Left""
                       Placeholder=""Select a time"">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitCircularTimePicker>";
    private readonly string example6CsharpCode = @"
private BitCircularTimePicker circularTimePicker;

private async Task OpenCallout()
{
    await circularTimePicker.OpenCallout();
}";

    private readonly string example7RazorCode = @"
<BitCircularTimePicker Label=""Response CircularTimePicker""
                       IsResponsive=""true""
                       Placeholder=""Select a time"" />";

    private readonly string example8RazorCode = @"
<EditForm Model=""formValidationCircularTimePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitCircularTimePicker @bind-Value=""formValidationCircularTimePickerModel.Time""
                                AllowTextInput=""true""
                                Placeholder=""Select a time""
                                Label=""Time required"" />
        <ValidationMessage For=""@(() => formValidationCircularTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>

@if (string.IsNullOrEmpty(successMessage) is false)
{
    <BitMessage Severity=""BitSeverity.Success"">@successMessage</BitMessage>
}



<EditForm Model=""formValidationCircularTimePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitCircularTimePicker @bind-Value=""formValidationCircularTimePickerModel.Time""
                        AllowTextInput=""true""
                        Label=""Custom Invalid Error Message""
                        InvalidErrorMessage=""Invalid Time!!!"" />
        <ValidationMessage For=""@(() => formValidationCircularTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class FormValidationCircularTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCircularTimePickerModel formValidationCircularTimePickerModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(3000);

    formValidationCircularTimePickerModel = new();

    StateHasChanged();
}";

    private readonly string example9RazorCode = @"
<BitCircularTimePicker Dir=""BitDir.Rtl"" />";
}
