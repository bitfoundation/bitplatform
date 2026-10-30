namespace Bit.BlazorUI;

// The built-in items of the item-based inputs (BitDropdownItem<T>, BitChoiceGroupOption<T>, ...) carry their value type
// as a type argument of their own, so nothing ties it to the TValue of the component they are handed to. An item of
// another T is read as the TValue its value is - the BitDropdownItem<Guid> of a dropdown bound to a Guid? - but a T
// whose values are never a TValue would give no item a value at all, and an option of another TValue never reaches
// the component, since it finds its parent through a cascade typed by its own TValue. Either one would render a
// component in which no item ever matches a value, so the components refuse such a pair instead.
internal static class BitItemValueType
{
    // Why a component of the given TValue cannot read the built-in items of the given TItem, or null when it can.
    internal static string? GetMismatch(string componentName, Type itemType, Type valueType, Type genericItemType, Type genericOptionType)
    {
        var itemValueType = Of(itemType, genericItemType);
        if (itemValueType is not null && valueType.IsAssignableFrom(itemValueType) is false)
        {
            return $"The values of {Describe(itemType)} items are never of type {Describe(valueType)}, so none of the items of " +
                   $"this {componentName} would ever match its value. Use {Describe(genericItemType, valueType)} items, " +
                   $"or set the NameSelectors of the {componentName} to read their values.";
        }

        var optionValueType = Of(itemType, genericOptionType);
        if (optionValueType is not null && optionValueType != valueType)
        {
            return $"{Describe(itemType)} options never reach a {componentName} of {Describe(valueType)}, which only takes " +
                   $"the options of its own TValue. Use {Describe(genericOptionType, valueType)} options.";
        }

        return null;
    }

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

    // A type the way it is written in C# (Guid? and BitDropdownItem<Guid> rather than Nullable`1 and BitDropdownItem`1),
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
}
