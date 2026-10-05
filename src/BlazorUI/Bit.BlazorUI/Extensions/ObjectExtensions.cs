using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Bit.BlazorUI;

[SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
internal static class ObjectExtensions
{
    // These helpers sit on the render path of the item-based components (called one or more times per item
    // per render), so the property lookups are cached; the property set of a type is fixed for the lifetime
    // of the process, so the cache cannot go stale. Misses are cached too (as null): a custom item type
    // without one of the optional properties would otherwise pay the failed lookup on every call.
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> _properties = new();

    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName) =>
        _properties.GetOrAdd((type, propertyName), static key => key.Type.GetProperty(key.Name));

    internal static T? GetValueFromProperty<T>(this object? obj, string propertyName, T? defaultValue = default)
    {
        var value = obj is null ? null : GetPropertyInfo(obj.GetType(), propertyName)?.GetValue(obj);

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

    // The disabled flag of an item read through its name selector. The flag used to be IsEnabled, true by
    // default (#5527): a custom item type that still has only that property would be read as not disabled,
    // every item it marks off quietly selectable, so it is refused with what to change instead.
    internal static bool GetIsDisabledFromProperty(this object? obj, string propertyName)
    {
        if (obj is null) return false;

        var type = obj.GetType();
        if (propertyName == "IsDisabled" &&
            GetPropertyInfo(type, "IsDisabled") is null &&
            GetPropertyInfo(type, "IsEnabled") is not null)
        {
            throw new InvalidOperationException(
                $"The item type '{type.Name}' has an 'IsEnabled' property, which is no longer read: the items' enabled state is now " +
                $"the 'IsDisabled' flag, true for an item that is disabled. Add an 'IsDisabled' property to '{type.Name}', or map " +
                "the existing one through the name selectors: NameSelectors = new() { IsDisabled = { Selector = i => i.IsEnabled is false } }.");
        }

        return obj.GetValueFromProperty(propertyName, false);
    }

    internal static void SetValueToProperty(this object? obj, string propertyName, object value)
    {
        if (obj is null) return;

        var property = GetPropertyInfo(obj.GetType(), propertyName);

        // A custom item type is free to expose a property the component only ever reads - a computed key,
        // an expanded flag driven from elsewhere - so a property that cannot be written to is left alone
        // rather than throwing from the middle of a render.
        if (property?.CanWrite is not true) return;

        property.SetValue(obj, value);
    }
}
