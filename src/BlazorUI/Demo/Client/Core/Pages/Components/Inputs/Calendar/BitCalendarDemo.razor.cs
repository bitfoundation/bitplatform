namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public partial class BitCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the Calendar."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = @"The format of the date in the Calendar like ""yyyy/MM/dd"".",
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the day cell is rendered."
        },
        new()
        {
            Name = "GoToToday",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "GoToToday text for the Calendar.",
        },
        new()
        {
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button.",
        },
        new()
        {
            Name = "GoToNextMonthTitle",
            Type = "string",
            DefaultValue = "Go to next month",
            Description = "The title of the Go to next month button.",
        },
        new()
        {
            Name = "HighlightCurrentMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the current month."
        },
        new()
        {
            Name = "HighlightSelectedMonth",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the month picker should highlight the selected month."
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
            Name = "IsMonthPickerVisible",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the month picker is shown beside the day picker or hidden.",
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "null",
            Description = "The maximum allowable date.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum allowable date.",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the month cell is rendered."
        },
        new()
        {
            Name = "MonthPickerPosition",
            Type = "BitCalendarMonthPickerPosition",
            DefaultValue = "BitCalendarMonthPickerPosition.Besides",
            LinkType = LinkType.Link,
            Href ="#month-position-enum",
            Description = "Used to set month picker position.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "Callback for when the on selected date changed.",
        },
        new()
        {
            Name = "ShowGoToToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the \"Go to today\" link should be shown or not."
        },
        new()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show week number in the year.",
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
            Name = "Value",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The value of Calendar.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "Callback for when the on date value changed.",
        },
        new()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the year cell is rendered."
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show time picker for select times.",
        }
    };
    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "month-position-enum",
            Name = "BitCalendarMonthPickerPosition",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name = "Beside",
                    Description = "Show the month picker at the beside.",
                    Value = "0",
                },
                new()
                {
                    Name = "Overlay",
                    Description = "Show the month picker as overlay.",
                    Value = "1",
                }
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitCalendar AriaLabel=""Select a date"" />";

    private readonly string example2RazorCode = @"
<BitCalendar IsEnabled=false AriaLabel=""Select a date"" />";

    private readonly string example3RazorCode = @"
<BitCalendar ShowWeekNumbers=true AriaLabel=""Select a date"" />";

    private readonly string example4RazorCode = @"
<EditForm Model=""formValidationCalendarModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <ValidationSummary />
    
    <BitCalendar @bind-Value=""formValidationCalendarModel.Date"" AriaLabel=""Select a date"" />
    <ValidationMessage For=""@(() => formValidationCalendarModel.Date)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset""
               ButtonStyle=""BitButtonStyle.Standard""
               OnClick=""() => { formValidationCalendarModel = new(); SuccessMessage=string.Empty; }"">
        Reset
    </BitButton>
</EditForm>

@if (string.IsNullOrEmpty(SuccessMessage) is false)
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";
    private readonly string example4CsharpCode = @"
public class FormValidationCalendarModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationCalendarModel formValidationCalendarModel = new();
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

    private readonly string example5RazorCode = @"
<BitCalendar @bind-Value=""@formattedDateTime"" AriaLabel=""Select a date."" DateFormat=""dd=MM(yy)"" />
<BitLabel>Selected DateTime: @formattedDateTime.ToString()</BitLabel>";

    private readonly string example6RazorCode = @"
<BitCalendar AriaLabel=""Select a date""
             MaxDate=""DateTimeOffset.Now.AddDays(5)""
             MinDate=""DateTimeOffset.Now.AddDays(-5)"" />

<BitCalendar AriaLabel=""Select a date""
             MaxDate=""DateTimeOffset.Now.AddMonths(1)""
             MinDate=""DateTimeOffset.Now.AddMonths(-2)"" />

<BitCalendar AriaLabel=""Select a date""
             MaxDate=""DateTimeOffset.Now.AddYears(1)""
             MinDate=""DateTimeOffset.Now.AddYears(-5)"" />";

    private readonly string example7RazorCode = @"
<BitCalendar @bind-Value=""@selectedDate"" AriaLabel=""Select a date"" />
<BitLabel>Selected date: @selectedDate.ToString()</BitLabel>";
    private readonly string example7CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);";

    private readonly string example8RazorCode = @"
<BitCalendar DateFormat=""yyyy/MM/dd hh:mm tt""
             Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
             GoToToday=""برو به امروز"" />

<BitCalendar DateFormat=""yyyy/MM/dd hh:mm tt""
             Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
             GoToToday=""Boro be emrouz"" />";

    private readonly string example9RazorCode = @"
<style>
    .weekend-cell {
        color: red;
    }

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
</style>

<BitCalendar AriaLabel=""Select a date"">
    <DayCellTemplate>
        <span class=""@(context.DayOfWeek == DayOfWeek.Sunday ? ""weekend-cell"" : null)"">
            @context.Day
        </span>
    </DayCellTemplate>
</BitCalendar>

<BitCalendar AriaLabel=""Select a date"">
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
</BitCalendar>";
    private readonly string example9CsharpCode = @"
private CultureInfo Culture = CultureInfo.CurrentUICulture;";

    private readonly string example10RazorCode = @"
<BitCalendar MonthPickerPosition=""@monthPickerPosition"" AriaLabel=""Select a date"" />
<BitToggleButton Text=""Toggle month picker position"" OnChange=""ToggleMonthPickerPosition"" />

<BitCalendar @bind-IsMonthPickerVisible=""@isMonthPickerVisible"" AriaLabel=""Select a date"" />
<BitToggleButton Text=""Toggle month picker visibility"" @bind-IsChecked=""@isMonthPickerVisible"" />";
    private readonly string example10CsharpCode = @"
private bool isMonthPickerVisible = true;
private BitCalendarMonthPickerPosition monthPickerPosition;

private void ToggleMonthPickerPosition(bool newState)
{
    monthPickerPosition = newState ? BitCalendarMonthPickerPosition.Overlay : BitCalendarMonthPickerPosition.Besides;
}";

    private readonly string example11RazorCode = @"
<BitCalendar @bind-Value=""@selectedDateTime"" ShowTimePicker=""true"" AriaLabel=""Select a date"" />
<BitLabel>Selected DateTime: @selectedDateTime.ToString()</BitLabel>";
    private readonly string example11CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;";
}
