using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.DatePicker
{
    public partial class BitDatePickerDemo
    {
        private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);

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
                Name = "CalendarType",
                Type = "BitCalendarType",
                LinkType = LinkType.Link,
                Href = "#calendar-type-enum",
                DefaultValue = "BitCalendarType.gregorian",
                Description = "Calendar type for the DatePicker.",
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
                Name = "OnMonthChange",
                Type = "EventCallback<int>",
                DefaultValue = "",
                Description = "Callback for when the month changes.",
            },
            new ComponentParameter()
            {
                Name = "OnYearChange",
                Type = "EventCallback<int>",
                DefaultValue = "",
                Description = "Callback for when the year changes.",
            },
            new ComponentParameter()
            {
                Name = "OnDateSet",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the date changes.",
            },
            new ComponentParameter()
            {
                Name = "OnSelectDate",
                Type = "Func<BitDate, string>",
                DefaultValue = "",
                Description = "Callback for when the on selected date changed.",
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
                Type = "string",
                DefaultValue = "",
                Description = "The value of DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "CalueChanged",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when DatePicker value changed.",
            },
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "calendar-type-enum",
                Title = "CalendarType Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Gregorian",
                        Description="Show DatePicker in Gregorian calendar.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Persian",
                        Description="Show DatePicker in Persian calendar.",
                        Value="1",
                    }
                }
            }
        };

        private readonly string example1HTMLCode = @"<BitDatePicker Style=""width: 300px""
               ShowMonthPickerAsOverlay=""true"">
</BitDatePicker>";

        private readonly string example2HTMLCode = @"<BitDatePicker Style=""width: 300px""
               ShowWeekNumbers=""true"">
</BitDatePicker>";

        private readonly string example3HTMLCode = @"<BitDatePicker Culture=""@(new System.Globalization.CultureInfo(""fa-IR""))""
               GoToToday=""برو به امروز""
               Style=""width: 300px"">
</BitDatePicker>";

        private readonly string example4HTMLCode = @"<BitDatePicker Culture=""Bit.Client.Web.BlazorUI.CultureInfoHelper.GetPersianCultureByFinglishNames()""
               GoToToday=""Boro be emrouz""
               Style=""width: 300px"">
</BitDatePicker>";

        private readonly string example5HTMLCode = @"<BitDatePicker @bind-Value=""@selectedDate"" Style=""width: 300px""></BitDatePicker>
<BitLabel>this is selected date: @selectedDate.ToString()</BitLabel>";

        private readonly string example5CSharpCode = @"
@code {
    private DateTimeOffset? selectedDate = new DateTimeOffset(new DateTime(2020, 1, 17), DateTimeOffset.Now.Offset);
}";

        private readonly string example6HTMLCode = @"<BitDatePicker FormatDate=""d"" Style=""width: 300px""></BitDatePicker>";
    }
}
