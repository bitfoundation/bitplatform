using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DatePicker;

public partial class BitDatePickerDemo
{
    private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);
    private FormValidationDatePickerModel formValidationDatePickerModel = new();
    private string SuccessMessage = string.Empty;

    private async void HandleValidSubmit()
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
        new ComponentParameter()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the DatePicker."
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
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<DateTimeOffset?>",
            DefaultValue = "",
            Description = "Callback for when the on date value changed.",
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "Select a date...",
            Description = "Placeholder text for the DatePicker.",
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
            Name = "FormatDate",
            Type = "string",
            DefaultValue = "",
            Description = @"Date format like ""yyyy/MM/dd"".",
        },
        new ComponentParameter()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "",
            Description = "Show week number in the year.",
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
<BitDatePicker Style=""width: 300px""
               ShowMonthPickerAsOverlay=""true"">
</BitDatePicker>";

    private readonly string example2HTMLCode = @"
<BitDatePicker Style=""width: 300px""
               ShowWeekNumbers=""true"">
</BitDatePicker>";

    private readonly string example3HTMLCode = @"
<BitDatePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
               GoToToday=""برو به امروز""
               Style=""width: 300px"">
</BitDatePicker>";

    private readonly string example4HTMLCode = @"
<BitDatePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
               GoToToday=""Boro be emrouz""
               Style=""width: 300px"">
</BitDatePicker>";

    private readonly string example5HTMLCode = @"
<BitDatePicker @bind-Value=""@selectedDate"" Style=""width: 300px""></BitDatePicker>
<BitLabel>this is selected date: @selectedDate.ToString()</BitLabel>";

    private readonly string example5CSharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);";

    private readonly string example6HTMLCode = @"<BitDatePicker FormatDate=""d"" Style=""width: 300px""></BitDatePicker>";

    private readonly string example7HTMLCode = @"
<BitDatePicker Style=""width: 300px""
               MaxDate=""DateTimeOffset.Now.AddYears(1)"" 
               MinDate=""DateTimeOffset.Now.AddYears(-5)"">
</BitDatePicker>";

    private readonly string example8HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""formValidationDatePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitDatePicker Style=""width: 300px"" @bind-Value=""formValidationDatePickerModel.Date"" 
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

    private readonly string example8CSharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationDatePickerModel formValidationDatePickerModel = new();
private string SuccessMessage = string.Empty;

private async void HandleValidSubmit()
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
}
