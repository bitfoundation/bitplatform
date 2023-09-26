namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker allows input a date string directly or not.",
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the DateRangePicker closes automatically after selecting the second value or not.",
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
            Description = "CultureInfo for the DateRangePicker."
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
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = @"Date format like ""yyyy/MM/dd"".",
        },
        new()
        {
            Name = "GoToToday",
            Type = "string",
            DefaultValue = "Go to today",
            Description = "GoToToday text for the DateRangePicker.",
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
            DefaultValue = "true",
            Description = "Determines if the DateRangePicker has a border.",
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
            Description = "Custom DateRangePicker icon template."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
            DefaultValue = "BitIconLocation.Right",
            Description = "DateRangePicker icon location"
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "Optional DateRangePicker icon."
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
            Description = "Whether or not this DateRangePicker is open.",
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
            Description = "Whether or not the Text field of the DateRangePicker is underlined.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Label for the DateRangePicker.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the DateRangePicker."
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Maximum date for the DateRangePicker",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "Minimum date for the DateRangePicker",
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
            Description = "Callback for when clicking on DateRangePicker input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "Callback for when focus moves into the DateRangePicker input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "Callback for when clicking on DateRangePicker input.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<BitDateRangePickerValue?>",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerValue",
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
            Description = "Placeholder text for the DateRangePicker.",
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
            Type = "BitDateRangePickerValue?",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerValue",
            DefaultValue = "null",
            Description = "The value of DateRangePicker.",
        },
        new()
        {
            Name = "ValueChanged",
            Type = "EventCallback<BitDateRangePickerValue?>",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerValue",
            Description = "Callback for when the on date value changed.",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string",
            DefaultValue = "Start: {0} - End: {1}",
            Description = "ValueFormat for the DateRangePicker.",
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
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "dateRangePickerValue",
            Title = "BitDateRangePickerValue",
            Parameters = new()
            {
               new()
               {
                   Name = "StartDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "null",
                   Description = "Indicates the beginning of the date range.",
               },
               new()
               {
                   Name = "EndDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "null",
                   Description = "Indicates the end of the date range.",
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "icon-location-enum",
            Name = "BitIconLocation",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Left",
                    Description="Show the icon at the left side.",
                    Value="0",
                },
                new()
                {
                    Name= "Right",
                    Description="Show the icon at the right side.",
                    Value="1",
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
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example2RazorCode = @"
<BitDateRangePicker IsEnabled=false
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />

<BitDateRangePicker IsEnabled=false
                    Style=""max-width: 300px""
                    Label=""Date range""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example3RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    ShowWeekNumbers=true
                    ShowMonthPickerAsOverlay=true
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example4RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates...""
                    DateFormat=""dd=MM(yy)"" />";

    private readonly string example5RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates...""
                    MaxDate=""DateTimeOffset.Now.AddDays(5)""
                    MinDate=""DateTimeOffset.Now.AddDays(-5)"" />
<br />
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates...""
                    MaxDate=""DateTimeOffset.Now.AddMonths(1)""
                    MinDate=""DateTimeOffset.Now.AddMonths(-2)"" />
<br />
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates...""
                    MaxDate=""DateTimeOffset.Now.AddYears(1)""
                    MinDate=""DateTimeOffset.Now.AddYears(-5)"" />";

    private readonly string example6RazorCode = @"
<BitDateRangePicker @ref=""dateRangePicker""
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."">
    <LabelTemplate>
        Custom label <BitIconButton IconName=""@BitIconName.Calendar"" OnClick=""OpenCallout""></BitIconButton>
    </LabelTemplate>
</BitDateRangePicker>";
    private readonly string example6CsharpCode = @"
private BitDateRangePicker dateRangePicker;
private async Task OpenCallout()
{
    await dateRangePicker.OpenCallout();
}";

    private readonly string example7RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    @bind-Value=""@selectedDateRange""
                    AriaLabel=""select dates""
                    Placeholder=""select dates..."" />
<BitLabel>selected date: @selectedDateRange.StartDate.ToString() - @selectedDateRange.EndDate.ToString()</BitLabel>";
    private readonly string example7CsharpCode = @"
private BitDateRangePickerValue selectedDateRange = new()
{
    StartDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(new DateTime(2020, 1, 25), DateTimeOffset.Now.Offset)
};";

    private readonly string example8RazorCode = @"
<BitDateRangePicker DateFormat=""dd-MM-yyyy""
                    Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
                    GoToToday=""برو به امروز""
                    ValueFormat=""شروع: {0}, پایان: {1}""
                    Style=""max-width: 300px"">
</BitDateRangePicker>

<BitDateRangePicker DateFormat=""dd-MM-yyyy""
                    Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
                    GoToToday=""Boro be emrouz""
                    ValueFormat=""شروع: {0}, پایان: {1}""
                    Style=""max-width: 300px"">
</BitDateRangePicker>";

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

<BitDateRangePicker Label=""Custom weekend cells""
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."">
    <DayCellTemplate>
        <span class=""@(context.DayOfWeek == DayOfWeek.Sunday ? ""weekend-cell"" : null)"">
            @context.Day
        </span>
    </DayCellTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""Custom year, month, and day cells""
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."">
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
            @Culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)

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
</BitDateRangePicker>

<BitDateRangePicker Label=""Icon template""
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    IconLocation=""BitIconLocation.Left""
                    Placeholder=""Select dates..."">
    <IconTemplate>
        <img src=""https://img.icons8.com/fluency/2x/calendar-13.png"" width=""24"" height=""24""/>
    </IconTemplate>
</BitDateRangePicker>";
    private readonly string example9CsharpCode = @"
private CultureInfo Culture = CultureInfo.CurrentUICulture;";

    private readonly string example10RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    ValueFormat=""Dep: {0}, Arr: {1}""
                    DateFormat=""dd=MM(yy)""
                    Placeholder=""Select dates..."" />";

    private readonly string example11RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    IconName=""@BitIconName.Airplane""
                    Placeholder=""Select dates..."" />";

    private readonly string example12RazorCode = @"
<BitDateRangePicker IsResponsive=""true""
                    Style=""max-width: 300px""
                    AriaLabel=""Select a date""
                    Placeholder=""Select a date..."" />";

    private readonly string example13RazorCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AutoClose=""false""
                    AriaLabel=""Select a date""
                    Placeholder=""Select a date..."" />";

    private readonly string example14RazorCode = @"
<BitDateRangePicker @bind-Value=""@selectedDateTimeRange""
                    Label=""Time format 24 hours""
                    ShowTimePicker=""true""
                    Style=""max-width: 300px""
                    AriaLabel=""Select a date""
                    Placeholder=""Select a date..."" />

<BitDateRangePicker @bind-Value=""@selectedDateTimeRange""
                    Label=""Time format 12 hours""
                    ShowTimePicker=""true""
                    TimeFormat=""BitTimeFormat.TwelveHours""
                    Style=""max-width: 300px""
                    AriaLabel=""Select a date""
                    Placeholder=""Select a date..."" />";

    private readonly string example14CsharpCode = @"
private BitDateRangePickerValue selectedDateTimeRange = new()
{
    StartDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(new DateTime(2020, 1, 25), DateTimeOffset.Now.Offset)
};";
}
