namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DatePicker;

public partial class BitDatePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the DatePicker allows a string date input.",
        },
        new()
        {
            Name = "CalloutAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label of the DatePicker's callout for screen readers."
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional html attributes for the DatePicker's callout."
        },
        new()
        {
            Name = "Culture",
            Type = "CultureInfo",
            DefaultValue = "CultureInfo.CurrentUICulture",
            Description = "CultureInfo for the DatePicker."
        },
        new()
        {
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the DatePicker.",
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the day cells of the DatePicker."
        },
        new()
        {
            Name = "GoToNextMonthTitle",
            Type = "string",
            DefaultValue = "Go to next month",
            Description = "The title of the Go to next month button (tooltip)."
        },
        new()
        {
            Name = "GoToNextYearTitle",
            Type = "string",
            DefaultValue = "Go to next year {0}",
            Description = "The title of the Go to next year button (tooltip)."
        },
        new()
        {
            Name = "GoToNextYearRangeTitle",
            Type = "string",
            DefaultValue = "Next year range {0} - {1}",
            Description = "The title of the Go to next year range button (tooltip)."
        },
        new()
        {
            Name = "GoToPreviousYearRangeTitle",
            Type = "string",
            DefaultValue = "Previous year range {0} - {1}",
            Description = "The title of the Go to previous year range button (tooltip)."
        },
        new()
        {
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button (tooltip)."
        },
        new()
        {
            Name = "GoToPrevYearTitle",
            Type = "string",
            DefaultValue = "Go to previous year {0}",
            Description = "The title of the Go to previous year button (tooltip)."
        },
        new()
        {
            Name = "GoToTodayTitle",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "The title of the GoToToday button (tooltip)."
        },
        new()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the DatePicker has a border.",
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
            Name = "IconTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DatePicker's icon."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            DefaultValue = "BitIconLocation.Right",
            Description = "Determines the location of the DatePicker's icon.",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the DatePicker's icon."
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
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this DatePicker is open.",
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
            Description = "Whether or not the Text field of the DatePicker is underlined.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the DatePicker's label.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DatePicker's label."
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DatePicker.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DatePicker.",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the month cells of the DatePicker."
        },
        new()
        {
            Name = "MonthPickerToggleAriaLabel",
            Type = "string",
            DefaultValue = "{0}, change month",
            Description = "The aria-label of the month picker's toggle."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            Description = "The callback for clicking on the DatePicker's input.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "The callback for focusing the DatePicker's input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "The callback for when the focus moves into the DatePicker's input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "The callback for when the focus moves out of the DatePicker's input.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "The callback for selecting a date in the DatePicker.",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The placeholder text of the DatePicker's input.",
        },
        new()
        {
            Name = "SelectedDateAriaAtomic",
            Type = "string",
            DefaultValue = "Selected date {0}",
            Description = "The text of selected date aria-atomic of the calendar."
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DatePicker's close button should be shown or not."
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
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date picker when visible.",
        },
        new()
        {
            Name = "ShowTimePicker",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not render the time-picker.",
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
            Name = "TabIndex",
            Type = "int",
            DefaultValue = "0",
            Description = "The tabIndex of the DatePicker's input.",
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
            Name = "WeekNumberTitle",
            Type = "string",
            DefaultValue = "Week number {0}",
            Description = "The title of the week number (tooltip)."
        },
        new()
        {
            Name = "WeekNumberAriaLabel",
            Type = "string",
            DefaultValue = "Week number {0}",
            Description = "The aria-label of the week number."
        },
        new()
        {
            Name = "WeekNumberTitle",
            Type = "string",
            DefaultValue = "Week number {0}",
            Description = "The title of the week number (tooltip)."
        },
        new()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template to render the year cells of the DatePicker."
        },
        new()
        {
            Name = "YearPickerToggleAriaLabel",
            Type = "string",
            DefaultValue = "{0}, change year",
            Description = "The aria-label of the year picker's toggle."
        },
        new()
        {
            Name = "YearRangePickerToggleAriaLabel",
            Type = "string",
            DefaultValue = "{0} - {1}, change month",
            Description = "The aria-label of the year range picker's toggle."
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
            Id = "time-format-enum",
            Name = "BitTimeFormat",
            Description = "",
            Items = new()
            {
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
            }
        }
    };



    private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);

    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private BitDatePickerValidationModel validationModel = new();
    private string SuccessMessage = string.Empty;

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



    private readonly string example1RazorCode = @"
<BitDatePicker Label=""Basic DatePicker"" />
<BitDatePicker Label=""Disabled"" IsEnabled=""false"" />
<BitDatePicker Label=""PlaceHolder"" Placeholder=""Select a date"" />
<BitDatePicker Label=""Week numbers"" ShowWeekNumbers=""true"" />
<BitDatePicker Label=""Highlight months"" HighlightCurrentMonth=""true"" HighlightSelectedMonth=""true"" />
<BitDatePicker Label=""TimePicker"" ShowTimePicker=""true"" />";

    private readonly string example2RazorCode = @"
<BitDatePicker MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitDatePicker MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />";

    private readonly string example3RazorCode = @"
<BitDatePicker Label=""Text input allowed""
               AllowTextInput=true
               DateFormat=""dd/MM/yyyy""
               Placeholder=""Enter a date (dd/MM/yyyy)"" />";

    private readonly string example4RazorCode = @"
<BitDatePicker Label=""Formatted Date""
               DateFormat=""dd=MM(yy)""
               Placeholder=""Select a date"" />";

    private readonly string example5RazorCode = @"
<BitDatePicker @bind-Value=""@selectedDate"" />
<div>Selected date: @selectedDate.ToString()</div>";
    private readonly string example5CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example6RazorCode = @"
<BitDatePicker Label=""fa-IR culture with Farsi names""
               GoToTodayTitle=""برو به امروز""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDatePicker Label=""fa-IR culture with Fingilish names""
               GoToTodayTitle=""Boro be emrouz""
               Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example7RazorCode = @"
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

<BitDatePicker>
    <LabelTemplate>
        Custom label <BitIcon IconName=""@BitIconName.Calendar"" />
    </LabelTemplate>
</BitDatePicker>

<BitDatePicker Label=""DayCellTemplate"">
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitDatePicker>

<BitDatePicker Label=""MonthCellTemplate"">
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitDatePicker>

<BitDatePicker Label=""YearCellTemplate"">
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitDatePicker>";
    private readonly string example7CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example8RazorCode = @"
<BitDatePicker Label=""Response DatePicker""
               IsResponsive=""true""
               Placeholder=""Select a date"" />";

    private readonly string example9RazorCode = @"
<style>
    .validation-message {
        color: red;
    }
</style>

<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <BitDatePicker @bind-Value=""validationModel.Date"" />
    <ValidationMessage For=""@(() => validationModel.Date)"" />
    
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    <BitButton ButtonType=""BitButtonType.Reset"" ButtonStyle=""BitButtonStyle.Standard""
               OnClick=""() => { validationModel = new(); SuccessMessage=string.Empty; }"">
        Reset
    </BitButton>
</EditForm>";
    private readonly string example9CsharpCode = @"
public class BitDatePickerValidationModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private BitDatePickerValidationModel validationModel = new();

private void HandleValidSubmit() { }
private void HandleInvalidSubmit() { }";
}
