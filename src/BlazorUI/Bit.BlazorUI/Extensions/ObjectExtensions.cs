using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

[SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
internal static class ObjectExtensions
{
    internal static T? GetValueFromProperty<T>(this object? obj, string propertyName, T? defaultValue = default)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        if (value is null)
        {
            return defaultValue;
        }

        Type targetType = typeof(T);
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType == typeof(object))
        {
            return (T)value;
        }

        if (targetType == typeof(string))
        {
            value = value.ToString();

            if (value is null) return defaultValue;
        }

        if (value is T tValue)
        {
            return tValue;
        }

        var implicitOp = targetType.GetMethod("op_Implicit", [value.GetType()]);
        if (implicitOp is not null)
        {
            var result = implicitOp.Invoke(null, [value]);
            return result is null ? defaultValue : (T)result;
        }

        return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    internal static void SetValueToProperty(this object? obj, string propertyName, object value)
    {
        obj?.GetType().GetProperty(propertyName)?.SetValue(obj, value);
    }
}
