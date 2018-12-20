using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    public class ValueConverter<TSource, TTarget, TParameter> : IMarkupExtension, IValueConverter
    {
        protected virtual TTarget Convert(TSource value, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected virtual TSource ConvertBack(TTarget value, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null && !typeof(TSource).IsClass && Nullable.GetUnderlyingType(typeof(TSource)) == null)
                throw new NotSupportedException();

            if (parameter is null && !typeof(TParameter).IsClass && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException();

            return Convert((TSource)value, targetType, (TParameter)parameter, culture);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null && !typeof(TTarget).IsClass && Nullable.GetUnderlyingType(typeof(TTarget)) == null)
                throw new NotSupportedException();

            if (parameter is null && !typeof(TParameter).IsClass && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException();

            return ConvertBack((TTarget)value, targetType, (TParameter)parameter, culture);
        }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class ValueConverter<TSource, TTarget> : ValueConverter<TSource, TTarget, object>
    {

    }
}
