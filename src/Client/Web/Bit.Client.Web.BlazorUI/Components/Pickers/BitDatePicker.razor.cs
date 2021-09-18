using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

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
        private int monthLength;
        private int dayOfWeekDifference;
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
        [Parameter] public CalendarType CalendarType { get; set; } = CalendarType.Gregorian;

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

        [Parameter] public Func<BitDate,string> OnSelectDate { get; set; }

        [Parameter] public EventCallback<string> ValueChanged { get; set; }

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

        public async Task HandleDateChoose(int dayOfWeek, int day, int month)
        {
            if (IsEnabled)
            {
                if (ValueHasBeenSet && ValueChanged.HasDelegate is false) return;
                IsOpen = false;
                BitDate date = new(currentYear,month,day,dayOfWeek);
                selectedDate = OnSelectDate is not null ? OnSelectDate.Invoke(date): GetSelectedDateString(date);
                await ValueChanged.InvokeAsync(selectedDate);
                await OnDateSet.InvokeAsync(selectedDate);
            }
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
                await CreateMonthCalendar(currentYear, currentMonth);
                await OnMonthChange.InvokeAsync(currentMonth);
            }
        }

        public async Task HandleMonthChange(int month)
        {
            if (IsEnabled)
            {
                currentMonth = month;
                await CreateMonthCalendar(currentYear, currentMonth);
                await OnMonthChange.InvokeAsync(currentMonth);
            }
        }

        public async Task HandleYearChanged(int year)
        {
            if (IsEnabled)
            {
                currentYear = year;
                ChangeYearRanges(currentYear - 1);
                await CreateMonthCalendar(currentYear, currentMonth);
                await OnYearChange.InvokeAsync(currentYear);
            }
        }

        public async Task HandleMonthsShownChanged(MouseEventArgs eventArgs)
        {
            if (IsEnabled)
            {
                isMonthsShown = !isMonthsShown;
            }
        }

        public async Task HandleYearChanged(bool nextYear)
        {
            if (IsEnabled)
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
        }

        public async Task HandleYearRangeChanged(int fromYear)
        {
            if (IsEnabled)
            {
                ChangeYearRanges(fromYear);
            }
        }

        public async Task HandleGoToToday(MouseEventArgs args)
        {
            if (IsEnabled)
            {
                await CreateMonthCalendar();
            }
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
            monthLength = calendar.GetDaysInMonth(year, month);
            var firstDay = calendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
            var currentDay = 1; 
            dayOfWeekDifference = CalendarType == CalendarType.Persian ? -1 : 0;
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
                    if (currentDay > monthLength && dayIndex != 6)
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
            return calendar.GetDayOfWeekShortName(Enum.Parse<DayOfWeek>(dayOfWeek.ToString()))
                   + " " + calendar.GetMonthShortName(month)
                   + " " + day.ToString().PadLeft(2, '0')
                   + " " + year;
        }
        
        private void ChangeYearRanges(int fromYear)
        {
            yearRangeFrom = fromYear;
            yearRangeTo = fromYear + 11;
        }

        [JSInvokable]
        public void CloseCallout()
        {
            IsOpen = false;
            StateHasChanged();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (IsEnabled && firstRender)
            {
                _ = JSRuntime?.BitDatePickerRegisterOnDocumentClickEvent(this, "CloseCallout");
            }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
