﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.Client.Web.BlazorUI
{
    public partial class BitDatePicker
    {
        private const int DEFAULT_WEEK_COUNT = 6;
        private const int DEFAULT_DAY_COUNT_PER_WEEK = 7;
        private bool isOpen;
        private CultureInfo culture = CultureInfo.CurrentUICulture;
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
        private int[,] currentMonthCalendar = new int[DEFAULT_WEEK_COUNT, DEFAULT_DAY_COUNT_PER_WEEK];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional
        private int currentYear;
        private int displayYear;
        private int currentMonth;
        private int yearRangeFrom;
        private int yearRangeTo;
        private string monthTitle = string.Empty;
        private DateTimeOffset? selectedDate;
        private bool showMonthPicker = true;
        private bool isMonthPickerOverlayOnTop;
        private int monthLength;
        private bool ValueHasBeenSet;

        [Inject] public IJSRuntime? JSRuntime { get; set; }

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
        public DateTimeOffset? Value
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
        /// CultureInfo for the DatePicker
        /// </summary>
        [Parameter] public CultureInfo Culture
        {
            get => culture;
            set
            {
                culture = value;
                ClassBuilder.Reset();
            }
        }

        /// <summary>
        /// FormatDate for the DatePicker
        /// </summary>
        [Parameter] public string FormatDate { get; set; } = "ddd MMM dd yyyy";

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
        /// Callback for when the date changes
        /// </summary>
        [Parameter] public EventCallback<DateTimeOffset?> OnSelectDate { get; set; }

        [Parameter] public EventCallback<DateTimeOffset?> ValueChanged { get; set; }

        /// <summary>
        /// Label for the DatePicker
        /// </summary>
        [Parameter] public string? Label { get; set; }

        /// <summary>
        /// Determines if the DatePicker has a border.
        /// </summary>
        [Parameter] public bool HasBorder { get; set; } = true;

        /// <summary>
        /// Whether or not the Textfield of the DatePicker is underlined.
        /// </summary>
        [Parameter] public bool IsUnderlined { get; set; }

        /// <summary>
        /// The tabIndex of the TextField
        /// </summary>
        [Parameter] public int TabIndex { get; set; }

        /// <summary>
        /// Whether the DatePicker allows input a date string directly or not
        /// </summary>
        [Parameter] public bool AllowTextInput { get; set; }

        /// <summary>
        /// Whether the month picker is shown beside the day picker or hidden.
        /// </summary>
        [Parameter] public bool IsMonthPickerVisible { get; set; } = true;

        /// <summary>
        /// Show month picker on top of date picker when visible.
        /// </summary>
        [Parameter] public bool ShowMonthPickerAsOverlay { get; set; }

        /// <summary>
        /// Whether the calendar should show the week number (weeks 1 to 53) before each week row
        /// </summary>
        [Parameter] public bool ShowWeekNumbers { get; set; }

        public string CalloutId { get; set; } = string.Empty;
        public string OverlayId { get; set; } = string.Empty;
        public string WrapperId { get; set; } = string.Empty;
        public string MonthAndYearId { get; set; } = Guid.NewGuid().ToString();
        public string ActiveDescendantId { get; set; } = Guid.NewGuid().ToString();

        [JSInvokable("CloseCallout")]
        public void CloseCalloutBeforeAnotherCalloutIsOpened()
        {
            IsOpen = false;
        }

        protected override string RootElementClass { get; } = "bit-dtp";

        protected override void RegisterComponentClasses()
        {
            ClassBuilder.Register(() => IsEnabled is false
                ? $"{RootElementClass}-disabled-{VisualClassRegistrar()}" : string.Empty);

            ClassBuilder.Register(() => Culture.TextInfo.IsRightToLeft
                ? $"{RootElementClass}-rtl" : string.Empty);
        }

        protected override Task OnInitializedAsync()
        {
            CalloutId = $"DatePicker-Callout{UniqueId}";
            OverlayId = $"DatePicker-Overlay{UniqueId}";
            WrapperId = $"DatePicker-Wrapper{UniqueId}";
            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
        {
            var dateTime = Value.GetValueOrDefault(DateTimeOffset.Now).DateTime;
            CreateMonthCalendar(dateTime);

            return base.OnParametersSetAsync();
        }

        public async Task HandleClick(MouseEventArgs eventArgs)
        {
            if (IsEnabled is false || JSRuntime is null) return;

            if (ShowMonthPickerAsOverlay)
            {
                isMonthPickerOverlayOnTop = false;
            }

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, isOpen);
            IsOpen = !isOpen;
            displayYear = currentYear;
            await OnClick.InvokeAsync(eventArgs);
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

        public async Task HandleTextInputChanged(string value)
        {
            if (IsEnabled && AllowTextInput && value.HasValue() && DateTime.TryParse(value, Culture, DateTimeStyles.None, out DateTime date))
            {
                var dateTimeOffset = new DateTimeOffset(date);

                if (selectedDate != dateTimeOffset)
                {
                    selectedDate = date;
                    await ValueChanged.InvokeAsync(dateTimeOffset);
                    await OnSelectDate.InvokeAsync(dateTimeOffset);
                }
            }
        }

        public async Task SelectDate(int dayIndex, int weekIndex)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
            if (JSRuntime is null) return;

            int day = currentMonthCalendar[weekIndex, dayIndex];
            int selectedMonth = GetCorrectTargetMonth(weekIndex, dayIndex);
            if (selectedMonth < currentMonth && currentMonth == 12 && IsInCurrentMonth(weekIndex, dayIndex) is false)
            {
                currentYear++;
            }

            if (selectedMonth > currentMonth && currentMonth == 1 && IsInCurrentMonth(weekIndex, dayIndex) is false)
            {
                currentYear--;
            }

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, isOpen);
            IsOpen = false;
            displayYear = currentYear;
            currentMonth = selectedMonth;
            CreateMonthCalendar(currentYear, currentMonth);
            selectedDate = new DateTimeOffset(Culture.Calendar.ToDateTime(currentYear, currentMonth, day, 0, 0, 0, 0));
            await ValueChanged.InvokeAsync(selectedDate);
            await OnSelectDate.InvokeAsync(selectedDate);
        }

        public void HandleMonthChange(bool nextMonth)
        {
            if (IsEnabled is false) return;

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

            displayYear = currentYear;
            CreateMonthCalendar(currentYear, currentMonth);
        }

        public void SelectMonth(int month)
        {
            if (IsEnabled is false) return;

            currentMonth = month;
            currentYear = displayYear;
            CreateMonthCalendar(currentYear, currentMonth);
            if (ShowMonthPickerAsOverlay is false) return;

            ToggleMonthPickerAsOverlay();
        }

        public void SelectYear(int year)
        {
            if (IsEnabled is false) return;

            currentYear = displayYear = year;
            ChangeYearRanges(currentYear - 1);
            CreateMonthCalendar(currentYear, currentMonth);

            if (ShowMonthPickerAsOverlay is false) return;

            ToggleBetweenMonthAndYearPicker();
        }

        public void ToggleBetweenMonthAndYearPicker()
        {
            if (IsEnabled is false) return;

            showMonthPicker = !showMonthPicker;
        }

        public void HandleYearChange(bool nextYear)
        {
            if (IsEnabled is false) return;

            if (nextYear)
            {
                displayYear++;
            }
            else
            {
                displayYear--;
            }

            CreateMonthCalendar(currentYear, currentMonth);
        }

        public void HandleYearRangeChange(int fromYear)
        {
            if (IsEnabled is false) return;

            ChangeYearRanges(fromYear);
        }

        public void HandleGoToToday(MouseEventArgs args)
        {
            if (IsEnabled)
            {
                CreateMonthCalendar(DateTime.Now);
            }
        }

        private void CreateMonthCalendar(DateTime dateTime)
        {
            currentMonth = Culture?.Calendar.GetMonth(dateTime) ?? 1;
            currentYear = Culture?.Calendar.GetYear(dateTime) ?? 1;
            displayYear = currentYear;
            yearRangeFrom = currentYear - 1;
            yearRangeTo = currentYear + 10;
            CreateMonthCalendar(currentYear, currentMonth);
        }

        private void CreateMonthCalendar(int year, int month)
        {
            monthTitle = $"{Culture?.DateTimeFormat.GetMonthName(month) ?? string.Empty} {year}";
            monthLength = Culture?.Calendar.GetDaysInMonth(year, month) ?? 29;
            var firstDay = Culture?.Calendar.ToDateTime(year, month, 1, 0, 0, 0, 0) ?? DateTime.Now;
            var currentDay = 1;
            ResetCalendar();

            var isCalendarEnded = false;
            for (int weekIndex = 0; weekIndex < DEFAULT_WEEK_COUNT; weekIndex++)
            {
                for (int dayIndex = 0; dayIndex < DEFAULT_DAY_COUNT_PER_WEEK; dayIndex++)
                {
                    if (weekIndex == 0
                        && currentDay == 1
                        && (int)firstDay.DayOfWeek > dayIndex + GetValueForComparison((int)firstDay.DayOfWeek))
                    {
                        int previousMonth;
                        int previousMonthDaysCount;
                        if (month - 1 == 0)
                        {
                            previousMonth = 12;
                            previousMonthDaysCount = Culture?.Calendar.GetDaysInMonth(year - 1, previousMonth) ?? 29;
                        }
                        else
                        {
                            previousMonth = month - 1;
                            previousMonthDaysCount = Culture?.Calendar.GetDaysInMonth(year, previousMonth) ?? 29;
                        }

                        var firstDayOfWeek = (int)(Culture?.DateTimeFormat.FirstDayOfWeek ?? DayOfWeek.Sunday);

                        if ((int)firstDay.DayOfWeek > firstDayOfWeek)
                        {
                            currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (int)firstDay.DayOfWeek + 1 + firstDayOfWeek;
                        }
                        else
                        {
                            currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (7 + (int)firstDay.DayOfWeek - 1 - firstDayOfWeek);
                        }
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

        private string? GetSelectedDateString()
        {
            if (Value.HasValue)
            {
                return Value.Value.ToString(FormatDate, Culture);
            }
            else
            {
                return null;
            }
        }

        private void ChangeYearRanges(int fromYear)
        {
            yearRangeFrom = fromYear;
            yearRangeTo = fromYear + 11;
        }

        private async Task CloseCallout()
        {
            if (JSRuntime is null) return;

            var obj = DotNetObjectReference.Create(this);
            await JSRuntime.InvokeVoidAsync("BitDatePicker.toggleDatePickerCallout", obj, UniqueId, CalloutId, OverlayId, isOpen);
            IsOpen = false;
            StateHasChanged();
        }

        private string GetDateElClass(int day, int week)
        {
            var className = string.Empty;
            var todayYear = Culture?.Calendar.GetYear(DateTime.Now) ?? 1;
            var todayMonth = Culture?.Calendar.GetMonth(DateTime.Now) ?? 1;
            var todayDay = Culture?.Calendar.GetDayOfMonth(DateTime.Now) ?? 1;
            var currentDay = currentMonthCalendar[week, day];

            if (IsInCurrentMonth(week, day) is false)
            {
                className += className.Length == 0 ? "date-cell--outside-month" : " date-cell--outside-month";
            }

            if (IsInCurrentMonth(week, day) && todayYear == currentYear && todayMonth == currentMonth && todayDay == currentDay)
            {
                className = "date-cell--today";
            }

            if (IsDateSelected(week, day))
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

            return $"{currentMonthCalendar[week, day]}, {Culture?.DateTimeFormat.GetMonthName(month)}, {year}";
        }

        private bool IsDateSelected(int weekIndex, int dayIndex)
        {
            int month = GetCorrectTargetMonth(weekIndex, dayIndex);
            int day = currentMonthCalendar[weekIndex, dayIndex];
            int year = currentYear;

            if (weekIndex == 0 && day > 20 && month == 12)
            {
                year = displayYear - 1;
            }
            else if (weekIndex == 5 && day > 20 && month == 12 && currentYear > displayYear)
            {
                year = currentYear - 1;
            }
            var currentDate = new DateTimeOffset(Culture.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0));
            return selectedDate is not null && selectedDate == currentDate;
        }

        private bool IsMonthSelected(int month)
        {
            return month == currentMonth;
        }

        private bool IsYearSelected(int year)
        {
            return year == currentYear;
        }

        private bool IsGoTodayDisabeld()
        {
            var todayMonth = Culture?.Calendar.GetMonth(DateTime.Now) ?? 1;
            var todayYear = Culture?.Calendar.GetYear(DateTime.Now) ?? 1;

            if (ShowMonthPickerAsOverlay)
            {
                return (yearRangeFrom == todayYear - 1 && yearRangeTo == todayYear + 10 && todayMonth == currentMonth && todayYear == currentYear);
            }
            else
            {
                return (todayMonth == currentMonth && todayYear == currentYear);
            }
        }

        private DayOfWeek GetDayOfWeek(int index)
        {
            int dayOfWeek = (int)(Culture?.DateTimeFormat.FirstDayOfWeek ?? DayOfWeek.Sunday) + index;
            if (dayOfWeek > 6) dayOfWeek -= 7;
            return (DayOfWeek)dayOfWeek;
        }

        private int GetWeekNumber(int weekIndex)
        {
            int month = GetCorrectTargetMonth(weekIndex, 0);
            int year = currentYear;
            if (IsInCurrentMonth(weekIndex, 0) is false)
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

            int day = currentMonthCalendar[weekIndex, 0];
            var date = Culture?.Calendar.ToDateTime(year, month, day, 0, 0, 0, 0) ?? DateTime.Now;
            return Culture?.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, Culture.DateTimeFormat.FirstDayOfWeek) ?? 1;
        }

        private void ToggleMonthPickerAsOverlay()
        {
            isMonthPickerOverlayOnTop = !isMonthPickerOverlayOnTop;
        }

        private int GetValueForComparison(int firstDay)
        {
            var firstDayOfWeek = (int)(Culture?.DateTimeFormat.FirstDayOfWeek ?? DayOfWeek.Sunday);

            return firstDay > firstDayOfWeek ? firstDayOfWeek : firstDayOfWeek - 7;
        }
    }
}
