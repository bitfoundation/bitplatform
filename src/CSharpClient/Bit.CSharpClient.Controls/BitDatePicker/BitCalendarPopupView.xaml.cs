using NodaTime;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]

namespace Bit.View.Controls
{
    [IgnoreMeInNavigationStack]
    public partial class BitCalendarPopupView
    {
        public BitCalendarPopupView()
        {
            InitializeComponent();

            SelectDateTimeCommand = new Command<CalendarDay>(SyncViewModelWithView, selectedDateTime => selectedDateTime != null);

            ShowNextMonthCommand = new Command(ShowNextMonth);

            ShowPreviousMonthCommand = new Command(ShowPreviousMonth);

            CurrentDay = new LocalDate(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, CalendarSystem.Gregorian);

            CalcCurrentMonthDays();
        }

        public virtual void ShowNextMonth()
        {
            CurrentDay = LocalDate.Add(CurrentDay, Period.FromMonths(1));
            CalcCurrentMonthDays();
        }

        public virtual void ShowPreviousMonth()
        {
            CurrentDay = LocalDate.Subtract(CurrentDay, Period.FromMonths(1));
            CalcCurrentMonthDays();
        }

        protected virtual void SyncViewModelWithView(CalendarDay selectedDay)
        {
            foreach (CalendarDay day in Days)
            {
                if (day == null)
                    continue;

                bool isSelected = day == selectedDay ? true : false;

                if (isSelected)
                {
                    DateTime newSelectedDateTime = day.LocalDate.ToDateTimeUnspecified();
                    SelectedDateTime = new DateTime(newSelectedDateTime.Year, newSelectedDateTime.Month, newSelectedDateTime.Day, SelectedDateTime?.Hour ?? 0, SelectedDateTime?.Minute ?? 0, SelectedDateTime?.Second ?? 0);
                }
            }

            if (AutoClose == true)
                Navigation.PopPopupAsync();
        }

        protected virtual void SyncViewWithViewModel()
        {
            foreach (CalendarDay day in Days)
            {
                if (day == null)
                    continue;

                if (SelectedDateTime == null)
                {
                    day.IsSelected = false;
                }
                else
                {
                    day.IsSelected = day.LocalDate.ToDateTimeUnspecified().Date == SelectedDateTime.Value.Date ? true : false;
                }
            }

            if (AutoClose == true)
                Navigation.PopPopupAsync();
        }

        public virtual void CalcCurrentMonthDays()
        {
            DayOfWeek firstDayOfWeekOfCurrentCulture = Culture.DateTimeFormat.FirstDayOfWeek;

            List<DayOfWeekInfo> daysOfWeek = Enum.GetValues(typeof(DayOfWeek))
               .Cast<DayOfWeek>()
               .OrderBy(d => (d - firstDayOfWeekOfCurrentCulture + 7) % 7)
               .Select((d, i) => new DayOfWeekInfo
               {
                   DayOfWeekNumber = i + 1,
                   IsoDayOfWeek = (IsoDayOfWeek)Enum.Parse(typeof(IsoDayOfWeek), d.ToString()),
                   DayOfWeekName = Culture.DateTimeFormat.GetAbbreviatedDayName(d)
               })
               .ToList();

            DaysOfWeekNames = daysOfWeek.Select(d => d.DayOfWeekName).ToArray();

            if (CalendarSystem != CalendarSystemProperty.DefaultValue)
                CurrentDay = CurrentDay.WithCalendar(CalendarSystem);

            CalendarTitle = CurrentDay.ToString(Culture.DateTimeFormat.YearMonthPattern, Culture);

            int thisMonthDaysCount = CurrentDay.Calendar.GetDaysInMonth(CurrentDay.Year, CurrentDay.Month);

            LocalDate firstDayOfMonth = CurrentDay;

            while (firstDayOfMonth.Day != 1)
            {
                firstDayOfMonth = LocalDate.Subtract(firstDayOfMonth, Period.FromDays(1));
            }

            DayOfWeekInfo firstDayOfMonthDayOfWeek = daysOfWeek.Single(d => d.IsoDayOfWeek == firstDayOfMonth.DayOfWeek);

            int prevMonthDaysInCurrentMonthView = (firstDayOfMonthDayOfWeek.DayOfWeekNumber - 1);
            int nextMonthDaysInCurrentMonthView = 42 - thisMonthDaysCount - prevMonthDaysInCurrentMonthView;

            Days = new List<CalendarDay>(42);

            LocalDate today = new LocalDate(DateTimeOffset.Now.Year, DateTimeOffset.Now.Month, DateTimeOffset.Now.Day, CalendarSystem.Gregorian).WithCalendar(CalendarSystem);

            for (int i = 0; i < prevMonthDaysInCurrentMonthView; i++)
            {
                Days.Add(null);
            }

            for (int i = 1; i <= thisMonthDaysCount; i++)
            {
                Days.Add(new CalendarDay
                {
                    LocalDate = new LocalDate(CurrentDay.Year, CurrentDay.Month, i, CurrentDay.Calendar),
                    IsToday = today.Year == CurrentDay.Year && today.Month == CurrentDay.Month && today.Day == i ? true : false
                });
            }

            for (int i = 0; i < nextMonthDaysInCurrentMonthView; i++)
            {
                Days.Add(null);
            }

            if (Days.Count != 42)
                throw new InvalidOperationException($"{nameof(Days)}'s count is {Days.Count}, but it should be 42");
        }

        public virtual string CalendarTitle { get; protected set; }
        public virtual string[] DaysOfWeekNames { get; protected set; }
        public virtual List<CalendarDay> Days { get; protected set; }
        public virtual LocalDate CurrentDay { get; protected set; }

        public virtual ICommand SelectDateTimeCommand { get; protected set; }
        public virtual ICommand ShowNextMonthCommand { get; protected set; }
        public virtual ICommand ShowPreviousMonthCommand { get; protected set; }

        public static BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(BitCalendarPopupView), defaultValue: CultureInfo.CurrentUICulture, defaultBindingMode: BindingMode.OneWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitCalendarPopupView calendarPopupView = (BitCalendarPopupView)sender;
            calendarPopupView.CalcCurrentMonthDays();
        });
        public virtual CultureInfo Culture
        {
            get { return (CultureInfo)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
        }

        public static BindableProperty CalendarSystemProperty = BindableProperty.Create(nameof(CalendarSystem), typeof(CalendarSystem), typeof(BitCalendarPopupView), defaultValue: CalendarSystem.Gregorian, defaultBindingMode: BindingMode.OneWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitCalendarPopupView calendarPopupView = (BitCalendarPopupView)sender;
            calendarPopupView.CalcCurrentMonthDays();
        });
        public virtual CalendarSystem CalendarSystem
        {
            get { return (CalendarSystem)GetValue(CalendarSystemProperty); }
            set { SetValue(CalendarSystemProperty, value); }
        }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(BitCalendarPopupView), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public string FontFamily
        {
            get { return (string)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static BindableProperty SelectedDateTimeProperty = BindableProperty.Create(nameof(SelectedDateTime), typeof(DateTime?), typeof(BitCalendarPopupView), defaultValue: null, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitCalendarPopupView calendarPopupView = (BitCalendarPopupView)sender;
            calendarPopupView.SyncViewWithViewModel();
        });
        public virtual DateTime? SelectedDateTime
        {
            get { return (DateTime?)GetValue(SelectedDateTimeProperty); }
            set { SetValue(SelectedDateTimeProperty, value); }
        }

        public static BindableProperty TodayColorProperty = BindableProperty.Create(nameof(TodayColor), typeof(Color), typeof(BitCalendarPopupView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color TodayColor
        {
            get { return (Color)GetValue(TodayColorProperty); }
            set { SetValue(TodayColorProperty, value); }
        }

        public static BindableProperty SelectedColorProperty = BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(BitCalendarPopupView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static BindableProperty AutoCloseProperty = BindableProperty.Create(nameof(AutoClose), typeof(bool), typeof(BitCalendarPopupView), defaultValue: false, defaultBindingMode: BindingMode.OneWay);
        public virtual bool AutoClose
        {
            get { return (bool)GetValue(AutoCloseProperty); }
            set { SetValue(AutoCloseProperty, value); }
        }

        public static BindableProperty ShowTimePickerProperty = BindableProperty.Create(nameof(ShowTimePicker), typeof(bool), typeof(BitCalendarPopupView), defaultValue: true, defaultBindingMode: BindingMode.OneWay);
        public virtual bool ShowTimePicker
        {
            get { return (bool)GetValue(ShowTimePickerProperty); }
            set { SetValue(ShowTimePickerProperty, value); }
        }
    }

    internal class DayOfWeekInfo : INotifyPropertyChanged
    {
        public virtual IsoDayOfWeek IsoDayOfWeek { get; set; }

        /// <summary>
        /// Based on current culture.
        /// </summary>
        public virtual string DayOfWeekName { get; set; }

        /// <summary>
        /// Based on current culture.
        /// </summary>
        public virtual int DayOfWeekNumber { get; set; }

        public virtual event PropertyChangedEventHandler PropertyChanged;
    }
}
