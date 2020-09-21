using NodaTime;
using Rg.Plugins.Popup.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Bit.View.Controls
{
    public partial class BitDateTimePicker
    {
        public BitDateTimePicker()
        {
            InitializeComponent();

            BitDateTimePopupView = new BitDateTimePopupView() { };

            void ApplyBinding<TProp>(BindableProperty bindableProp, Func<BitDateTimePicker, TProp> getter, Action<BitDateTimePicker, TProp> setter)
            {
                BitDateTimePopupView.SetBinding(bindableProp, new TypedBinding<BitDateTimePicker, TProp>(_this => (getter.Invoke(_this), true), setter, new[]
                {
                    new Tuple<Func<BitDateTimePicker, object>, string>(mvm => mvm, bindableProp.PropertyName)
                })
                {
                    Source = this
                });
            }

            ApplyBinding(BitDateTimePopupView.SelectedDateTimeProperty, _this => _this.SelectedDateTime, (_this, val) => _this.SelectedDateTime = val);
            ApplyBinding(BitDateTimePopupView.CultureProperty, _this => _this.Culture, (_this, val) => _this.Culture = val);
            ApplyBinding(BitDateTimePopupView.CalendarSystemProperty, _this => _this.CalendarSystem, (_this, val) => _this.CalendarSystem = val);
            ApplyBinding(BitDateTimePopupView.SelectedColorProperty, _this => _this.SelectedColor, (_this, val) => _this.SelectedColor = val);
            ApplyBinding(BitDateTimePopupView.TodayColorProperty, _this => _this.TodayColor, (_this, val) => _this.TodayColor = val);
            ApplyBinding(BitDateTimePopupView.FontFamilyProperty, _this => _this.FontFamily, (_this, val) => _this.FontFamily = val);
            ApplyBinding(BitDateTimePopupView.AutoCloseProperty, _this => _this.AutoClose, (_this, val) => _this.AutoClose = val);
            ApplyBinding(BitDateTimePopupView.ShowTimePickerProperty, _this => _this.ShowTimePicker, (_this, val) => _this.ShowTimePicker = val);
            ApplyBinding(BitDateTimePopupView.FlowDirectionProperty, _this => _this.FlowDirection, (_this, val) => _this.FlowDirection = val);

            OpenPopupCommand = new Command(OpenPopup);
        }

        public virtual void OpenPopup()
        {
            Navigation.PushPopupAsync(BitDateTimePopupView);
        }

        private BitDateTimePopupView _BitDateTimePopupView = default!;
        public virtual BitDateTimePopupView BitDateTimePopupView
        {
            get => _BitDateTimePopupView;
            protected set
            {
                _BitDateTimePopupView = value;
                OnPropertyChanged(nameof(BitDateTimePopupView));
            }
        }

        private ICommand? _OpenPopupCommand;
        public virtual ICommand? OpenPopupCommand
        {
            get => _OpenPopupCommand;
            protected set
            {
                _OpenPopupCommand = value;
                OnPropertyChanged(nameof(OpenPopupCommand));
            }
        }

        public static BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(BitDateTimePicker), defaultValue: CultureInfo.CurrentUICulture, defaultBindingMode: BindingMode.OneWay);
        [TypeConverter(typeof(StringToCultureInfoConverter))]
        public virtual CultureInfo Culture
        {
            get => (CultureInfo)GetValue(CultureProperty);
            set => SetValue(CultureProperty, value);
        }

        public static BindableProperty CalendarSystemProperty = BindableProperty.Create(nameof(CalendarSystem), typeof(CalendarSystem), typeof(BitDateTimePicker), defaultValue: CalendarSystem.Gregorian, defaultBindingMode: BindingMode.OneWay);
        public virtual CalendarSystem CalendarSystem
        {
            get => (CalendarSystem)GetValue(CalendarSystemProperty);
            set => SetValue(CalendarSystemProperty, value);
        }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string? FontFamily
        {
            get => (string?)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public static BindableProperty SelectedDateTimeProperty = BindableProperty.Create(nameof(SelectedDateTime), typeof(DateTime?), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.TwoWay, propertyChanged: (sender, oldValue, newValue) =>
        {
            BitDateTimePicker dateTimePicker = (BitDateTimePicker)sender;
            dateTimePicker.RaiseDisplayTextChanged();
        });
        public virtual DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        public static BindableProperty TodayColorProperty = BindableProperty.Create(nameof(TodayColor), typeof(Color), typeof(BitDateTimePicker), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color TodayColor
        {
            get => (Color)GetValue(TodayColorProperty);
            set => SetValue(TodayColorProperty, value);
        }

        public static BindableProperty SelectedColorProperty = BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(BitDateTimePicker), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public static readonly BindableProperty DateTimeDisplayFormatProperty = BindableProperty.Create(nameof(DateTimeDisplayFormat), typeof(string), typeof(BitDateTimePicker), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string? DateTimeDisplayFormat
        {
            get => (string?)GetValue(DateTimeDisplayFormatProperty);
            set => SetValue(DateTimeDisplayFormatProperty, value);
        }

        public static BindableProperty AutoCloseProperty = BindableProperty.Create(nameof(AutoClose), typeof(bool), typeof(BitDateTimePicker), defaultValue: false, defaultBindingMode: BindingMode.OneWay);
        public virtual bool AutoClose
        {
            get => (bool)GetValue(AutoCloseProperty);
            set => SetValue(AutoCloseProperty, value);
        }

        private string? _Text;
        public virtual string? Text
        {
            get => _Text;
            set
            {
                _Text = value;
                OnPropertyChanged(nameof(Text));
                OnPropertyChanged(nameof(DisplayText));
            }
        }

        public static BindableProperty ShowTimePickerProperty = BindableProperty.Create(nameof(ShowTimePicker), typeof(bool), typeof(BitDateTimePicker), defaultValue: true, defaultBindingMode: BindingMode.OneWay);
        public virtual bool ShowTimePicker
        {
            get => (bool)GetValue(ShowTimePickerProperty);
            set => SetValue(ShowTimePickerProperty, value);
        }

        protected void RaiseDisplayTextChanged()
        {
            OnPropertyChanged((nameof(DisplayText)));
        }

        public virtual string? DisplayText
        {
            get
            {
                if (SelectedDateTime != null)
                {
                    DateTime selectedDateTime = SelectedDateTime.Value;
                    return new LocalDateTime(selectedDateTime.Year, selectedDateTime.Month, selectedDateTime.Day, selectedDateTime.Hour, selectedDateTime.Minute, selectedDateTime.Second, CalendarSystem.Gregorian)
                         .WithCalendar(CalendarSystem)
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

        private static CultureInfoProvider? _Current;

        public static CultureInfoProvider Current
        {
            get
            {
                _Current ??= new CultureInfoProvider { };
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
