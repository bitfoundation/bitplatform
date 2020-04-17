using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.View
{
    public class SetPropertyAction : TriggerAction<VisualElement>
    {
        public virtual void InvokeAction(object sender)
        {
            Invoke(sender);
        }

        public VisualElement? TargetElement { get; set; }

        public string Property { get; set; } = default!;

        public object? Value { get; set; }

        public object? Delay { get; set; }

        static readonly Dictionary<Type, ConvertFromInvariantString> xamlTypeConvertersCache = new Dictionary<Type, ConvertFromInvariantString>();

        delegate object ConvertFromInvariantString(string value);

        protected override async void Invoke(VisualElement sender)
        {
            if (Property == null)
                throw new InvalidOperationException($"{nameof(Property)} may not be null");

            if (Value == null)
                throw new InvalidOperationException($"{nameof(Value)} may not be null");

            TargetElement = TargetElement ?? sender ?? throw new ArgumentNullException(nameof(sender));

            Type targetElementType = TargetElement.GetType();

            PropertyInfo? propertyInfo = targetElementType.GetProperty(Property);

            if (propertyInfo == null)
                throw new ArgumentException($"Could not find property {Property} in {targetElementType}");

            Type propertyType = propertyInfo.PropertyType;

            if (propertyType != Value.GetType())
            {
                if (ConvertUsingXamlTypeConverter(propertyInfo, Value, out object? resultValue))
                {
                    Value = resultValue!;
                }
                else
                {
                    Value = ConvertUsingDotNetTypeConverter(propertyType, Value);
                }
            }

            if (Delay != null)
            {
                int delay = Convert.ToInt32(Delay, CultureInfo.InvariantCulture);
                await Task.Delay(delay);
            }

            propertyInfo.SetValue(TargetElement, Value);
        }

        static object? ConvertUsingDotNetTypeConverter(Type destinationType, object input)
        {
            destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            if (destinationType.IsEnum)
            {
                return Enum.Parse(destinationType, (string)input);
            }
            else if (destinationType == typeof(string))
            {
                return Convert.ToString(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(bool))
            {
                return Convert.ToBoolean(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(byte))
            {
                return Convert.ToByte(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(short))
            {
                return Convert.ToInt16(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(int))
            {
                return Convert.ToInt32(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(long))
            {
                return Convert.ToInt64(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(double))
            {
                return Convert.ToDouble(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(float))
            {
                return Convert.ToSingle(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(decimal))
            {
                return Convert.ToDecimal(input, CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Value {input} is not convertible to {destinationType}");
        }

        static bool ConvertUsingXamlTypeConverter(PropertyInfo propertyInfo, object inputValue, [MaybeNullWhen(returnValue: false)] out object? resultValue)
        {
            if (propertyInfo.GetCustomAttribute(typeof(TypeConverterAttribute)) is TypeConverterAttribute propertyAttr)
            {
                resultValue = UseXamlTypeConverter(Type.GetType(propertyAttr.ConverterTypeName)!, inputValue);
                return true;
            }
            else if (propertyInfo.PropertyType.GetCustomAttribute(typeof(TypeConverterAttribute)) is TypeConverterAttribute classAttr)
            {
                resultValue = UseXamlTypeConverter(Type.GetType(classAttr.ConverterTypeName)!, inputValue);
                return true;
            }

            resultValue = null;

            return false;
        }

        static object UseXamlTypeConverter(Type converterType, object value)
        {
            if (!xamlTypeConvertersCache.ContainsKey(converterType))
            {
                object converter = Activator.CreateInstance(converterType)!;

                MethodInfo methodInfo = converterType.GetMethod(nameof(ConvertFromInvariantString))!;

                ConvertFromInvariantString converterDelegate = ((ConvertFromInvariantString)methodInfo
                    .CreateDelegate(typeof(ConvertFromInvariantString), converter));

                xamlTypeConvertersCache.Add(converterType, converterDelegate);
            }

            return xamlTypeConvertersCache[converterType].Invoke((string)value);
        }
    }
}
