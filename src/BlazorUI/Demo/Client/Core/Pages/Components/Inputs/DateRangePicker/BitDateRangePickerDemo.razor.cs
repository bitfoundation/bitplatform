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
            Description = "Whether or not the DateRangePicker allows string date inputs.",
        },
        new()
        {
            Name = "AutoClose",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the DateRangePicker closes automatically after selecting the second value.",
        },
        new()
        {
            Name = "CalloutAriaLabel",
            Type = "string",
            DefaultValue = "Calendar",
            Description = "Aria label of the DateRangePicker's callout for screen readers."
        },
        new()
        {
            Name = "CalloutHtmlAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional html attributes for the DateRangePicker's callout."
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
            Name = "DateFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The format of the date in the DateRangePicker.",
        },
        new()
        {
            Name = "DayCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the day cells of the DateRangePicker."
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
            Name = "GoToPrevMonthTitle",
            Type = "string",
            DefaultValue = "Go to previous month",
            Description = "The title of the Go to previous month button.",
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
            Description = "Custom template for the DateRangePicker's icon."
        },
        new()
        {
            Name = "IconLocation",
            Type = "BitIconLocation",
            DefaultValue = "BitIconLocation.Right",
            Description = "Determines the location of the DateRangePicker's icon.",
            LinkType = LinkType.Link,
            Href = "#icon-location-enum",
        },
        new()
        {
            Name = "IconName",
            Type = "string",
            DefaultValue = "CalendarMirrored",
            Description = "The name of the DateRangePicker's icon."
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
            Description = "Whether or not the DateRangePicker's callout is open.",
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
            Description = "The text of the DateRangePicker's label.",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Custom template for the DateRangePicker's label."
        },
        new()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The maximum date allowed for the DateRangePicker.",
        },
        new()
        {
            Name = "MinDate",
            Type = "DateTimeOffset?",
            DefaultValue = "null",
            Description = "The minimum date allowed for the DateRangePicker.",
        },
        new()
        {
            Name = "MonthCellTemplate",
            Type = "RenderFragment<DateTimeOffset>?",
            DefaultValue = "null",
            Description = "Custom template to render the month cells of the DateRangePicker."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback",
            Description = "The callback for clicking on the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocus",
            Type = "EventCallback",
            Description = "The callback for focusing the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocusIn",
            Type = "EventCallback",
            Description = "The callback for when the focus moves into the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnFocusOut",
            Type = "EventCallback",
            Description = "The callback for when the focus moves out of the DateRangePicker's input.",
        },
        new()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<BitDateRangePickerValue?>",
            Description = "The callback for selecting a date in the DateRangePicker.",
            LinkType = LinkType.Link,
            Href = "#date-range-picker-value",
        },
        new()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "The placeholder text of the DateRangePicker's input.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker's close button should be shown or not."
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
            Description = "The tabIndex of the DateRangePicker's input.",
        },
        new()
        {
            Name = "TimeFormat",
            Type = "BitTimeFormat",
            DefaultValue = "BitTimeFormat.TwentyFourHours",
            Description = "Time format of the time-pickers, 24H or 12H.",
            LinkType = LinkType.Link,
            Href = "#time-format-enum",
        },
        new()
        {
            Name = "ValueFormat",
            Type = "string",
            DefaultValue = "Start: {0} - End: {1}",
            Description = "The string format used to show the DateRangePicker's value in its input.",
        },
        new()
        {
            Name = "YearCellTemplate",
            Type = "RenderFragment<int>?",
            DefaultValue = "null",
            Description = "Custom template to render the year cells of the DateRangePicker."
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "date-range-picker-value",
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



    private BitDateRangePickerValue selectedDateRange = new()
    {
        StartDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset),
        EndDate = new DateTimeOffset(2020, 1, 25, 0, 0, 0, DateTimeOffset.Now.Offset),
    };

    private CultureInfo culture = CultureInfo.CurrentUICulture;



    private readonly string example1RazorCode = @"
<BitDateRangePicker Label=""Basic DateRangePicker"" />
<BitDateRangePicker Label=""Disabled"" IsEnabled=""false"" />
<BitDateRangePicker Label=""PlaceHolder"" Placeholder=""Select a date range"" />
<BitDateRangePicker Label=""Week numbers"" ShowWeekNumbers=""true"" />
<BitDateRangePicker Label=""Highlight months"" HighlightCurrentMonth=""true"" HighlightSelectedMonth=""true"" />
<BitDateRangePicker Label=""TimePicker"" ShowTimePicker=""true"" />
<BitDateRangePicker Label=""Custom Icon"" IconName=""@BitIconName.Airplane"" />
<BitDateRangePicker Label=""Disabled AutoClose"" AutoClose=""false"" />";

    private readonly string example2RazorCode = @"
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddDays(-5)"" MaxDate=""DateTimeOffset.Now.AddDays(5)"" />
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddMonths(-2)"" MaxDate=""DateTimeOffset.Now.AddMonths(1)"" />
<BitDateRangePicker MinDate=""DateTimeOffset.Now.AddYears(-5)"" MaxDate=""DateTimeOffset.Now.AddYears(1)"" />";

    private readonly string example3RazorCode = @"
<BitDateRangePicker Label=""DateFormat: 'dd=MM(yy)'"" DateFormat=""dd=MM(yy)"" />
<BitDateRangePicker Label=""ValueFormat: 'Dep: {0}, Arr: {1}'"" ValueFormat=""Dep: {0}, Arr: {1}"" />";

    private readonly string example4RazorCode = @"
<BitDateRangePicker @bind-Value=""@selectedDateRange"" />
<div>From: <b>@selectedDateRange.StartDate.ToString()</b></div>
<div>To: <b>@selectedDateRange.EndDate.ToString()</b></div>";
    private readonly string example4CsharpCode = @"
private BitDateRangePickerValue selectedDateRange = new()
{
    StartDate = new DateTimeOffset(2020, 1, 17, 0, 0, 0, DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(2020, 1, 25, 0, 0, 0, DateTimeOffset.Now.Offset)
};";

    private readonly string example5RazorCode = @"
<BitDateRangePicker Label=""fa-IR culture with Farsi names""
                    GoToTodayTitle=""برو به امروز""
                    ValueFormat=""شروع: {0}, پایان: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFarsiNames()"" />

<BitDateRangePicker Label=""fa-IR culture with Fingilish names""
                    GoToTodayTitle=""Boro be emrouz""
                    ValueFormat=""Shoro: {0}, Payan: {1}""
                    Culture=""CultureInfoHelper.GetFaIrCultureWithFingilishNames()"" />";

    private readonly string example6RazorCode = @"
<BitDateRangePicker>
    <LabelTemplate>
        Custom label <BitIcon IconName=""@BitIconName.Calendar"" />
    </LabelTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""DayCellTemplate"">
    <DayCellTemplate>
        <span class=""day-cell@(context.DayOfWeek == DayOfWeek.Sunday ? "" weekend-cell"" : null)"">
            @context.Day

            @if (context.Day % 5 is 0)
            {
                <span class=""badge""></span>
            }
        </span>
    </DayCellTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""MonthCellTemplate"">
    <MonthCellTemplate>
        <div style=""width:28px;padding:3px;color:black;background:@(context.Month == 1 ? ""lightcoral"" : ""yellowgreen"")"">
            @culture.DateTimeFormat.GetAbbreviatedMonthName(context.Month)
        </div>
    </MonthCellTemplate>
</BitDateRangePicker>

<BitDateRangePicker Label=""YearCellTemplate"">
    <YearCellTemplate>
        <span style=""position: relative"">
            @context
            <span class=""year-suffix"">AC</span>
        </span>
    </YearCellTemplate>
</BitDateRangePicker>";
    private readonly string example6CsharpCode = @"
private CultureInfo culture = CultureInfo.CurrentUICulture;";

    private readonly string example7RazorCode = @"
<BitDateRangePicker Label=""Responsive DateRangePicker"" IsResponsive=""true"" />";
}
