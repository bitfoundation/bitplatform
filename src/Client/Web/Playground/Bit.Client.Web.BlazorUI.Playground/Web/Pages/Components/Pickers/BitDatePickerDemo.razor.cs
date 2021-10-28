using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pickers
{
    public partial class BitDatePickerDemo
    {
        private string selectedDate = "";
        private DayOfWeek firstDayOfWeek = DayOfWeek.Sunday;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "allowTextInput",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the DatePicker allows input a date string directly or not",
            },
            new ComponentParameter()
            {
                Name = "calendarType",
                Type = "BitCalendarType",
                LinkType = LinkType.Link,
                Href = "#calendar-type-enum",
                DefaultValue = "BitCalendarType.gregorian",
                Description = "Calendar type for the DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "goToToday",
                Type = "string",
                DefaultValue = "Go to today",
                Description = "GoToToday text for the DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "hasBorder",
                Type = "bool",
                DefaultValue = "false",
                Description = "Determines if the DatePicker has a border.",
            },
            new ComponentParameter()
            {
                Name = "isMonthPickerVisible",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether the month picker is shown beside the day picker or hidden.",
            },
            new ComponentParameter()
            {
                Name = "isOpen",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not this DatePicker is open.",
            },
            new ComponentParameter()
            {
                Name = "isUnderlined",
                Type = "bool",
                DefaultValue = "false",
                Description = "Whether or not the Textfield of the DatePicker is underlined.",
            },
            new ComponentParameter()
            {
                Name = "label",
                Type = "string",
                DefaultValue = "",
                Description = "Label for the DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "onClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when clicking on DatePicker input.",
            },
            new ComponentParameter()
            {
                Name = "onFocusIn",
                Type = "EventCallback<FocusEventArgs>",
                DefaultValue = "",
                Description = "Callback for when focus moves into the DatePicker input.",
            },
            new ComponentParameter()
            {
                Name = "onFocusOut",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when clicking on DatePicker input.",
            },
            new ComponentParameter()
            {
                Name = "onMonthChange",
                Type = "EventCallback<int>",
                DefaultValue = "",
                Description = "Callback for when the month changes.",
            },
            new ComponentParameter()
            {
                Name = "onYearChange",
                Type = "EventCallback<int>",
                DefaultValue = "",
                Description = "Callback for when the year changes.",
            },
            new ComponentParameter()
            {
                Name = "onDateSet",
                Type = "EventCallback<string>",
                DefaultValue = "",
                Description = "Callback for when the date changes.",
            },
            new ComponentParameter()
            {
                Name = "onSelectDate",
                Type = "Func<BitDate, string>",
                DefaultValue = "",
                Description = "Callback for when the on selected date changed.",
            },
            new ComponentParameter()
            {
                Name = "placeholder",
                Type = "string",
                DefaultValue = "Select a date...",
                Description = "Placeholder text for the DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "showMonthPickerAsOverlay",
                Type = "bool",
                DefaultValue = "false",
                Description = "Show month picker on top of date picker when visible.",
            },
            new ComponentParameter()
            {
                Name = "tabIndex",
                Type = "int",
                DefaultValue = "0",
                Description = "The tabIndex of the TextField.",
            },
            new ComponentParameter()
            {
                Name = "value",
                Type = "string",
                DefaultValue = "",
                Description = "The value of DatePicker.",
            },
            new ComponentParameter()
            {
                Name = "calueChanged",
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
                Title = "CalendarType enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "gregorian",
                        Description="Show DatePicker in Gregorian calendar",
                        Value="gregorian = 0",
                    },
                    new EnumItem()
                    {
                        Name= "persian",
                        Description="Show DatePicker in Persian calendar",
                        Value="persian = 1",
                    }
                }
            }
        };

        private string OnDateFormat(BitDate date)
        {
            return $"{date.GetDate()}/{date.GetMonth()}/{date.GetYear()}";
        }

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

        private void SelectFirstDayOfWeek(BitDropDownItem item)
        {
            firstDayOfWeek = (DayOfWeek)int.Parse(item.Value);
        }
    }
}
