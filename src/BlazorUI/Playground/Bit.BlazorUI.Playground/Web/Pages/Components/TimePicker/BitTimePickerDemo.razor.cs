using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.TimePicker;

public partial class BitTimePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {

        new()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Label for the TimePicker.",
        },
        new()
        {
            Name = "LabelFragment",
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
            Name = "TimeEditMode",
            Type = "BitTimeEditMode",
            LinkType = LinkType.Link,
            Href = "#edit-mode-enum",
            DefaultValue = "BitTimeEditMode.Normal",
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
            Type = "string",
            DefaultValue = "",
            Description = "Placeholder text for the DatePicker.",
        },
        new()
        {
            Name = "IconFragment",
            Type = "RenderFragment?",
            DefaultValue = "",
            Description = "Custom TimePicker icon template."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
            DefaultValue = "BitIconLocation.Left",
            Description = "TimePicker icon location."
        },
        new()
        {
            Name = "IconName",
            Type = "BitIconName",
            DefaultValue = "BitIconName.Clock",
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
            DefaultValue = "",
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
            DefaultValue = "",
            Description = "Callback for when clicking on TimePicker input.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when focus moves into the TimePicker input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Callback for when focus moves into the TimePicker input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            DefaultValue = "",
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
            DefaultValue = "false",
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
            Type = "string",
            DefaultValue = "",
            Description = @"The format of the time in the TimePicker like ""HH:mm"".",
        },
        new()
        {
            Name = "InvalidErrorMessage",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The custom validation error message for the invalid value."
        },
        new()
        {
            Name = "OnSelectTime",
            Type = "EventCallback<TimeSpan?>",
            DefaultValue = "",
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
            Type = "TimeSpan",
            DefaultValue = "",
            Description = "The value of TimePicker.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<TimeSpan?>",
            DefaultValue = "",
            Description = "Callback for when the on time value changed.",
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
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
            Title = "BitIconLocation Enum",
            Description = "",
            EnumList = new List<EnumItem>()
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
            Title = "BitTimeEditMode Enum",
            Description = "",
            EnumList = new List<EnumItem>()
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
    private FormValidationTimePickerModel formValidationTimePickerModel = new();
    private string successMessage = string.Empty;
    private BitTimePicker timePicker;

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

    private readonly string example1HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example2HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time...""
               AmPm=""true"" />";

    private readonly string example3HTMLCode = @"
<BitTimePicker IsEnabled=false
               Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example4HTMLCode = @"
<BitTimePicker IsEnabled=false
               Style=""max-width: 300px""
               Label=""Start time""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";

    private readonly string example5HTMLCode = @"
<EditForm Model=""formValidationTimePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitTimePicker @bind-Value=""formValidationTimePickerModel.Time""
                        AllowTextInput=""true""
                        Style=""max-width: 300px""
                        AriaLabel=""Select a time""
                        Placeholder=""Select a time...""
                        Label=""Time required"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
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

    private readonly string example5CSharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationTimePickerModel formValidationTimePickerModel = new();
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

    private readonly string example6HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AllowTextInput=true
               Label=""Start time""
               AriaLabel=""Select a time"" />";

    private readonly string example7HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time.""
               Placeholder=""Select a time...""
               TimeFormat=""hh:MM:ss"" />";

    private readonly string example8HTMLCode = @"
<BitTimePicker @ref=""timePicker""
               Style=""max-width: 300px""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."">
    <LabelFragment>
        Custom label <BitIconButton IconName=""BitIconName.AlarmClock"" OnClick=""OpenCallout""></BitIconButton>
    </LabelFragment>
</BitTimePicker>";

    private readonly string example8CSharpCode = @"
private BitTimePicker timePicker;

private async Task OpenCallout()
{
    await timePicker.OpenCallout();
}";

    private readonly string example9HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               @bind-Value=""@selectedTime""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />
<BitLabel>Selected time: @selectedTime.ToString()</BitLabel>";

    private readonly string example9CSharpCode = @"
private TimeSpan? selectedTime = new TimeSpan(5, 12, 15);";

    private readonly string example10HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               IconLocation=""BitIconLocation.Left""
               Placeholder=""Select a time..."">
    <IconFragment>
        <img src=""https://img.icons8.com/fluency/2x/clock.png"" width=""24"" height=""24"" />
    </IconFragment>
</BitTimePicker>";

    private readonly string example11HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               AriaLabel=""Select a time""
               IconName=""BitIconName.Airplane""
               Placeholder=""Select a time..."" />";

    private readonly string example12HTMLCode = @"
<EditForm Model=""formValidationTimePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitTimePicker @bind-Value=""formValidationTimePickerModel.Time""
                        Style=""max-width: 350px""
                        AllowTextInput=""true""
                        Label=""BitTimePicker with Custom Invalid Error Message""
                        InvalidErrorMessage=""Invalid Time!!!"" />
        <ValidationMessage For=""@(() => formValidationTimePickerModel.Time)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";

    private readonly string example12CSharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationTimePickerModel formValidationTimePickerModel = new();";

    private readonly string example13HTMLCode = @"
<BitTimePicker Style=""max-width: 300px""
               IsResponsive=""true""
               AriaLabel=""Select a time""
               Placeholder=""Select a time..."" />";
}
