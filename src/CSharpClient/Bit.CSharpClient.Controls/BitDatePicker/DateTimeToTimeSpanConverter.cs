using System;
using System.Globalization;
using Xamarin.Forms;

namespace Bit.View.Controls
{
    internal class DateTimeToTimeSpanConverter : IValueConverter
    {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second);
            }

            return null;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan && parameter is BitCalendarPopupView calendarPopupView && calendarPopupView.SelectedDateTime.HasValue)
            {
                DateTime selectedDateTime = calendarPopupView.SelectedDateTime.Value;
                DateTime dateTime = new DateTime(selectedDateTime.Year, selectedDateTime.Month, selectedDateTime.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
                return dateTime;
            }
            return null;
        }
    }
}
