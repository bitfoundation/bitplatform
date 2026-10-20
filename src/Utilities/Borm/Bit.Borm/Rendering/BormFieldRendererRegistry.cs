using Microsoft.AspNetCore.Components;

namespace Bit.Borm;

/// <inheritdoc />
public sealed class BormFieldRendererRegistry : IBormFieldRendererRegistry
{
    private readonly Dictionary<BormFieldType, Type> _byType = new();
    private readonly Dictionary<string, Type> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public BormFieldRendererRegistry()
    {
        // Built-in, dependency-free HTML renderers.
        Register(BormFieldType.Text, typeof(BormTextInput));
        Register(BormFieldType.Email, typeof(BormTextInput));
        Register(BormFieldType.Password, typeof(BormTextInput));
        Register(BormFieldType.Url, typeof(BormTextInput));
        Register(BormFieldType.Tel, typeof(BormTextInput));
        Register(BormFieldType.Search, typeof(BormTextInput));
        Register(BormFieldType.Color, typeof(BormTextInput));
        Register(BormFieldType.Hidden, typeof(BormTextInput));
        Register(BormFieldType.TextArea, typeof(BormTextAreaInput));
        Register(BormFieldType.Integer, typeof(BormNumberInput));
        Register(BormFieldType.Number, typeof(BormNumberInput));
        Register(BormFieldType.Range, typeof(BormRangeInput));
        Register(BormFieldType.Checkbox, typeof(BormCheckboxInput));
        Register(BormFieldType.Select, typeof(BormSelectInput));
        Register(BormFieldType.MultiSelect, typeof(BormMultiSelectInput));
        Register(BormFieldType.Radio, typeof(BormRadioInput));
        Register(BormFieldType.Combobox, typeof(BormComboboxInput));
        Register(BormFieldType.Tags, typeof(BormTagsInput));
        Register(BormFieldType.Date, typeof(BormDateInput));
        Register(BormFieldType.DateTime, typeof(BormDateInput));
        Register(BormFieldType.Time, typeof(BormDateInput));
        Register(BormFieldType.File, typeof(BormFileInput));
        Register(BormFieldType.Static, typeof(BormStaticContent));
    }

    public void Register(BormFieldType type, Type componentType)
    {
        EnsureComponent(componentType);
        _byType[type] = componentType;
    }

    public void RegisterCustom(string key, Type componentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        EnsureComponent(componentType);
        _byKey[key] = componentType;
    }

    public bool TryResolve(BormFieldDefinition field, out Type componentType)
    {
        // An explicit renderer key always wins, so a field can opt out of its type's default widget.
        if (field.CustomRenderer is { Length: > 0 } key && _byKey.TryGetValue(key, out var custom))
        {
            componentType = custom;
            return true;
        }
        return _byType.TryGetValue(field.Type, out componentType!);
    }

    public Type Resolve(BormFieldDefinition field)
    {
        if (TryResolve(field, out var componentType)) return componentType;

        if (field.Type == BormFieldType.Custom)
        {
            // No key at all is a different mistake from an unregistered one, and naming an empty key in
            // the registration hint sends the developer looking for a typo that is not there. The usual
            // way to arrive here is a property no built-in control can edit — a byte[] or a Stream.
            if (string.IsNullOrEmpty(field.CustomRenderer))
                throw new InvalidOperationException(
                    $"Field '{field.Name}' is a custom field with no renderer key, so no component can render it. " +
                    "No built-in control edits a value of this kind (a byte[] or a Stream, say): give it a renderer with " +
                    ".CustomRenderer(\"key\") and register that key with services.AddBorm(r => r.RegisterCustom(\"key\", typeof(MyComponent))).");

            throw new InvalidOperationException(
                $"Field '{field.Name}' uses the custom renderer key '{field.CustomRenderer}', which is not registered. " +
                "Register it with services.AddBorm(r => r.RegisterCustom(\"" + field.CustomRenderer + "\", typeof(MyComponent))).");
        }

        // Any other unmapped type still renders something usable rather than nothing.
        return typeof(BormTextInput);
    }

    private static void EnsureComponent(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"'{componentType}' is not a Blazor component.", nameof(componentType));
    }
}
