using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DatePicker;

public partial class BitDatePickerDemo
{
    private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);
    private FormValidationDatePickerModel formValidationDatePickerModel = new();
    private string SuccessMessage = string.Empty;
    private CultureInfo Culture = CultureInfo.CurrentUICulture;

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

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DatePicker allows input a date string directly or not.",
        },
        new ComponentParameter
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "",
            Description = "Capture and render additional attributes in addition to the main callout's parameters."
        },
        new ComponentParameter()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the DatePicker."
        },
        new ComponentParameter()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "",
            Description = "Used to customize how content inside the day cell is rendered."
        },
        new ComponentParameter()
        {
            Name = "FormatDate",
            Type = "string",
            DefaultValue = "",
            Description = @"Date format like ""yyyy/MM/dd"".",
        },
        new ComponentParameter()
        {
            Name = "GoToToday",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "GoToToday text for the DatePicker.",
        },
        new ComponentParameter()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the DatePicker has a border.",
        },
        new ComponentParameter
        {
            Name = "HighlightCurrentMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the current month."
        },
        new ComponentParameter
        {
            Name = "HighlightSelectedMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the selected month."
        },
        new ComponentParameter()
        {
            Name = "IsMonthPickerVisible",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker is shown beside the day picker or hidden.",
        },
        new ComponentParameter()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this DatePicker is open.",
        },
        new ComponentParameter()
        {
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Textfield of the DatePicker is underlined.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Label for the DatePicker.",
        },
        new ComponentParameter
        {
            Name = "LabelFragment",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the DatePicker."
        },
        new ComponentParameter()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "",
            Description = "The maximum allowable date.",
        },
        new ComponentParameter()
        {
            Name = "MinDate",
            Type = "DateTimeOffset",
            DefaultValue = "",
            Description = "The minimum allowable date.",
        },
        new ComponentParameter()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "",
            Description = "Used to customize how content inside the month cell is rendered."
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when clicking on DatePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the DatePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when clicking on DatePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            DefaultValue = "",
            Description = "Callback for when the on selected date changed.",
        },
        new ComponentParameter
        {
            Name = "PickerAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label for date picker popup for screen reader users."
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "",
            Description = "Placeholder text for the DatePicker.",
        },
        new ComponentParameter
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "",
            Description = "Whether the CalendarDay close button should be shown or not."
        },
        new ComponentParameter
        {
            Name = "ShowGoToToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the \"Go to today\" link should be shown or not."
        },
        new ComponentParameter()
        {
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date picker when visible.",
        },
        new ComponentParameter()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "",
            Description = "Show week number in the year.",
        },
        new ComponentParameter()
        {
            Name = "TabIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The tabIndex of the TextField.",
        },
        new ComponentParameter()
        {
            Name = "Value",
            Type = "DateTimeOffset",
            DefaultValue = "",
            Description = "The value of DatePicker.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<DateTimeOffset?>",
            DefaultValue = "",
            Description = "Callback for when the on date value changed.",
        },
        new ComponentParameter()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "",
            Description = "Used to customize how content inside the year cell is rendered."
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    private readonly string example1HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example2HTMLCode = @"
<BitDatePicker IsEnabled=false
               Style=""max-width: 300px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />
<BitDatePicker IsEnabled=false
               Style=""max-width: 300px""
               Label=""Disabled (with label)""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example3HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               ShowWeekNumbers=true
               ShowMonthPickerAsOverlay=true
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example4HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""formValidationDatePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitDatePicker Style=""max-width: 300px"" 
                           @bind-Value=""formValidationDatePickerModel.Date"" 
                           AriaLabel=""Select a date""
                           Placeholder=""Select a date...""
                           Label=""Date required (with label)"" />

            <ValidationMessage For=""@(() => formValidationDatePickerModel.Date)"" />
        </div>
        <br />
        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

    private readonly string example4CSharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationDatePickerModel formValidationDatePickerModel = new();
private string SuccessMessage = string.Empty;

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}";

    private readonly string example5HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               AllowTextInput=true
               Label=""Start date""
               AriaLabel=""Select a date"" />";

    private readonly string example6HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               AriaLabel=""Select a date. Date format is month slash month slash year.""
               Placeholder=""Select a date...""
               FormatDate=""dd/MM/yy"" />";

    private readonly string example7HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date...""
               MaxDate=""DateTimeOffset.Now.AddYears(1)""
               MinDate=""DateTimeOffset.Now.AddYears(-5)"" />";

    private readonly string example8HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."">
    <LabelFragment>
        Custom label <BitIconButton IconName=""BitIconName.Calendar""></BitIconButton>
    </LabelFragment>
</BitDatePicker>";

    private readonly string example9HTMLCode = @"
<BitDatePicker Style=""max-width: 300px""
               @bind-Value=""@selectedDate"" 
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />
<BitLabel>Selected date: @selectedDate.ToString()</BitLabel>";

    private readonly string example9CSharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);";

    private readonly string example10HTMLCode = @"
<BitDatePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
               GoToToday=""برو به امروز""
               Style=""max-width: 300px"">
</BitDatePicker>";

    private readonly string example11HTMLCode = @"
<BitDatePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
               GoToToday=""Boro be emrouz""
               Style=""max-width: 300px"">
</BitDatePicker>";

    private readonly string example12HTMLCode = @"
<style>
.weekend-cell {
    color: red;
}
</style>

<BitDatePicker Style=""max-width: 300px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."">
    <DayCellTemplate>
        <span class=""@(context.DayOfWeek == DayOfWeek.Sunday ? ""weekend-cell"" : null)"">
            @context.Day
        </span>
    </DayCellTemplate>
</BitDatePicker>";

    private readonly string example13HTMLCode = @"
<style>
.custom-day-cell {
    position: relative;
    width: 44px !important;
    height: 44px !important;
}

.discount-badge {
    position: absolute;
    top: 0;
    right: 0;
    display: inline-flex;
    align-items: center;
    width: fit-content !important;
    height: 16px !important;
    border-radius: 2px;
    padding: 0 4px;
    background-color: red;
    color: white;
    font-size: 8px;
}

.year-suffix {
    position: absolute;
    bottom: 10px;
    right: -12px;
    height: 12px;
    color: gray;
    font-size: 8px;
}

.date-picker-wrapper {
    ::deep .bit-dtp {
        &.bit-dtp-fluent {
            .day-picker-wrapper {
                .week-day-label {
                    width: 44px;
                }
            }
        }
    }
}
</style>

<div class=""date-picker-wrapper"">
    <BitDatePicker Style=""max-width: 300px""
                   AriaLabel=""Select a date""
                   Placeholder=""Select a date..."">
        <DayCellTemplate>
            <span class=""custom-day-cell"">
                @context.Day

                @if (context.Day % 5 is 0)
                {
                    <span class=""discount-badge"">off</span>
                }
            </span>
        </DayCellTemplate>
        <MonthCellTemplate>
            <span>
                @this.Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)

                @if (context.Month == 1)
                {
                    <span class=""discount-badge"">Xmas</span>
                }
            </span>
        </MonthCellTemplate>
        <YearCellTemplate>
            <span style=""position: relative"">
                @context
                <span class=""year-suffix"">AC</span>
            </span>
        </YearCellTemplate>
    </BitDatePicker>
</div>";

    private readonly string example13CSharpCode = @"
private CultureInfo Culture = CultureInfo.CurrentUICulture;";
}
