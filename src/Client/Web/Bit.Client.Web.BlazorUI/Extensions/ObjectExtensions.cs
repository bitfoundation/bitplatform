﻿using System;
using System.Globalization;

namespace Bit.Client.Web.BlazorUI
{
    public static class ObjectExtensions
    {
        public static string? GetValueFromProperty(this object? obj, string propertyName)
        {
            return obj?.GetType().GetProperty(propertyName)?.GetValue(obj)?.ToString();
        }

        public static BitIconName? GetBitIconNameFromProperty(this object? obj, string propertyName)
        {
            var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

            if (value is null) return null;

            if (value is BitIconName bitIconName)
                return bitIconName;

            return null;
        }

        public static T? GetValueFromProperty<T>(this object? obj, string propertyName)
        {
            var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

            if (value is null)
            {
                return default;
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T? GetValueFromProperty<T>(this object? obj, string propertyName, T? defaultValue)
        {
            var value = obj?.GetType().GetProperty(propertyName)?.GetValue(obj);

            if (value is null)
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T? ConvertTo<T>(this object? obj)
        {
            if (obj is null) return default;

            if (typeof(T).IsEnum)
            {
                if (obj.ToString().HasNoValue()) return default;

                return (T)Enum.Parse(typeof(T), obj.ToString()!, true);
            }
            else
            {
                return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
            }
        }
    }
}
