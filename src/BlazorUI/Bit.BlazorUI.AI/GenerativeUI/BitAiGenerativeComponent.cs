using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// One bindable property of a registered generative component.
/// </summary>
/// <param name="Name">The parameter name, as the model must spell it.</param>
/// <param name="Type">The CLR type the JSON value is converted to.</param>
/// <param name="Description">What the property means, included in the schema sent to the model.</param>
/// <param name="IsRequired">Whether the model must supply it.</param>
public sealed record BitAiGenerativeProperty(string Name, Type Type, string? Description = null, bool IsRequired = false);

/// <summary>
/// A Blazor component that model output is allowed to instantiate.
/// </summary>
/// <remarks>
/// <para>
/// The registration is the security boundary. A model can only name components that appear here,
/// and can only set the properties listed on them. Everything else in the payload is discarded
/// rather than reflected onto the component.
/// </para>
/// <para>
/// It is also the prompting surface: <see cref="Schema"/> is the JSON Schema you give the model so
/// it knows what it may render and with what.
/// </para>
/// <para>
/// Both halves read the property types by reflection, down to every type they reach, which is why creating a
/// registration is marked as unsafe to trim: an app that trims keeps those types itself.
/// </para>
/// </remarks>
public sealed class BitAiGenerativeComponent
{
    /// <summary>Creates a registration.</summary>
    /// <param name="name">The name the model uses.</param>
    /// <param name="componentType">The Blazor component type.</param>
    /// <param name="properties">The bindable properties.</param>
    /// <param name="description">What the component is for.</param>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.GenerativeDynamicCode)]
    public BitAiGenerativeComponent(
        string name,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        IReadOnlyList<BitAiGenerativeProperty> properties,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(properties);

        Name = name;
        ComponentType = componentType;
        Properties = properties;
        Description = description;
        Schema = BuildSchema(this);
        BindingOptions = BitAiGenerativePropsBinder.ReflectionOptions;
    }

    /// <summary>The name the model uses.</summary>
    public string Name { get; }

    /// <summary>The Blazor component type.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type ComponentType { get; }

    /// <summary>What the component is for, included in the schema.</summary>
    public string? Description { get; }

    /// <summary>The properties the model may set.</summary>
    public IReadOnlyList<BitAiGenerativeProperty> Properties { get; }

    /// <summary>The JSON Schema describing this component to the model.</summary>
    public JsonElement Schema { get; }

    /// <summary>
    /// The JSON settings the props of this component are bound with, whose resolver supplies the metadata of the
    /// property types. Made where the registration is made, which is where an app that trims is told about it.
    /// </summary>
    internal JsonSerializerOptions BindingOptions { get; }

    /// <summary>Finds a property by name, matching case-insensitively as models are inconsistent.</summary>
    /// <param name="name">The property name.</param>
    /// <returns>The property, or <see langword="null"/> when the component has no such property.</returns>
    public BitAiGenerativeProperty? FindProperty(string name)
    {
        foreach (var property in Properties)
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) return property;
        }

        return null;
    }

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static JsonElement BuildSchema(BitAiGenerativeComponent component)
    {
        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        var required = new List<string>();

        foreach (var property in component.Properties)
        {
            properties[property.Name] = SchemaFor(property);
            if (property.IsRequired) required.Add(property.Name);
        }

        var schema = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false,
        };

        if (component.Description is not null) schema["description"] = component.Description;
        if (required.Count > 0) schema["required"] = required;

        return JsonSerializer.SerializeToElement<Dictionary<string, object?>>(schema, BitAiJsonContext.Default.DictionaryStringObject!);
    }

    /// <summary>
    /// Describes one property as JSON Schema, the way the binder reads it: nested objects with their
    /// own <c>properties</c>, collections with <c>items</c>, enumerations as their names, and dates,
    /// identifiers and addresses as formatted strings. See <see cref="BitAiGenerativeSchema"/>.
    /// </summary>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static Dictionary<string, object?> SchemaFor(BitAiGenerativeProperty property) =>
        BitAiGenerativeSchema.Describe(property.Type, property.Description);
}
