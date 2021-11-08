using System;
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
        private int displayYear;
        private int currentMonth;
        private int yearRangeFrom;
        private int yearRangeTo;
        private string monthTitle = "";
        private string selectedDate = "";
        private bool showMonthPicker = true;
        private bool isMonthPickerOverlayOnTop = false;
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

        /// <summary>
        /// The first day of the week for your locale
        /// </summary>
        [Parameter] public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;

        /// <summary>
        /// Whether the calendar should show the week number (weeks 1 to 53) before each week row
        /// </summary>
        [Parameter] public bool ShowWeekNumbers { get; set; } = false;

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
            CalloutId = $"DatePicker-Callout{UniqueId}";
            return base.OnInitializedAsync();
        }

        protected override Task OnParametersSetAsync()
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

            return base.OnParametersSetAsync();
        }

        public async Task HandleClick(MouseEventArgs eventArgs)
        {
            if (IsEnabled is false) return;

            IsOpen = true;
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

        public async Task SelectDate(int dayIndex, int weekIndex)
        {
            if (IsEnabled is false) return;
            if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;

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

            IsOpen = false;
            displayYear = currentYear;
            currentMonth = selectedMonth;
            CreateMonthCalendar(currentYear, currentMonth);
            int dayOfWeek = (int)GetDayOfWeek(dayIndex);
            BitDate date = new(currentYear, currentMonth, day, dayOfWeek);
            selectedDate = OnSelectDate is not null ? OnSelectDate.Invoke(date) : GetSelectedDateString(date);
            await ValueChanged.InvokeAsync(selectedDate);
            await OnDateSet.InvokeAsync(selectedDate);
        }

        public async Task HandleMonthChange(bool nextMonth)
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
            await OnMonthChange.InvokeAsync(currentMonth);
        }

        public async Task SelectMonth(int month)
        {
            if (IsEnabled is false) return;

            currentMonth = month;
            currentYear = displayYear;
            CreateMonthCalendar(currentYear, currentMonth);
            await OnMonthChange.InvokeAsync(currentMonth);
            if (ShowMonthPickerAsOverlay is false) return;

            ToggleMonthPickerAsOverlay();
        }

        public async Task SelectYear(int year)
        {
            if (IsEnabled is false) return;

            currentYear = displayYear = year;
            ChangeYearRanges(currentYear - 1);
            CreateMonthCalendar(currentYear, currentMonth);
            await OnYearChange.InvokeAsync(currentYear);

            if (ShowMonthPickerAsOverlay is false) return;

            ToggleBetweenMonthAndYearPicker();
        }

        public void ToggleBetweenMonthAndYearPicker()
        {
            if (IsEnabled is false) return;

            showMonthPicker = !showMonthPicker;
        }

        public async Task HandleYearChange(bool nextYear)
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
            await OnYearChange.InvokeAsync(currentYear);
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
                CreateMonthCalendar();
            }
        }

        private void CreateMonthCalendar()
        {
            currentMonth = calendar?.GetMonth(DateTime.Now) ?? 1;
            currentYear = calendar?.GetYear(DateTime.Now) ?? 1;
            displayYear = currentYear;
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
                        && (int)firstDay.DayOfWeek > dayIndex + GetValueForComparison((int)firstDay.DayOfWeek))
                    {
                        int previousMonth;
                        int previousMonthDaysCount;
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

                        if ((int)firstDay.DayOfWeek > (int)FirstDayOfWeek)
                        {
                            currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (int)firstDay.DayOfWeek + 1 + (int)FirstDayOfWeek;
                        }
                        else
                        {
                            currentMonthCalendar[weekIndex, dayIndex] = previousMonthDaysCount + dayIndex - (7 + (int)firstDay.DayOfWeek - 1 - (int)FirstDayOfWeek);
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

        private string GetSelectedDateString(BitDate date)
        {
            int year = date.GetYear();
            int month = date.GetMonth();
            int day = date.GetDate();
            int dayOfWeek = date.GetDayOfWeek();
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

            return $"{currentMonthCalendar[week, day]}, {calendar?.GetMonthName(month)}, {year}";
        }

        private bool IsDateSelected(int weekIndex, int dayIndex)
        {
            int month = GetCorrectTargetMonth(weekIndex, dayIndex);
            int day = currentMonthCalendar[weekIndex, dayIndex];
            BitDate currentDate = new(currentYear, month, day, (int)GetDayOfWeek(dayIndex));
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
            int dayOfWeek = (int)FirstDayOfWeek + index;
            if (dayOfWeek > 6) dayOfWeek = dayOfWeek - 7;
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
            var date = calendar?.ToDateTime(year, month, day, 0, 0, 0, 0) ?? DateTime.Now;
            int weekNumber = calendar?.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, FirstDayOfWeek) ?? 1;
            return weekNumber;
        }

        private void ToggleMonthPickerAsOverlay()
        {
            isMonthPickerOverlayOnTop = !isMonthPickerOverlayOnTop;
        }

        private int GetValueForComparison(int firstDay) => firstDay > (int)FirstDayOfWeek ? (int)FirstDayOfWeek : (int)FirstDayOfWeek - 7;
    }
}
