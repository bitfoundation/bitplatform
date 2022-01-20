using Bit.ViewModel;
using System;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Bit.View
{
    public class MultiValueConverter<TTarget, TParameter>
         : BindableObject, IMarkupExtension, IMultiValueConverter
    {
        public virtual TTarget Convert(object[] values, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(Convert)} in {GetType().Name} class and provide required implementation there. Do not call base.Convert, it's not required at all.");
        }

        public virtual object[]? ConvertBack(TTarget value, Type[] targetTypes, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(ConvertBack)} in {GetType().Name} class and provide required implementation there. Do not call base.ConvertBack, it's not required at all.");
        }

        public virtual object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            try
            {
                return Convert(values, targetType, (TParameter)parameter!, culture)!;
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }

        object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is null && typeof(TTarget).IsValueType && Nullable.GetUnderlyingType(typeof(TTarget)) == null)
                throw new NotSupportedException($"Value of type {typeof(TTarget).Name} may not be null");

            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            try
            {
                return ConvertBack((TTarget)value!, targetTypes, (TParameter)parameter!, culture);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TSource14, TSource15, TSource16, TTarget, TParameter>
        : BindableObject, IMarkupExtension, IMultiValueConverter
    {
        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, TSource14 source14, TSource15 source15, TSource16 source16, Type targetType, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(Convert)} in {GetType().Name} class and provide required implementation there. Do not call base.Convert, it's not required at all.");
        }

        public virtual object[]? ConvertBack(TTarget value, Type[] targetTypes, TParameter parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"Override {nameof(ConvertBack)} in {GetType().Name} class and provide required implementation there. Do not call base.ConvertBack, it's not required at all.");
        }

        public virtual object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            TSource GetCheckedCastedValue<TSource>(int index)
            {
                object result = values.ElementAtOrDefault(index);

                if (result is null && typeof(TSource).IsValueType && Nullable.GetUnderlyingType(typeof(TSource)) == null)
                    throw new NotSupportedException($"Value of type {typeof(TSource).Name} may not be null");

                return (TSource)result!;
            }

            try
            {
                return Convert(GetCheckedCastedValue<TSource1>(0),
                    GetCheckedCastedValue<TSource2>(1),
                    GetCheckedCastedValue<TSource3>(2),
                    GetCheckedCastedValue<TSource4>(3),
                    GetCheckedCastedValue<TSource5>(4),
                    GetCheckedCastedValue<TSource6>(5),
                    GetCheckedCastedValue<TSource7>(6),
                    GetCheckedCastedValue<TSource8>(7),
                    GetCheckedCastedValue<TSource9>(8),
                    GetCheckedCastedValue<TSource10>(9),
                    GetCheckedCastedValue<TSource11>(10),
                    GetCheckedCastedValue<TSource12>(11),
                    GetCheckedCastedValue<TSource13>(12),
                    GetCheckedCastedValue<TSource14>(13),
                    GetCheckedCastedValue<TSource15>(14),
                    GetCheckedCastedValue<TSource16>(15),
                    targetType, (TParameter)parameter!, culture)!;
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }

        object[]? IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is null && typeof(TTarget).IsValueType && Nullable.GetUnderlyingType(typeof(TTarget)) == null)
                throw new NotSupportedException($"Value of type {typeof(TTarget).Name} may not be null");

            if (parameter is null && typeof(TParameter).IsValueType && Nullable.GetUnderlyingType(typeof(TParameter)) == null)
                throw new NotSupportedException($"Parameter of type {typeof(TParameter).Name} may not be null");

            try
            {
                return ConvertBack((TTarget)value!, targetTypes, (TParameter)parameter!, culture);
            }
            catch (Exception exp)
            {
                BitExceptionHandler.Current.OnExceptionReceived(exp);
                return default;
            }
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TSource14, TSource15, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TSource14, TSource15, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, TSource14 source14, TSource15 source15, object source16, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14, source15, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, TSource14 source14, TSource15 source15, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14, source15, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TSource14, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TSource14, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, TSource14 source14, object source15, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, TSource14 source14, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, source14, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TSource13, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, object source14, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, TSource13 source13, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, source13, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TSource12, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, object source13, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, TSource12 source12, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, source12, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TSource11, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, object source12, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, TSource11 source11, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, source11, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, object source11, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, object source10, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, source9, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, object source9, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, source8, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, source8, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, object source8, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, source7, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, source7, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, object source7, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, source6, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, source6, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TSource5, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, object source6, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, source5, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, source5, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TSource4, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, TSource4, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, object source5, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, source4, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, source4, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TSource3, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, TSource3, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, object source4, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, source3, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, TSource3 source3, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, source3, null, targetType, parameter, culture);
        }
    }

    public class MultiValueConverter<TSource1, TSource2, TTarget, TParameter>
        : MultiValueConverter<TSource1, TSource2, object, TTarget, TParameter>
    {
        public sealed override TTarget Convert(TSource1 source1, TSource2 source2, object source3, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return Convert(source1, source2, targetType, parameter, culture);
        }

        public virtual TTarget Convert(TSource1 source1, TSource2 source2, Type targetType, TParameter parameter, CultureInfo culture)
        {
            return base.Convert(source1, source2, null, targetType, parameter, culture);
        }
    }
}
