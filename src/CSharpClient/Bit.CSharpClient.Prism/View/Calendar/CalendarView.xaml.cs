using NodaTime;
using Rg.Plugins.Popup.Services;
using System;
using System.Globalization;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View.Calendar
{
    public partial class CalendarView : TemplatedView
    {
        public CalendarView()
        {
            InitializeComponent();

            CalendarPopupView = new CalendarPopupView() { };

            CalendarPopupView.SetBinding(CalendarPopupView.CultureProperty, new Binding(nameof(Culture), source: this));
            CalendarPopupView.SetBinding(CalendarPopupView.CalendarSystemProperty, new Binding(nameof(CalendarSystem), source: this));
            CalendarPopupView.SetBinding(CalendarPopupView.SelectedColorProperty, new Binding(nameof(SelectedColor), source: this));
            CalendarPopupView.SetBinding(CalendarPopupView.TodayColorProperty, new Binding(nameof(TodayColor), source: this));
            CalendarPopupView.SetBinding(CalendarPopupView.SelectedDateProperty, new Binding(nameof(SelectedDate), source: this));
            CalendarPopupView.SetBinding(CalendarPopupView.FontFamilyProperty, new Binding(nameof(FontFamily), source: this));
            CalendarPopupView.SetBinding(FlowDirectionProperty, new Binding(nameof(FlowDirection), source: this));

            OpenPopupCommand = new Command(OpenPopup);
        }

        public virtual void OpenPopup()
        {
            PopupNavigation.Instance.PushAsync(CalendarPopupView);
        }

        public virtual CalendarPopupView CalendarPopupView { get; protected set; }
        public virtual ICommand OpenPopupCommand { get; protected set; }

        public static BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(CalendarView), defaultValue: CultureInfo.CurrentUICulture, defaultBindingMode: BindingMode.OneWay);
        [TypeConverter(typeof(StringToCultureInfoConverter))]
        public virtual CultureInfo Culture
        {
            get { return (CultureInfo)GetValue(CultureProperty); }
            set { SetValue(CultureProperty, value); }
        }

        public static BindableProperty CalendarSystemProperty = BindableProperty.Create(nameof(CalendarSystem), typeof(CalendarSystem), typeof(CalendarView), defaultValue: CalendarSystem.Gregorian, defaultBindingMode: BindingMode.OneWay);
        public virtual CalendarSystem CalendarSystem
        {
            get { return (CalendarSystem)GetValue(CalendarSystemProperty); }
            set { SetValue(CalendarSystemProperty, value); }
        }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(CalendarView), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string FontFamily
        {
            get { return (string)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static BindableProperty SelectedDateProperty = BindableProperty.Create(nameof(SelectedDate), typeof(DateTime?), typeof(CalendarView), defaultValue: null, defaultBindingMode: BindingMode.OneWayToSource /*Must be two way*/);
        public virtual DateTime? SelectedDate
        {
            get { return (DateTime?)GetValue(SelectedDateProperty); }
            set { SetValue(SelectedDateProperty, value); }
        }

        public static BindableProperty TodayColorProperty = BindableProperty.Create(nameof(TodayColor), typeof(Color), typeof(CalendarView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color TodayColor
        {
            get { return (Color)GetValue(TodayColorProperty); }
            set { SetValue(TodayColorProperty, value); }
        }

        public static BindableProperty SelectedColorProperty = BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(CalendarView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly BindableProperty DateDisplayFormatProperty = BindableProperty.Create(nameof(DateDisplayFormat), typeof(string), typeof(CalendarView), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual string DateDisplayFormat
        {
            get { return (string)GetValue(DateDisplayFormatProperty); }
            set { SetValue(DateDisplayFormatProperty, value); }
        }

        public virtual string Text { get; set; }

        public virtual string DisplayText
        {
            get
            {
                if (SelectedDate != null)
                {
                    return new LocalDate(SelectedDate.Value.Year, SelectedDate.Value.Month, SelectedDate.Value.Day, CalendarSystem.Gregorian)
                         .WithCalendar(CalendarSystem)
                         .ToString(DateDisplayFormat ?? Culture.DateTimeFormat.ShortDatePattern, Culture);

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
}