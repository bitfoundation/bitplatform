﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDatePicker
    {
        private const int DEFAULT_WEEK_COUNT = 6;
        private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
        private bool isOpen;
        private Calendar? calendar;
        private int[,] currentMonthCalendar = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
        private int currentYear;
        private int currentMonth;
        private int yearRangeFrom;
        private int yearRangeTo;
        private string monthTitle = "";
        private string selectedDate = "";
        private bool showMonthPicker = true;
        private int monthLength;
        private bool ValueHasBeenSet;

        /// <summary>
        /// Whether or not this DatePicker is open
        /// </summary>
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

        [Parameter]
        public string Value
        {
            get => selectedDate;
            set
            {
                if (value == selectedDate) return;
                selectedDate = value;

                _ = ValueChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// GoToToday text for the DatePicker
        /// </summary>
        [Parameter] public string GoToToday { get; set; } = "Go to today";

        /// <summary>
        /// Placeholder text for the DatePicker
        /// </summary>
        [Parameter] public string Placeholder { get; set; } = "Select a date...";

        /// <summary>
        /// Calendar type for the DatePicker
        /// </summary>
        [Parameter] public BitCalendarType CalendarType { get; set; } = BitCalendarType.Gregorian;

        /// <summary>
        /// Callback for when clicking on DatePicker input
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Callback for when focus moves into the DatePicker input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusIn { get; set; }

        /// <summary>
        /// Callback for when focus moves out the DatePicker input
        /// </summary>
        [Parameter] public EventCallback<FocusEventArgs> OnFocusOut { get; set; }

        /// <summary>
        /// Callback for when the month changes
        /// </summary>
        [Parameter] public EventCallback<int> OnMonthChange { get; set; }

        /// <summary>
        /// Callback for when the year changes
        /// </summary>
        [Parameter] public EventCallback<int> OnYearChange { get; set; }

        /// <summary>
        /// Callback for when the date changes
        /// </summary>
        [Parameter] public EventCallback<string> OnDateSet { get; set; }

        [Parameter] public Func<BitDate, string>? OnSelectDate { get; set; }

        [Parameter] public EventCallback<string> ValueChanged { get; set; }

        /// <summary>
        /// Label for the DatePicker
        /// </summary>
        [Parameter] public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Determines if the DatePicker has a border.
        /// </summary>
        [Parameter] public bool HasBorder { get; set; } = true;

        /// <summary>
        /// Whether or not the Textfield of the DatePicker is underlined.
        /// </summary>
        [Parameter] public bool IsUnderlined { get; set; } = false;

        /// <summary>
        /// The tabIndex of the TextField
        /// </summary>
        [Parameter] public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Whether the DatePicker allows input a date string directly or not
        /// </summary>
        [Parameter] public bool AllowTextInput { get; set; } = false;

        /// <summary>
        /// Whether the month picker is shown beside the day picker or hidden.
        /// </summary>
        [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

        /// <summary>
        /// Show month picker on top of date picker when visible.
        /// </summary>
        [Parameter] public bool ShowMonthPickerAsOverlay { get; set; } = false;

        public string CalloutId { get; set; } = string.Empty;
        public string MonthAndYearId { get; set; } = Guid.NewGuid().ToString();
        public string ActiveDescendantId { get; set; } = Guid.NewGuid().ToString();

        protected override string RootElementClass { get; } = "bit-dtp";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);
        }

        protected override Task OnInitializedAsync()
        {
            if (CalendarType == BitCalendarType.Gregorian)
            {
                calendar = new GregorianCalendar();
            }
            else if (CalendarType == BitCalendarType.Persian)
            {
                calendar = new PersianCalendar();
            }

            CreateMonthCalendar();
            CalloutId = $"DatePicker-Callout{UniqueId}";

            return base.OnInitializedAsync();
        }

        public async Task HandleClick(MouseEventArgs eventArgs)
        {
            if (IsEnabled)
            {
                IsOpen = true;
                await OnClick.InvokeAsync(eventArgs);
            }
        }

        public async Task HandleFocusIn(FocusEventArgs eventArgs)
        {
            if (IsEnabled)
            {
                await OnFocusIn.InvokeAsync(eventArgs);
            }
        }

        public async Task HandleFocusOut(FocusEventArgs eventArgs)
        {
            if (IsEnabled)
            {
                await OnFocusOut.InvokeAsync(eventArgs);
            }
        }

        public async Task SelectDate(int dayOfWeek, int day, int week)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

            var selectedMonth = GetCorrectTargetMonth(week, dayOfWeek);
            if (selectedMonth < currentMonth && currentMonth == 12 && IsInCurrentMonth(week, dayOfWeek) is false)
            {
                currentYear++;
            }

            if (selectedMonth > currentMonth && currentMonth == 1 && IsInCurrentMonth(week, dayOfWeek) is false)
            {
                currentYear--;
            }

            IsOpen = false;
            currentMonth = selectedMonth;
            CreateMonthCalendar(currentYear, currentMonth);

            dayOfWeek += dayOfWeekDifference;

            if (dayOfWeek < 0)
            {
                dayOfWeek = 7 + dayOfWeek;
            }

            if (dayOfWeek > 7)
            {
                dayOfWeek = -1 + dayOfWeek;
            }

            BitDate date = new(currentYear, currentMonth, day, dayOfWeek);
            selectedDate = OnSelectDate is not null ? OnSelectDate.Invoke(date) : GetSelectedDateString(date);
            await ValueChanged.InvokeAsync(selectedDate);
            await OnDateSet.InvokeAsync(selectedDate);
        }

        public async Task HandleMonthChange(bool nextMonth)
        {
            if (IsEnabled)
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

                CreateMonthCalendar(currentYear, currentMonth);
                await OnMonthChange.InvokeAsync(currentMonth);
            }
        }

        public async Task HandleMonthChange(int month)
        {
            if (IsEnabled is false) return;

            currentMonth = month;
            CreateMonthCalendar(currentYear, currentMonth);
            await OnMonthChange.InvokeAsync(currentMonth);
        }

        public async Task HandleYearChanged(int year)
        {
            if (IsEnabled is false) return;

            currentYear = year;
            ChangeYearRanges(currentYear - 1);
            CreateMonthCalendar(currentYear, currentMonth);
            await OnYearChange.InvokeAsync(currentYear);
        }

        public void ToggleBetweenMonthAndYearPicker()
        {
            if (IsEnabled is false) return;

            showMonthPicker = !showMonthPicker;
        }

        public async Task HandleYearChanged(bool nextYear)
        {
            if (IsEnabled is false) return;

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

        public void HandleYearRangeChanged(int fromYear)
        {
            if (IsEnabled)
            {
                ChangeYearRanges(fromYear);
            }
        }

        public void HandleGoToToday(MouseEventArgs args)
        {
            if (IsEnabled)
            {
                CreateMonthCalendar();
            }
        }

        private void CreateMonthCalendar()
        {
            currentMonth = calendar?.GetMonth(DateTime.Now) ?? 1;
            currentYear = calendar?.GetYear(DateTime.Now) ?? 1;
            yearRangeFrom = currentYear - 1;
            yearRangeTo = currentYear + 10;
            CreateMonthCalendar(currentYear, currentMonth);
        }

        private void CreateMonthCalendar(int year, int month)
        {
            monthTitle = $"{calendar?.GetMonthName(month) ?? ""} {year}";
            monthLength = calendar?.GetDaysInMonth(year, month) ?? 29;
            var firstDay = calendar?.ToDateTime(year, month, 1, 0, 0, 0, 0) ?? DateTime.Now;
            var currentDay = 1;
            ResetCalendar();

            var isCalendarEnded = false;
            for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
            {
                for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
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
                            previousMonthDaysCount = calendar?.GetDaysInMonth(year - 1, previousMonth) ?? 29;
                        }
                        else
                        {
                            previousMonth = month - 1;
                            previousMonthDaysCount = calendar?.GetDaysInMonth(year, previousMonth) ?? 29;
                        }
                        currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount - ((int)firstDay.DayOfWeek - (dayIndex + dayOfWeekDifference)) + 1;
                    }
                    else
                    {
                        if (currentDay <= monthLength)
                        {
                            currentMonthCalendar[weekIndex, dayIndex] = currentDay;
                            currentDay++;
                        }
                    }

                    if (currentDay > monthLength)
                    {
                        currentDay = 1;
                        isCalendarEnded = true;
                    }
                }

                if (isCalendarEnded)
                {
                    break;
                }
            }
        }

        private void ResetCalendar()
        {
            for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
            {
                for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
                {
                    currentMonthCalendar[weekIndex, dayIndex] = 0;
                }
            }
        }

        private string GetSelectedDateString(BitDate date)
        {
            int year = date.GetYear();
            int month = date.GetMonth();
            int day = date.GetDate();
            int dayOfWeek = date.GetDayOfWeek();
            if (dayOfWeek < 0)
            {
                dayOfWeek = 7 + dayOfWeek;
            }

            if (dayOfWeek > 7)
            {
                dayOfWeek = -1 + dayOfWeek;
            }

            return calendar?.GetDayOfWeekShortName(Enum.Parse<DayOfWeek>(dayOfWeek.ToString()))
                   + " " + calendar?.GetMonthShortName(month)
                   + " " + day.ToString().PadLeft(2, '0')
                   + " " + year;
        }

        private void ChangeYearRanges(int fromYear)
        {
            yearRangeFrom = fromYear;
            yearRangeTo = fromYear + 11;
        }

        private void CloseCallout()
        {
            IsOpen = false;
            StateHasChanged();
        }

        private string GetDateElClass(int day, int week)
        {
            var className = "";
            var todayYear = calendar?.GetYear(DateTime.Now) ?? 1;
            var todayMonth = calendar?.GetMonth(DateTime.Now) ?? 1;
            var todayDay = calendar?.GetDayOfMonth(DateTime.Now) ?? 1;
            var currentDay = currentMonthCalendar[week, day];

            if (IsInCurrentMonth(week, day) is false)
            {
                className += className.Length == 0 ? "date-cell--outside-month" : " date-cell--outside-month";
            }

            if (IsInCurrentMonth(week, day) && todayYear == currentYear && todayMonth == currentMonth && todayDay == currentDay)
            {
                className = "date-cell--today";
            }

            if (IsDateSelected(week, day, currentDay))
            {
                className += className.Length == 0 ? "date-cell--selected" : " date-cell--selected";
            }

            return className;
        }

        private bool IsInCurrentMonth(int week, int day)
        {
            if ((week == 0 || week == 1) && currentMonthCalendar[week, day] > 20) return false;
            if ((week == 4 || week == 5) && currentMonthCalendar[week, day] < 7) return false;
            return true;
        }

        private int GetCorrectTargetMonth(int week, int day)
        {
            int month = currentMonth;
            if (IsInCurrentMonth(week, day) is false)
            {
                if (week >= 4)
                {
                    month = currentMonth + 1 == 13 ? 1 : currentMonth + 1;
                }
                else
                {
                    month = currentMonth - 1 == 0 ? 12 : currentMonth - 1;
                }
            }

            return month;
        }

        private string GetDateAriaLabel(int week, int day)
        {
            int month = GetCorrectTargetMonth(week, day);
            int year = currentYear;
            if (IsInCurrentMonth(week, day) is false)
            {
                if (currentMonth == 12 && month == 1)
                {
                    year++;
                }
                else if (currentMonth == 1 && month == 12)
                {
                    year--;
                }
            }

            return $"{currentMonthCalendar[week, day]}, {calendar?.GetMonthName(month)}, {year}";
        }

        private bool IsDateSelected(int week, int day, int currentDay)
        {
            int month = GetCorrectTargetMonth(week, day);
            BitDate currentDate = new(currentYear, month, currentDay, day + dayOfWeekDifference);
            if (selectedDate.HasValue() && selectedDate == GetSelectedDateString(currentDate)) return true;

            return false;
        }

        private bool IsMonthSelected(int month)
        {
            return (month == currentMonth);
        }

        private bool IsYearSelected(int year)
        {
            return (year == currentYear);
        }

        private bool IsGoTodayDisabeld()
        {
            var todayMonth = calendar?.GetMonth(DateTime.Now) ?? 1;
            var todayYear = calendar?.GetYear(DateTime.Now) ?? 1;
            return (todayMonth == currentMonth && todayYear == currentYear);
        }

        private int dayOfWeekDifference => CalendarType == BitCalendarType.Persian ? -1 : 0;
    }
}
