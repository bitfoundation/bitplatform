using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDatePicker
    {
        private bool isOpen;
        private Calendar calendar;
        private int[,] monthWeeks = new int[6, 7];
        private int currentYear;
        private int currentMonth;
        private string monthTitle = "July 2021";

        [Parameter]
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                ClassBuilder.Reset();
            }
        }

        [Parameter] public string GoToToday { get; set; } = "Go to today";
        [Parameter] public CalendarType CalendarType { get; set; } = CalendarType.Gregorian;

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }
        [Parameter] public EventCallback<int> OnMonthChange { get; set; }
        [Parameter] public EventCallback<int> OnYearChange { get; set; }

        protected override string RootElementClass { get; } = "bit-dtp";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsOpen is false
                ? $"{RootElementClass}-open-{VisualClassRegistrar()}" : string.Empty);
        }

        protected async override Task OnInitializedAsync()
        {
            if (CalendarType == CalendarType.Gregorian)
            {
                calendar = new GregorianCalendar();
            }
            else if (CalendarType == CalendarType.Persian)
            {
                calendar = new PersianCalendar();
            }
            CreateMonthCalendar();
        }

        public async Task HandleClick(MouseEventArgs eventArgs)
        {
            IsOpen = true;
            await OnClick.InvokeAsync(eventArgs);
        }
        public async Task HandleFocusIn(FocusEventArgs eventArgs)
        {
            await OnFocusIn.InvokeAsync(eventArgs);
        }
        public async Task HandleFocusOut(FocusEventArgs eventArgs)
        {
            //   IsOpen = false;
            await OnFocusOut.InvokeAsync(eventArgs);
        }

        public async Task HandleMonthChanged(bool nextMonth)
        {
            if (nextMonth)
            {
                if (currentMonth + 1 == 0)
                {
                    currentYear++;
                    currentMonth = 1;
                }
                else
                {
                    currentMonth++;
                }
            }
            else
            {
                if (currentMonth - 1 == 0)
                {
                    currentYear--;
                    currentMonth = 12;
                }
                else
                {
                    currentMonth--;
                }
            }
            CreateMonthCalendar(currentYear, currentMonth);
            await OnMonthChange.InvokeAsync(currentMonth);
        }

        public async Task HandleYearChanged(bool nextYear)
        {
            if (nextYear)
            {
                currentYear++;
            }
            else
            {
                currentYear--;
            }
            CreateMonthCalendar(currentYear, currentMonth);
            await OnYearChange.InvokeAsync(currentYear);
        }

        private void CreateMonthCalendar()
        {
            currentMonth = calendar.GetMonth(DateTime.Now);
            currentYear = calendar.GetYear(DateTime.Now);
            CreateMonthCalendar(currentYear, currentMonth);
        }

        private void CreateMonthCalendar(int year, int month)
        {
            var daysCount = calendar.GetDaysInMonth(year, month);
            var firstDay = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
            var currentDay = 1;
            var dayOfWeekDifference = CalendarType == CalendarType.Persian ? -1 : 0;
            var isCalendarEnded = false;
            for (int weekIndex = 0; weekIndex < monthWeeks.GetLength(0); weekIndex++)
            {
                for (int dayIndex = 0; dayIndex < monthWeeks.GetLength(1); dayIndex++)
                {
                    if (weekIndex == 0
                        && currentDay == 1
                        && (int)firstDay.DayOfWeek > dayIndex + dayOfWeekDifference)
                    {
                        var previousMonthDaysCount = 0;
                        if (month - 1 == 0)
                        {
                            previousMonthDaysCount = calendar.GetDaysInMonth(year - 1, 12);
                        }
                        else
                        {
                            previousMonthDaysCount = calendar.GetDaysInMonth(year, month - 1);
                        }
                        monthWeeks[weekIndex, dayIndex] = previousMonthDaysCount - ((int)firstDay.DayOfWeek - (dayIndex + dayOfWeekDifference)) + 1;
                    }
                    else
                    {
                        monthWeeks[weekIndex, dayIndex] = currentDay;
                        currentDay++;
                    }
                    if (currentDay > daysCount && dayIndex != 6)
                    {
                        currentDay = 1;
                        isCalendarEnded = true;
                    }
                    if (dayIndex == 6)
                    {
                        break;
                    }
                }
                if (isCalendarEnded)
                {
                    break;
                }
            }
        }
    }
}
