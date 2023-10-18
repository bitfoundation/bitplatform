namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.CirculateTimePicker;

public partial class BitCirculateTimePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
            Name = "AmPm",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, sets 12 hour selection clock."
        },
        new()
        {
            Name = "EditMode",
            Type = "BitCirculateTimePickerEditMode",
            LinkType = LinkType.Link,
            Href = "#edit-mode-enum",
            DefaultValue = "BitCirculateTimePickerEditMode.Normal",
            Description = "Choose the edition mode. By default, you can edit hours and minutes."
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
            Name = "PickerAriaLabel",
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
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "false",
            Description = "If AutoClose is set to true and PickerActions are defined, the hour and the minutes can be defined without any action."
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
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "component-visibility-enum",
            Name = "BitVisibility",
            Description = "",
            Items = new()
            {
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
            }
        },
        new()
        {
            Id = "icon-location-enum",
            Name = "BitIconLocation",
            Description = "",
            Items = new()
            {
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
            }
        },
        new()
        {
            Id = "edit-mode-enum",
            Name = "BitCirculateTimePickerEditMode",
            Description = "",
            Items = new()
            {
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
            }
        }
    };



    private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);
    private FormValidationCirculateTimePickerModel formValidationCirculateTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitCirculateTimePicker circulateTimePicker;

    private async Task OpenCallout()
    {
        await circulateTimePicker.OpenCallout();
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
<BitCirculateTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example2RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time...""
               AmPm=""true"" />";

    private readonly string example3RazorCode = @"
<BitCirculateTimePicker IsEnabled=false
               Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example4RazorCode = @"
<BitCirculateTimePicker IsEnabled=false
               Style=""max-width: 300px""
               Label=""Start time""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example5RazorCode = @"
<EditForm Model=""formValidationCirculateTimePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitCirculateTimePicker @bind-Value=""formValidationCirculateTimePickerModel.Time""
                        AllowTextInput=""true""
                        Style=""max-width: 300px""
                        AriaLabel=""Select a time""
                        Placeholder=""Select a time...""
                        Label=""Time required"" />
        <ValidationMessage For=""@(() => formValidationCirculateTimePickerModel.Time)"" />
    </div>
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">
        Submit
    </BitButton>
</EditForm>

@if (string.IsNullOrEmpty(successMessage) is false) {
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @successMessage
    </BitMessageBar>
}";
    private readonly string example5CsharpCode = @"
public class FormValidationCirculateTimePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCirculateTimePickerModel formValidationCirculateTimePickerModel = new();
private string successMessage = string.Empty;

private async Task HandleValidSubmit()
{
    successMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    successMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    successMessage = string.Empty;
}";

    private readonly string example6RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               AllowTextInput=true
               Label=""Start time""
               AriaLabel=""Select a time"" />";

    private readonly string example7RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time.""
               Placeholder=""Select a time...""
               TimeFormat=""hh:MM:ss"" />";

    private readonly string example8RazorCode = @"
<BitCirculateTimePicker @ref=""timePicker""
               Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."">
    <LabelTemplate>
        Custom label <BitIconButton IconName=""@BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitIconButton>
    </LabelTemplate>
</BitCirculateTimePicker>";
    private readonly string example8CsharpCode = @"
private BitCirculateTimePicker circulateTimePicker;

private async Task OpenCallout()
{
    await circulateTimePicker.OpenCallout();
}";

    private readonly string example9RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               @bind-Value=""@selectedTime""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />
<BitLabel>Selected time: @selectedTime.ToString()</BitLabel>";
    private readonly string example9CsharpCode = @"
private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);";

    private readonly string example10RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               IconLocation=""BitIconLocation.Left""
               Placeholder=""Select a time..."">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitCirculateTimePicker>";

    private readonly string example11RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               IconName=""@BitIconName.Airplane""
               Placeholder=""Select a time..."" />";

    private readonly string example12RazorCode = @"
<EditForm Model=""formValidationCirculateTimePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitCirculateTimePicker @bind-Value=""formValidationCirculateTimePickerModel.Time""
                        Style=""max-width: 350px""
                        AllowTextInput=""true""
                        Label=""BitCirculateTimePicker with Custom Invalid Error Message""
                        InvalidErrorMessage=""Invalid Time!!!"" />
        <ValidationMessage For=""@(() => formValidationCirculateTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example12CsharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationTimePickerModel formValidationTimePickerModel = new();";

    private readonly string example13RazorCode = @"
<BitCirculateTimePicker Style=""max-width: 300px""
               IsResponsive=""true""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";
}
