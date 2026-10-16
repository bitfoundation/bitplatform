using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Bit.BlazorUI;

internal static class ObjectExtensions
{
    // These helpers sit on the render path of the item-based components (called one or more times per item
    // per render), so the property lookups are cached; the property set of a type is fixed for the lifetime
    // of the process, so the cache cannot go stale. Misses are cached too (as null): a custom item type
    // without one of the optional properties would otherwise pay the failed lookup on every call.
    private static readonly ConcurrentDictionary<(Type Type, string Name), PropertyInfo?> _properties = new();

    // The item types are read by the name of a property, which the trimmer cannot follow. What keeps those properties is
    // the [DynamicallyAccessedMembers(PublicProperties)] on the TItem of every component that reads its items this way
    // (BitItemPropertyReadersContractTests pins that), so an app's item type keeps its public properties wherever it
    // is handed to one of them.
    [UnconditionalSuppressMessage("Trimming", "IL2080", Justification = "The public properties of the item types are kept by the DynamicallyAccessedMembers annotation on the TItem of every component that reads items by property name.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The public properties of the item types are kept by the DynamicallyAccessedMembers annotation on the TItem of every component that reads items by property name.")]
    private static PropertyInfo? GetPropertyInfo(Type type, string propertyName) =>
        _properties.GetOrAdd((type, propertyName), static key => key.Type.GetProperty(key.Name));

    // Whether the type of an item has a property of that name at all, to tell an item type that has nowhere to keep a
    // value apart from one whose property simply holds the default.
    internal static bool HasProperty(this object? obj, string propertyName)
    {
        return obj is not null && GetPropertyInfo(obj.GetType(), propertyName) is not null;
    }

    // The implicit operator is looked up on the type asked for, which the trimmer cannot see. The one the components
    // rely on - an icon given by its name - is called directly below, so it is kept; any other is an app's own type,
    // whose operator is kept when the app uses it, and a missing one falls back to Convert.ChangeType.
    [UnconditionalSuppressMessage("Trimming", "IL2090", Justification = "BitIconInfo's operator is called directly; another type's implicit operator is kept by the app's own use of it, and Convert.ChangeType is the fallback.")]
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

        if (targetType == typeof(BitIconInfo) && value is string iconName)
        {
            return (BitIconInfo?)iconName is T icon ? icon : defaultValue;
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
        if (obj is null) return;

        var property = GetPropertyInfo(obj.GetType(), propertyName);

        // A custom item type is free to expose a property the component only ever reads - a computed key,
        // an expanded flag driven from elsewhere - so a property that cannot be written to is left alone
        // rather than throwing from the middle of a render.
        if (property?.CanWrite is not true) return;

        property.SetValue(obj, value);
    }
}
