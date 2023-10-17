namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Calendar;

public partial class BitCalendarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "System.Globalization.CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the Calendar."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the Calendar.",
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
            Name = "GoToTodayTitle",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "The title of the GoToToday button (tooltip).",
        },
        new()
        {
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button (tooltip).",
        },
        new()
        {
            Name = "GoToNextMonthTitle",
            Type = "string",
            DefaultValue = "Go to next month",
            Description = "The title of the Go to next month button (tooltip).",
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
            Description = "Whether the month picker is shown or hidden.",
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "null",
            Description = "The maximum allowable date of the calendar.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum allowable date of the calendar.",
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
            Description = "Used to set the month picker position.",
            LinkType = LinkType.Link,
            Href ="#month-position-enum",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "Callback for when the user selects a date.",
        },
        new()
        {
            Name = "ShowGoToToday",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the GoToToday button should be shown or not."
        },
        new()
        {
            Name = "ShowWeekNumbers",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the week number (weeks 1 to 53) should be shown before each week row.",
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
            Description = "Whether the time picker should be shown or not.",
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
                    Description = "Show the month picker besides the calendar.",
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



    private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);

    private CultureInfo Culture = CultureInfo.CurrentUICulture;

    private bool isMonthPickerVisible = true;
    private BitCalendarMonthPickerPosition monthPickerPosition;

    private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;

    private string SuccessMessage = string.Empty;
    private BitCalendarValidationModel validationModel = new();

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form was submitted successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }



    private readonly string example1RazorCode = @"
<BitCalendar />
<BitCalendar IsEnabled=""false"" />
<BitCalendar ShowWeekNumbers=""true"" />
<BitCalendar HighlightCurrentMonth=""true"" HighlightSelectedMonth=""true"" />";

    private readonly string example2RazorCode = @"
<BitCalendar MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitCalendar MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitCalendar MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />";

    private readonly string example3RazorCode = @"
<BitCalendar @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>";
    private readonly string example3CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2023, 8, 19, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example4RazorCode = @"
<BitCalendar GoToToday=""برو به امروز"" Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />
<BitCalendar GoToToday=""Boro be emrouz"" Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example5RazorCode = @"
<style>
    .day-cell {
        width: 28px;
        height: 28px;
        position: relative;
    }

    .weekend-cell {
        color: red;
    }

    .badge {
        top: 2px;
        right: 2px;
        width: 8px;
        height: 8px;
        position: absolute;
        border-radius: 50%;
        background-color: red;
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

<BitCalendar>
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitCalendar>

<BitCalendar>
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitCalendar>

<BitCalendar>
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitCalendar>";

    private readonly string example6RazorCode = @"
<BitCalendar IsMonthPickerVisible=""@isMonthPickerVisible"" />
<BitToggleButton OnText=""MonthPicker visible"" OffText=""MonthPicker invisible"" @bind-IsChecked=""@isMonthPickerVisible"" />

<BitCalendar MonthPickerPosition=""@monthPickerPosition"" />
<BitToggleButton OnText=""Position Overlay"" OffText=""Position Besides""
                 OnChange=""v => monthPickerPosition = v ? BitCalendarMonthPickerPosition.Overlay : BitCalendarMonthPickerPosition.Besides"" />";
    private readonly string example6CsharpCode = @"
private bool isMonthPickerVisible = true;
private BitCalendarMonthPickerPosition monthPickerPosition;";

    private readonly string example7RazorCode = @"
<BitCalendar @bind-Value=""@selectedDateTime"" ShowTimePicker=""true"" />
<div>Selected DateTime: @selectedDateTime.ToString()</div>";
    private readonly string example7CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;";

    private readonly string example8RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitCalendar @bind-Value=""validationModel.Date"" />
    <ValidationMessage For=""@(() => validationModel.Date)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset"" ButtonStyle=""BitButtonStyle.Standard""
               OnClick=""() => { validationModel = new(); SuccessMessage=string.Empty; }"">
        Reset
    </BitButton>
</EditForm>";
    private readonly string example8CsharpCode = @"
public class BitCalendarValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private BitCalendarValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";
}
