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

        private readonly string datePickerSampleCode = $"<BitDatePicker Style='width: 300px' FirstDayOfWeek='firstDayOfWeek'></BitDatePicker>{Environment.NewLine}" +
            $"<BitDatePicker Style='width: 300px'{Environment.NewLine}" +
            $"Items='@GetFirstDayOfWeekDropdownItems()'{Environment.NewLine}" +
            $"Label='Select The First Day Of The Week'{Environment.NewLine}" +
            $"DefaultSelectedKey='0'{Environment.NewLine}" +
            $"OnSelectItem='@SelectFirstDayOfWeek'>{Environment.NewLine}" +
            $"@code {{{Environment.NewLine}" +
            $"private DayOfWeek firstDayOfWeek = DayOfWeek.Sunday;{Environment.NewLine}" +
            $"private List<BitDropDownItem> GetFirstDayOfWeekDropdownItems(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"List<BitDropDownItem> items = new(){Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Sunday',{Environment.NewLine}" +
            $"Value = '0'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Monday',{Environment.NewLine}" +
            $"Value = '1'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Tuesday',{Environment.NewLine}" +
            $"Value = '2'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Wednesday',{Environment.NewLine}" +
            $"Value = '3'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Thursday',{Environment.NewLine}" +
            $"Value = '4'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Friday',{Environment.NewLine}" +
            $"Value = '5'{Environment.NewLine}" +
            $"items.Add(new BitDropDownItem(){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"ItemType = BitDropDownItemType.Normal,{Environment.NewLine}" +
            $"Text = 'Saturday',{Environment.NewLine}" +
            $"Value = '6'{Environment.NewLine}" +
            $"}});{Environment.NewLine}" +
            $"}}{Environment.NewLine}" +
            $"private string SelectFirstDayOfWeek(BitDropDownItem item){Environment.NewLine}" +
            $"{{{Environment.NewLine}" +
            $"firstDayOfWeek = (DayOfWeek)int.Parse(item.Value);{Environment.NewLine}" +
            $"}}{Environment.NewLine}" +
            $"}}";

        private readonly string datePickerWithMonthSampleCode = $"<BitDatePicker Style='width: 300px' ShowMonthPickerAsOverlay='true'></BitDatePicker>";

        private readonly string datePickerWithWeekSampleCode = $"<BitDatePicker Style='width: 300px' ShowWeekNumbers='true'></BitDatePicker>";

        private readonly string shamsiDatePickerSampleCode = $"<BitDatePicker Style='width: 300px' CalendarType='CalendarType.Persian' FirstDayOfWeek='DayOfWeek.Saturday'></BitDatePicker>";

        private readonly string datePickerWithTwoWayBindingSampleCode = $"<BitDatePicker Style='width: 300px' @bind-Value='selectedDate'></BitDatePicker>{Environment.NewLine}" +
           $"<BitLabel >this is selected date: @selectedDate </BitLabel>{Environment.NewLine}" +
           $"@code {{{Environment.NewLine}" +
           $"private string selectedDate ='';{Environment.NewLine}" +
           $"}}";

        private readonly string datePickerWithcustomFormattingSampleCode = $"<BitDatePicker Style='width: 300px' OnSelectDate='OnDateFormat'></BitDatePicker>{Environment.NewLine}" +
           $"<BitLabel >this is selected date: @selectedDate </BitLabel>{Environment.NewLine}" +
           $"@code {{{Environment.NewLine}" +
           $"private string OnDateFormat(BitDate date) => $'{{date.Year}}/{{date.Month}}/{{date.Day}}';{Environment.NewLine}" +
           $"}}";

        private List<BitDropDownItem> GetFirstDayOfWeekDropdownItems()
        {
            List<BitDropDownItem> items = new();

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Sunday",
                Value = "0"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Monday",
                Value = "1"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Tuesday",
                Value = "2"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Wednesday",
                Value = "3"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Thursday",
                Value = "4"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Friday",
                Value = "5"
            });

            items.Add(new BitDropDownItem()
            {
                ItemType = BitDropDownItemType.Normal,
                Text = "Saturday",
                Value = "6"
            });

            return items;
        }
    }
}
