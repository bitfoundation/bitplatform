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
            Description = "Whether the DatePicker allows input a date string directly or not.",
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional attributes in addition to the main callout's parameters."
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
            Description = @"The format of the date in the DatePicker like ""yyyy/MM/dd"".",
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
            Description = "GoToToday text for the DatePicker.",
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
            Description = "Custom DatePicker icon template."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
            DefaultValue = "BitIconLocation.Right",
            Description = "DatePicker icon location."
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "Optional DatePicker icon."
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
            Description = "Label for the DatePicker.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the DatePicker."
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
            Name = "OnClick",
            Type = "EventCallback",
            Description = "Callback for when clicking on DatePicker input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "Callback for when focus moves into the DatePicker input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "Callback for when clicking on DatePicker input.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<DateTimeOffset?>",
            Description = "Callback for when the on selected date changed.",
        },
        new()
        {
            Name = "PickerAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label for date picker popup for screen reader users."
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "Placeholder text for the DatePicker.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the CalendarDay close button should be shown or not."
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
            Name = "ShowMonthPickerAsOverlay",
            Type = "bool",
            DefaultValue = "false",
            Description = "Show month picker on top of date picker when visible.",
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
            Description = "The value of DatePicker.",
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
        },
        new()
        {
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "Time format of the time pickers, 24H or 12H"
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



    private readonly string example1HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example2HtmlCode = @"
<BitDatePicker IsEnabled=false
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />

<BitDatePicker IsEnabled=false
               Style=""max-width: 350px""
               Label=""Start date""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example3HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               ShowWeekNumbers=true
               ShowMonthPickerAsOverlay=true
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example4HtmlCode = @"
<EditForm Model=""formValidationDatePickerModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
    <DataAnnotationsValidator />

    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
    <div>
        <BitDatePicker @bind-Value=""formValidationDatePickerModel.Date""
                       AllowTextInput=""true""
                       Style=""max-width: 350px""
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

@if (string.IsNullOrEmpty(SuccessMessage) is false)
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";
    private readonly string example4CsharpCode = @"
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

    private readonly string example5HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               AllowTextInput=true
               HighlightSelectedMonth=true
               Label=""Start date""
               DateFormat=""dd/MM/yyyy""
               AriaLabel=""Select a date""
               Placeholder=""Enter a date (DD/MM/YYYY)"" />";

    private readonly string example6HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date.""
               Placeholder=""Select a date...""
               DateFormat=""dd=MM(yy)"" />";

    private readonly string example7HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date...""
               MaxDate=""DateTimeOffset.Now.AddDays(5)""
               MinDate=""DateTimeOffset.Now.AddDays(-5)"" />
<br />
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date...""
               MaxDate=""DateTimeOffset.Now.AddMonths(1)""
               MinDate=""DateTimeOffset.Now.AddMonths(-2)"" />
<br />
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date...""
               MaxDate=""DateTimeOffset.Now.AddYears(1)""
               MinDate=""DateTimeOffset.Now.AddYears(-5)"" />";

    private readonly string example8HtmlCode = @"
<BitDatePicker @ref=""datePicker""
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."">
    <LabelTemplate>
        Custom label <BitIconButton IconName=""@BitIconName.Calendar"" OnClick=""OpenCallout""></BitIconButton>
    </LabelTemplate>
</BitDatePicker>";
    private readonly string example8CsharpCode = @"
private BitDatePicker datePicker;
private async Task OpenCallout()
{
    await datePicker.OpenCallout();
}";

    private readonly string example9HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               @bind-Value=""@selectedDate"" 
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />
<BitLabel>Selected date: @selectedDate.ToString()</BitLabel>";
    private readonly string example9CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);";

    private readonly string example10HtmlCode = @"
<BitDatePicker DateFormat=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
               GoToToday=""برو به امروز""
               Style=""max-width: 350px"">
</BitDatePicker>

<BitDatePicker DateFormat=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
               GoToToday=""Boro be emrouz""
               Style=""max-width: 350px"">
</BitDatePicker>";

    private readonly string example11HtmlCode = @"
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

<BitDatePicker Label=""Custom weekend cells""
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."">
    <DayCellTemplate>
        <span class=""@(context.DayOfWeek == DayOfWeek.Sunday ? ""weekend-cell"" : null)"">
            @context.Day
        </span>
    </DayCellTemplate>
</BitDatePicker>

<BitDatePicker Label=""Custom year, month, and day cells""
               Style=""max-width: 350px""
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


<BitDatePicker Label=""Custom icon template""
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               IconLocation=""BitIconLocation.Left""
               Placeholder=""Select a date..."">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/calendar-13.png"" width=""24"" height=""24"" />
    </IconTemplate>
</BitDatePicker>";
    private readonly string example11CsharpCode = @"
private CultureInfo Culture = CultureInfo.CurrentUICulture;";

    private readonly string example12HtmlCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               IconName=""@BitIconName.Airplane""
               Placeholder=""Select a date..."" />";

    private readonly string example13HtmlCode = @"
<EditForm Model=""formValidationDatePickerModel"">
    <DataAnnotationsValidator />
    <div>
        <BitDatePicker @bind-Value=""formValidationDatePickerModel.Date""
                        Style=""max-width: 350px""
                        AllowTextInput=""true""
                        Label=""BitDatePicker with Custom Invalid Error Message""
                        InvalidErrorMessage=""Invalid Date!!!"" />
        <ValidationMessage For=""@(() => formValidationDatePickerModel.Date)"" />
    </div>
    <br />
    <div class=""validation-summary"">
        <ValidationSummary />
    </div>
</EditForm>";
    private readonly string example13CsharpCode = @"
public class FormValidationDatePickerModel
{
    [Required]
    public DateTimeOffset? Date { get; set; }
}

private FormValidationDatePickerModel formValidationDatePickerModel = new();";

    private readonly string example14HtmlCode = @"
<BitDatePicker IsResponsive=""true""
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example15HtmlCode = @"
<BitDatePicker @bind-Value=""@selectedDateTime""
                Label=""Time format 24 hours""
                Style=""max-width: 350px""
                ShowTimePicker=""true""
                AriaLabel=""Select a date""
                Placeholder=""Select a date..."" />

<BitDatePicker @bind-Value=""@selectedDateTime""
                Label=""Time format 12 hours""
                Style=""max-width: 350px""
                ShowTimePicker=""true""
                TimeFormat=""BitTimeFormat.TwelveHours""
                AriaLabel=""Select a date""
                Placeholder=""Select a date..."" />";
    private readonly string example15CsharpCode = @"
private DateTimeOffset? selectedDateTime = DateTimeOffset.Now;";
}
