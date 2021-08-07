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
        private int yearRangeFrom;
        private int yearRangeTo;
        private string monthTitle = "";
        private string selectedDate = "";
        private bool isMonthsShown = true;
        private DayOfWeek weekStartingDay;

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
        [Parameter] public string Placeholder { get; set; } = "Select a date...";
        [Parameter] public CalendarType CalendarType { get; set; } = CalendarType.Gregorian;

        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }
        [Parameter] public EventCallback<int> OnMonthChange { get; set; }
        [Parameter] public EventCallback<int> OnYearChange { get; set; }
        [Parameter] public EventCallback<string> OnDateChoose { get; set; }

        protected override string RootElementClass { get; } = "bit-dtp";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => IsOpen is false
                ? $"{RootElementClass}-open-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override async Task OnInitializedAsync()
        {
            if (CalendarType == CalendarType.Gregorian)
            {
                calendar = new GregorianCalendar();
                weekStartingDay = DayOfWeek.Sunday;
            }
            else if (CalendarType == CalendarType.Persian)
            {
                calendar = new PersianCalendar();
                weekStartingDay = DayOfWeek.Saturday;
            }
            await CreateMonthCalendar();
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
           // IsOpen = false;
            await OnFocusOut.InvokeAsync(eventArgs);
        }

        public async Task HandleDateChoose(int dayOfWeek, int day, int month)
        {
            IsOpen = false;
            selectedDate = GetSelectedDateString(dayOfWeek, day, month);
            await OnDateChoose.InvokeAsync(selectedDate);
        }

        public async Task HandleMonthChange(bool nextMonth)
        {
            if (nextMonth)
            {
                if (currentMonth + 1 == 13)
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
            await CreateMonthCalendar(currentYear, currentMonth);
            await OnMonthChange.InvokeAsync(currentMonth);
        }

        public async Task HandleMonthChange(int month)
        {
            currentMonth = month;
            await CreateMonthCalendar(currentYear, currentMonth);
            await OnMonthChange.InvokeAsync(currentMonth);
        }

        public async Task HandleYearChanged(int year)
        {
            currentYear = year;
            await CreateMonthCalendar(currentYear, currentMonth);
            await OnYearChange.InvokeAsync(currentYear);
        }

        public async Task HandleMonthsShownChanged(MouseEventArgs eventArgs)
        {
            isMonthsShown = !isMonthsShown;
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

        public async Task HandleYearRangeChanged(int fromYear)
        {
            yearRangeFrom = fromYear;
            yearRangeTo = fromYear + 11;
        }

        public async Task HandleGoToToday(MouseEventArgs args)
        {
            await CreateMonthCalendar();
        }

        private async Task CreateMonthCalendar()
        {
            currentMonth = calendar.GetMonth(DateTime.Now);
            currentYear = calendar.GetYear(DateTime.Now);
            yearRangeFrom = currentYear - 1;
            yearRangeTo = currentYear + 10;
            await CreateMonthCalendar(currentYear, currentMonth);
        }

        private async Task CreateMonthCalendar(int year, int month)
        {
            monthTitle = $"{calendar.GetMonthName(month)} {year}";
            var daysCount = calendar.GetDaysInMonth(year, month);
            var firstDay = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
            var currentDay = 1;
            var dayOfWeekDifference = CalendarType == CalendarType.Persian ? -1 : 0;
            var isCalendarEnded = false;
            monthWeeks = new int[6, 7];
            for (int weekIndex = 0; weekIndex < monthWeeks.GetLength(0); weekIndex++)
            {
                for (int dayIndex = 0; dayIndex < monthWeeks.GetLength(1); dayIndex++)
                {
                    if (weekIndex == 0
                        && currentDay == 1
                        && (int)firstDay.DayOfWeek > dayIndex + dayOfWeekDifference)
                    {
                        var previousMonth = 0;
                        var previousMonthDaysCount = 0;
                        if (month - 1 == 0)
                        {
                            previousMonth = 12;
                            previousMonthDaysCount = calendar.GetDaysInMonth(year - 1, previousMonth);
                        }
                        else
                        {
                            previousMonth = month - 1;
                            previousMonthDaysCount = calendar.GetDaysInMonth(year, previousMonth);
                        }
                        monthWeeks[weekIndex, dayIndex] = previousMonthDaysCount - ((int)firstDay.DayOfWeek - (dayIndex + dayOfWeekDifference)) + 1;
                    }
                    else
                    {
                        if (currentDay > calendar.GetDaysInMonth(year, month))
                        {
                            continue;
                        }
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

        private string GetSelectedDateString(int dayOfWeek, int day, int month)
        {
            return calendar.GetDayOfWeekShortName(Enum.Parse<DayOfWeek>(dayOfWeek.ToString()))
                   + " " + calendar.GetMonthShortName(month)
                   + " " + day.ToString().PadLeft(2, '0')
                   + " " + currentYear;
        }
    }
}
