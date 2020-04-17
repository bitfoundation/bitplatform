using Bit.ViewModel;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    public class ValueConverter<TSource, TTarget, TParameter> : BindableObject, IMarkupExtension, IValueConverter
    {
        [return: MaybeNull]
        protected virtual TTarget Convert([MaybeNull]TSource value, Type? targetType, [MaybeNull]TParameter parameter, CultureInfo? culture)
        {
            throw new NotImplementedException($"Override {nameof(Convert)} in {GetType().Name} class and provide required implementation there. Do not call base.Convert, it's not required at all.");
        }

        [return: MaybeNull]
        protected virtual TSource ConvertBack([MaybeNull] TTarget value, Type? targetType, [MaybeNull] TParameter parameter, CultureInfo? culture)
        {
            throw new NotImplementedException($"Override {nameof(ConvertBack)} in {GetType().Name} class and provide required implementation there. Do not call base.ConvertBack, it's not required at all.");
        }

        object? IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null && !typeof(TSource).IsClass && Nullable.GetUnderlyingType(typeof(TSource)) == null)
                throw new NotSupportedException($"Source of type {typeof(TSource).Name} may not be null");

            if (parameter is null && !typeof(TParameter).IsClass && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
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

        object? IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null && !typeof(TTarget).IsClass && Nullable.GetUnderlyingType(typeof(TTarget)) == null)
                throw new NotSupportedException($"Source of type {typeof(TSource).Name} may not be null");

            if (parameter is null && !typeof(TParameter).IsClass && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
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

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class ValueConverter<TSource, TTarget> : ValueConverter<TSource, TTarget, object?>
    {

    }

    public class ValueConverter : ValueConverter<object?, object?, object?>
    {

    }
}
