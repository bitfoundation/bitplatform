using System.Globalization;

namespace Bit.BlazorUI;

// The built-in items of the item-based inputs (BitDropdownItem<T>, BitChoiceGroupItem<T>) carry their value type as a
// type argument of their own, so nothing ties it to the TValue of the component they are handed to: an id is rarely
// nullable while the property a component is bound to often is. A component that has no NameSelectors reads an item of
// another T the way the NameSelectors read a property of another type (TryConvert), so the BitDropdownItem<Guid> of a
// dropdown of Guid? works as the BitDropdownItem<Guid?> it stands for. An option of another TValue is another story: it
// finds its parent through a cascade typed by its own TValue, so it never reaches the component at all, and a component
// of such options is refused instead of rendering without a single one of them (GetOptionMismatch).
internal static class BitItemValueType
{
    // Why the options of the given TItem never reach a component of the given TValue, or null when they do.
    internal static string? GetOptionMismatch(string componentName, Type itemType, Type valueType, Type genericOptionType)
    {
        var optionValueType = Of(itemType, genericOptionType);
        if (optionValueType is null || optionValueType == valueType) return null;

        return $"{Describe(itemType)} options never reach a {componentName} of {Describe(valueType)}, which only takes " +
               $"the options of its own TValue. Use {Describe(genericOptionType, valueType)} options.";
    }

    // Whether the built-in items of the given TItem are of another T that has no null to hold for a TValue that does: the
    // BitDropdownItem<Guid> of a dropdown of Guid?, whose new item of the Dynamic mode has nothing to start with.
    internal static bool HoldsNoNullOf(Type itemType, Type valueType, Type genericItemType)
    {
        var itemValueType = Of(itemType, genericItemType);
        if (itemValueType is null || itemValueType == valueType) return false;

        return CanBeNull(valueType) && CanBeNull(itemValueType) is false;
    }

    // A value of one type as another, the way GetValueFromProperty converts the property a NameSelectors names: as the
    // value it is, as text, or between the primitives. It never throws, since it runs as a component renders: a value
    // that is none of those is no value at all (false), which is also what the null of a Guid? is to a Guid.
    internal static bool TryConvert<TFrom, TTo>(TFrom? value, out TTo? result)
    {
        // A Guid as a Guid?, a Guid? that holds one as a Guid, a string as an object.
        if (value is TTo same)
        {
            result = same;
            return true;
        }

        result = default;

        if (value is null) return default(TTo) is null;

        var type = Nullable.GetUnderlyingType(typeof(TTo)) ?? typeof(TTo);

        if (type == typeof(string))
        {
            result = (TTo?)(object?)value.ToString();
            return true;
        }

        // An int as a long, as long as the value fits the type: the text of a number is converted, a text that is none
        // is not, and neither is a number too big for the type.
        if (value is IConvertible && Type.GetTypeCode(type) is not (TypeCode.Object or TypeCode.Empty or TypeCode.DBNull))
        {
            try
            {
                result = (TTo?)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { }
        }

        return false;
    }

    // The value of an item read as the TValue of a component of another T (see TryConvert), kept in the item: a value type
    // is boxed on its way from one type to the other, and a component reads the value of every item time and again (each
    // render, each lookup of the selection), so the box is made once for each value an item holds rather than on every
    // read. A read is replaced and never changed, so the items of a list that several renderers read at once (a static
    // list of a server app) never hand one of them a value that was read for another.
    internal static TTo? Read<TFrom, TTo>(TFrom? value, ref ValueRead<TFrom>? lastRead)
    {
        var read = lastRead;

        if (read is null || read.Type != typeof(TTo) || IsSame(read.Source, value) is false)
        {
            lastRead = read = new(value, typeof(TTo), TryConvert(value, out TTo? converted) ? converted : default);
        }

        return (TTo?)read.Value;
    }

    // A value that reads as the one already read: an equal value of a value type, the very same instance of a reference
    // type (another instance may well be equal and still be another value to the component's ValueComparer).
    private static bool IsSame<T>(T? read, T? value) => typeof(T).IsValueType ? EqualityComparer<T?>.Default.Equals(read, value) : ReferenceEquals(read, value);

    private static bool CanBeNull(Type type) => type.IsValueType is false || Nullable.GetUnderlyingType(type) is not null;

    // The T of an item type that is, or derives from, the given generic item type (BitDropdownItem<> and the like).
    private static Type? Of(Type itemType, Type genericItemType)
    {
        for (var type = itemType; type is not null; type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericItemType)
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    // A type the way it is written in C# (Guid? and BitDropdownOption<Guid> rather than Nullable`1 and BitDropdownOption`1),
    // so that a message names the very type to write.
    private static string Describe(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlyingType) return $"{Describe(underlyingType)}?";

        if (type.IsGenericType is false) return type.Name;

        return Describe(type.GetGenericTypeDefinition(), type.GetGenericArguments());
    }

    private static string Describe(Type genericTypeDefinition, params Type[] typeArguments)
    {
        var name = genericTypeDefinition.Name;
        var tick = name.IndexOf('`');

        return $"{(tick < 0 ? name : name[..tick])}<{string.Join(", ", typeArguments.Select(Describe))}>";
    }

    // A value as it was read: the value of the item, the type it was read as and what it reads as.
    internal sealed class ValueRead<TFrom>(TFrom? source, Type type, object? value)
    {
        internal TFrom? Source { get; } = source;

        internal Type Type { get; } = type;

        internal object? Value { get; } = value;
    }
}
