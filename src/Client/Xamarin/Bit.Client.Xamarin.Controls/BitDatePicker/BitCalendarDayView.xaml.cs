using NodaTime;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Bit.View.Controls
{
    public partial class BitCalendarDayView
    {
        public BitCalendarDayView()
        {
            InitializeComponent();
        }

        public static BindableProperty DayProperty = BindableProperty.Create(nameof(Day), typeof(CalendarDay), typeof(BitCalendarDayView), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual CalendarDay? Day
        {
            get { return (CalendarDay?)GetValue(DayProperty); }
            set { SetValue(DayProperty, value); }
        }

        public static BindableProperty SelectDateCommandProperty = BindableProperty.Create(nameof(SelectDateCommand), typeof(ICommand), typeof(BitCalendarDayView), defaultValue: null, defaultBindingMode: BindingMode.OneWay);
        public virtual ICommand? SelectDateCommand
        {
            get { return (ICommand?)GetValue(SelectDateCommandProperty); }
            set { SetValue(SelectDateCommandProperty, value); }
        }

        public static BindableProperty TodayColorProperty = BindableProperty.Create(nameof(TodayColor), typeof(Color), typeof(BitCalendarDayView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color TodayColor
        {
            get { return (Color)GetValue(TodayColorProperty); }
            set { SetValue(TodayColorProperty, value); }
        }

        public static BindableProperty SelectedColorProperty = BindableProperty.Create(nameof(SelectedColor), typeof(Color), typeof(BitCalendarDayView), defaultValue: Color.DeepPink, defaultBindingMode: BindingMode.OneWay);
        public virtual Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(nameof(FontFamily), typeof(string), typeof(BitCalendarDayView), default(string), defaultBindingMode: BindingMode.OneWay);
        public virtual string? FontFamily
        {
            get { return (string?)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }
    }

    public class CalendarDay : INotifyPropertyChanged
    {
        private LocalDate _LocalDate;
        public virtual LocalDate LocalDate
        {
            get => _LocalDate;
            set
            {
                _LocalDate = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalDate)));
            }
        }

        private bool _IsToday;

        public virtual bool IsToday
        {
            get => _IsToday;
            set
            {
                _IsToday = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsToday)));
            }
        }

        private bool _IsSelected;

        public virtual bool IsSelected
        {
            get => _IsSelected;
            set
            {
                _IsSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public virtual event PropertyChangedEventHandler PropertyChanged = default!;
    }
}
