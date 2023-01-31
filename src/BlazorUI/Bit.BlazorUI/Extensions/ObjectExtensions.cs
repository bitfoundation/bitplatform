using System;
using System.Globalization;

namespace Bit.BlazorUI;

internal static class ObjectExtensions
{
    internal static object? GetValueAsObjectFromProperty(this object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName)?.GetValue(obj);
    }

    internal static BitIconName? GetBitIconNameFromProperty(this object? obj, string propertyName)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        if (value is null) return null;

        if (value is BitIconName bitIconName)
            return bitIconName;

        return null;
    }

    internal static T? GetValueFromProperty<T>(this object? obj, string propertyName)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        if (value is null)
        {
            return default;
        }

        Type targetType = typeof(T);
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    internal static T? GetValueFromProperty<T>(this object? obj, string propertyName, T? defaultValue)
    {
        var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

        if (value is null)
        {
            return defaultValue;
        }

        Type targetType = typeof(T);
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    internal static T? ConvertTo<T>(this object? obj)
    {
        if (obj is null) return default;

        if (typeof(T).IsEnum)
        {
            if (obj.ToString().HasNoValue()) return default;

            return (T)Enum.Parse(typeof(T), obj.ToString()!, true);
        }
        else
        {
            Type targetType = typeof(T);
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            return (T)Convert.ChangeType(obj, targetType, CultureInfo.InvariantCulture);
        }
    }

    internal static void SetValueToProperty(this object? obj, string propertyName, object value)
    {
        obj?.GetType().GetProperty(propertyName)?.SetValue(obj, value);
    }
}
