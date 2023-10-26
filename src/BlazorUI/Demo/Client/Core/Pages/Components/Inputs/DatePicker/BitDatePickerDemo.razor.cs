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
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template to render the year cells of the DatePicker."
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



    private readonly string example1RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example2RazorCode = @"
<BitDatePicker IsEnabled=false
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />

<BitDatePicker IsEnabled=false
               Style=""max-width: 350px""
               Label=""Start date""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example3RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               ShowWeekNumbers=true
               ShowMonthPickerAsOverlay=true
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example4RazorCode = @"
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

    private readonly string example5RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               AllowTextInput=true
               HighlightSelectedMonth=true
               Label=""Start date""
               DateFormat=""dd/MM/yyyy""
               AriaLabel=""Select a date""
               Placeholder=""Enter a date (DD/MM/YYYY)"" />";

    private readonly string example6RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date.""
               Placeholder=""Select a date...""
               DateFormat=""dd=MM(yy)"" />";

    private readonly string example7RazorCode = @"
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

    private readonly string example8RazorCode = @"
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

    private readonly string example9RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               @bind-Value=""@selectedDate"" 
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />
<BitLabel>Selected date: @selectedDate.ToString()</BitLabel>";
    private readonly string example9CsharpCode = @"
private DateTimeOffset? selectedDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset);";

    private readonly string example10RazorCode = @"
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

    private readonly string example11RazorCode = @"
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

    private readonly string example12RazorCode = @"
<BitDatePicker Style=""max-width: 350px""
               AriaLabel=""Select a date""
               IconName=""@BitIconName.Airplane""
               Placeholder=""Select a date..."" />";

    private readonly string example13RazorCode = @"
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

    private readonly string example14RazorCode = @"
<BitDatePicker IsResponsive=""true""
               Style=""max-width: 350px""
               AriaLabel=""Select a date""
               Placeholder=""Select a date..."" />";

    private readonly string example15RazorCode = @"
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
