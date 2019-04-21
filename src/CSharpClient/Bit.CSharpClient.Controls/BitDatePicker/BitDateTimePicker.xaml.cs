using NodaTime;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View.Controls
{
    public partial class BitDateTimePicker
    {
        public BitDateTimePicker()
        {
            InitializeComponent();

            BitCalendarPopupView = new BitCalendarPopupView() { };

            BitCalendarPopupView.SetBinding(BitCalendarPopupView.CultureProperty, new Binding(nameof(Culture), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.CalendarSystemProperty, new Binding(nameof(CalendarSystem), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.SelectedColorProperty, new Binding(nameof(SelectedColor), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.TodayColorProperty, new Binding(nameof(TodayColor), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.SelectedDateTimeProperty, new Binding(nameof(SelectedDateTime), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.AutoCloseProperty, new Binding(nameof(AutoClose), source: this));
            BitCalendarPopupView.SetBinding(BitCalendarPopupView.ShowTimePickerProperty, new Binding(nameof(ShowTimePicker), source: this));
            BitCalendarPopupView.SetBinding(FlowDirectionProperty, new Binding(nameof(FlowDirection), source: this));

            OpenPopupCommand = new Command(OpenPopup);
        }

        public virtual void OpenPopup()
        {
            Navigation.PushPopupAsync(BitCalendarPopupView);
        }

        public virtual BitCalendarPopupView BitCalendarPopupView { get; protected set; }
        public virtual ICommand OpenPopupCommand { get; protected set; }

        public static BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(BitDateTimePicker), defaultValue: CultureInfo.CurrentUICulture, defaultBindingMode: BindingMode.OneWay);
        [TypeConverter(typeof(StringToCultureInfoConverter))]
        public virtual CultureInfo Culture
        {
            get { return (CultureInfo)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
        }

        public static BindableProperty CalendarSystemProperty = BindableProperty.Create(nameof(CalendarSystem), typeof(CalendarSystem), typeof(BitDateTimePicker), defaultValue: CalendarSystem.Gregorian, defaultBindingMode: BindingMode.OneWay);
        public virtual CalendarSystem CalendarSystem
        {
            get { return (CalendarSystem)GetValue(CalendarSystemProperty); }
            set { SetValue(CalendarSystemProperty, value); }
        }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string FontFamily
        {
            get { return (string)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static BindableProperty SelectedDateTimeProperty = BindableProperty.Create(nameof(SelectedDateTime), typeof(DateTime?), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitDateTimePicker dateTimePicker = (BitDateTimePicker)sender;
            dateTimePicker.RaiseDisplayTextChanged();
        });
        public virtual DateTime? SelectedDateTime
        {
            get { return (DateTime?)GetValue(SelectedDateTimeProperty); }
            set { SetValue(SelectedDateTimeProperty, value); }
        }

        public static BindableProperty TodayColorProperty = BindableProperty.Create(nameof(TodayColor), typeof(Color), typeof(BitDateTimePicker), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color TodayColor
        {
            get { return (Color)GetValue(TodayColorProperty); }
            set { SetValue(TodayColorProperty, value); }
        }

        public static BindableProperty SelectedColorProperty = BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(BitDateTimePicker), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly BindableProperty DateTimeDisplayFormatProperty = BindableProperty.Create(nameof(DateTimeDisplayFormat), typeof(string), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string DateTimeDisplayFormat
        {
            get { return (string)GetValue(DateTimeDisplayFormatProperty); }
            set { SetValue(DateTimeDisplayFormatProperty, value); }
        }

        public static BindableProperty AutoCloseProperty = BindableProperty.Create(nameof(AutoClose), typeof(bool), typeof(BitDateTimePicker), defaultValue: false, defaultBindingMode: BindingMode.OneWay);
        public virtual bool AutoClose
        {
            get { return (bool)GetValue(AutoCloseProperty); }
            set { SetValue(AutoCloseProperty, value); }
        }

        public virtual string Text { get; set; }

        public static BindableProperty ShowTimePickerProperty = BindableProperty.Create(nameof(ShowTimePicker), typeof(bool), typeof(BitCalendarPopupView), defaultValue: true, defaultBindingMode: BindingMode.OneWay);
        public virtual bool ShowTimePicker
        {
            get { return (bool)GetValue(ShowTimePickerProperty); }
            set { SetValue(ShowTimePickerProperty, value); }
        }

        protected void RaiseDisplayTextChanged()
        {
            OnPropertyChanged((nameof(DisplayText)));
        }

        public virtual string DisplayText
        {
            get
            {
                if (SelectedDateTime != null)
                {
                    DateTime selectedDateTime = SelectedDateTime.Value;
                    return new LocalDateTime(selectedDateTime.Year, selectedDateTime.Month, selectedDateTime.Day, selectedDateTime.Hour, selectedDateTime.Minute, selectedDateTime.Second, CalendarSystem.Gregorian)
                         .WithCalendar(CalendarSystem)
                         .ToDateTimeUnspecified()
                         .ToString(DateTimeDisplayFormat ?? Culture.DateTimeFormat.FullDateTimePattern, Culture);
                }
                else
                {
                    return Text;
                }
            }
        }
    }

    public class StringToCultureInfoConverter : TypeConverter
    {
        public override object ConvertFromInvariantString(string value)
        {
            return CultureInfoProvider.Current.GetCultureInfo(value);
        }
    }

    public class CultureInfoProvider
    {
        private readonly Dictionary<string, CultureInfo> _CultureInfoCache;

        public CultureInfoProvider()
        {
            CultureInfo Fa = new CultureInfo("Fa");

            Fa.DateTimeFormat.MonthNames = Fa.DateTimeFormat.AbbreviatedMonthGenitiveNames = Fa.DateTimeFormat.MonthGenitiveNames = Fa.DateTimeFormat.AbbreviatedMonthNames = new[] { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند", "" };

            CultureInfo Ar = new CultureInfo("Ar");

            Ar.DateTimeFormat.DayNames = Ar.DateTimeFormat.AbbreviatedDayNames = Ar.DateTimeFormat.ShortestDayNames = new[] { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };

            Ar.DateTimeFormat.YearMonthPattern = Fa.DateTimeFormat.YearMonthPattern = "MMMM yyyy";

            _CultureInfoCache = new Dictionary<string, CultureInfo>
            {
                { "Fa", Fa },
                { "Ar", Ar }
            };
        }

        public virtual CultureInfo GetCultureInfo(string cultureName)
        {
            return _CultureInfoCache.ContainsKey(cultureName) ? _CultureInfoCache[cultureName] : CultureInfo.GetCultureInfo(cultureName);
        }

        private static CultureInfoProvider _Current;

        public static CultureInfoProvider Current
        {
            get
            {
                _Current = _Current ?? new CultureInfoProvider { };
                return _Current;
            }
            set
            {
                if (_Current != null)
                    throw new InvalidOperationException($"{nameof(Current)} {nameof(CultureInfoProvider)} has been already set.");

                _Current = value ?? throw new InvalidOperationException($"{nameof(Current)} {nameof(CultureInfoProvider)} may not be null.");
            }
        }
    }
}
