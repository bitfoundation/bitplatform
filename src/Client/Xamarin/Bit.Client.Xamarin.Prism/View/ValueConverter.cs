using Bit.ViewModel;
using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    public class ValueConverter<TSource, TTarget, TParameter>
        : BindableObject, IMarkupExtension, IValueConverter
    {
        public virtual TTarget Convert(TSource value, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(Convert)} in {GetType().Name} class and provide required implementation there. Do not call base.Convert, it's not required at all.");
        }

        public virtual TSource ConvertBack(TTarget value, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(ConvertBack)} in {GetType().Name} class and provide required implementation there. Do not call base.ConvertBack, it's not required at all.");
        }

        object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null && typeof(TSource).IsValueType && Nullable.GetUnderlyingType(typeof(TSource)) == null)
                throw new NotSupportedException($"Value of type {typeof(TSource).Name} may not be null");

            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            try
            {
                return Convert((TSource)value!, targetType, (TParameter)parameter!, culture);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }

        object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is null && typeof(TTarget).IsValueType && Nullable.GetUnderlyingType(typeof(TTarget)) == null)
                throw new NotSupportedException($"Value of type {typeof(TTarget).Name} may not be null");

            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            try
            {
                return ConvertBack((TTarget)value!, targetType, (TParameter)parameter!, culture);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }

        public virtual object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class ValueConverter<TSource, TTarget>
        : ValueConverter<TSource, TTarget, object?>
    {

    }

    public class ValueConverter
        : ValueConverter<object?, object?, object?>
    {

    }
}
