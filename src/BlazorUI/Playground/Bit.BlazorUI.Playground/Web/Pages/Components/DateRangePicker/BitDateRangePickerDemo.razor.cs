using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DateRangePicker;

public partial class BitDateRangePickerDemo
{
    private BitDateRangePickerValue selectedDateRange = new()
    {
        StartDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset),
        EndDate = new DateTimeOffset(new DateTime(2020, 1, 25), DateTimeOffset.Now.Offset)
    };
    private CultureInfo Culture = CultureInfo.CurrentUICulture;
    private BitDateRangePicker dateRangePicker;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AllowTextInput",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the DateRangePicker allows input a date string directly or not.",
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
            Description = "CultureInfo for the DateRangePicker."
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
            Description = "GoToToday text for the DateRangePicker.",
        },
        new ComponentParameter()
        {
            Name = "HasBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines if the DateRangePicker has a border.",
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
            Description = "Whether or not this DateRangePicker is open.",
        },
        new ComponentParameter()
        {
            Name = "IsUnderlined",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not the Textfield of the DateRangePicker is underlined.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "Label for the DateRangePicker.",
        },
        new ComponentParameter
        {
            Name = "LabelFragment",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the DateRangePicker."
        },
        new ComponentParameter()
        {
            Name = "MaxDate",
            Type = "DateTimeOffset",
            DefaultValue = "",
            Description = "Maximum date for the DateRangePicker",
        },
        new ComponentParameter()
        {
            Name = "MinDate",
            Type = "DateTimeOffset",
            DefaultValue = "",
            Description = "Minimum date for the DateRangePicker",
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
            Description = "Callback for when clicking on DateRangePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusIn",
            Type = "EventCallback<FocusEventArgs>",
            DefaultValue = "",
            Description = "Callback for when focus moves into the DateRangePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnFocusOut",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when clicking on DateRangePicker input.",
        },
        new ComponentParameter()
        {
            Name = "OnSelectDate",
            Type = "EventCallback<BitDateRangePickerValue?>",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerType",
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
            Description = "Placeholder text for the DateRangePicker.",
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
            Type = "BitDateRangePickerValue",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerType",
            DefaultValue = "",
            Description = "The value of DateRangePicker.",
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<BitDateRangePickerValue?>",
            LinkType = LinkType.Link,
            Href = "#dateRangePickerType",
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

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "dateRangePickerType",
            Title = "BitDateRangePickerValue",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "StartDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "",
                   Description = "Indicates the beginning of the date range.",
               },
               new ComponentParameter()
               {
                   Name = "EndDate",
                   Type = "DateTimeOffset?",
                   DefaultValue = "",
                   Description = "Indicates the end of the date range.",
               }
            }
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

    private async Task OpenCallout(MouseEventArgs eventArgs)
    {
        await dateRangePicker.OpenCallout(eventArgs);
    }

    private readonly string example1HTMLCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example2HTMLCode = @"
<BitDateRangePicker IsEnabled=false
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example3HTMLCode = @"
<BitDateRangePicker IsEnabled=false
                    Style=""max-width: 300px""
                    Label=""Date range""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example4HTMLCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    ShowWeekNumbers=true
                    ShowMonthPickerAsOverlay=true
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."" />";

    private readonly string example5HTMLCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates...""
                    FormatDate=""dd=MM(yy)"" />";

    private readonly string example6HTMLCode = @"
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

    private readonly string example7HTMLCode = @"
<BitDateRangePicker @ref=""dateRangePicker""
                    Style=""max-width: 300px""
                    AriaLabel=""Select dates""
                    Placeholder=""Select dates..."">
    <LabelFragment>
        Custom label <BitIconButton IconName=""BitIconName.Calendar"" OnClick=""OpenCallout""></BitIconButton>
    </LabelFragment>
</BitDateRangePicker>";

    private readonly string example7CSharpCode = @"
private BitDateRangePicker dateRangePicker;
private async Task OpenCallout(MouseEventArgs eventArgs)
{
    await dateRangePicker.OpenCallout(eventArgs);
}";

    private readonly string example8HTMLCode = @"
<BitDateRangePicker Style=""max-width: 300px""
                    @bind-Value=""@selectedDateRange""
                    AriaLabel=""select dates""
                    Placeholder=""select dates..."" />
<BitLabel>selected date: @selectedDateRange.StartDate.ToString() - @selectedDateRange.EndDate.ToString()</BitLabel>";

    private readonly string example8CSharpCode = @"
private BitDateRangePickerValue selectedDateRange = new()
{
    StartDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset),
    EndDate = new DateTimeOffset(new DateTime(2020, 1, 25), DateTimeOffset.Now.Offset)
};";

    private readonly string example9HTMLCode = @"
<BitDateRangePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFarsiNames()""
               GoToToday=""برو به امروز""
               Style=""max-width: 300px"">
</BitDateRangePicker>";

    private readonly string example10HTMLCode = @"
<BitDateRangePicker FormatDate=""yyyy/MM/dd hh:mm tt"" 
               Culture=""CultureInfoHelper.GetFaIrCultureByFingilishNames()""
               GoToToday=""Boro be emrouz""
               Style=""max-width: 300px"">
</BitDateRangePicker>";

    private readonly string example11HTMLCode = @"
<style>
.weekend-cell {
    color: red;
}
</style>

<BitDateRangePicker Style=""max-width: 300px""
               AriaLabel=""Select dates""
               Placeholder=""Select dates..."">
    <DayCellTemplate>
        <span class=""@(context.DayOfWeek == DayOfWeek.Sunday ? ""weekend-cell"" : null)"">
            @context.Day
        </span>
    </DayCellTemplate>
</BitDateRangePicker>";

    private readonly string example12HTMLCode = @"
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

.date-range-picker-wrapper {
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

<div class=""date-range-picker-wrapper"">
    <BitDateRangePicker Style=""max-width: 300px""
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
    </BitDateRangePicker>
</div>";

    private readonly string example12CSharpCode = @"
private CultureInfo Culture = CultureInfo.CurrentUICulture;";
}
