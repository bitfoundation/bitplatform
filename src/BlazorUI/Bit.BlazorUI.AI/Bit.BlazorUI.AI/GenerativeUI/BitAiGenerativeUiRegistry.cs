using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using IComponent = Microsoft.AspNetCore.Components.IComponent;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// The components model output is allowed to render.
/// </summary>
public interface IBitAiGenerativeUiRegistry
{
    /// <summary>Every registered component.</summary>
    IReadOnlyCollection<BitAiGenerativeComponent> Components { get; }

    /// <summary>
    /// Finds a component by the name the model used.
    /// </summary>
    /// <param name="name">The component name.</param>
    /// <returns>
    /// The registration, or <see langword="null"/> when nothing is registered under that name. A
    /// null result must render a fallback: it is the case where a model named something it should
    /// not have.
    /// </returns>
    BitAiGenerativeComponent? Find(string name);
}

/// <summary>
/// The default registry, populated at startup and safe to read from any thread.
/// </summary>
/// <remarks>
/// <para>
/// Registration derives the bindable property list from the component public
/// <see cref="ParameterAttribute"/> properties, minus the ones no model should ever set:
/// callbacks, render fragments, unmatched-attribute catch-alls, anything marked
/// <see cref="BitAiGenerativeIgnoreAttribute"/>, and any type whose object graph contains a delegate or a
/// <see cref="Type"/>. That exclusion is what stops a generated payload from wiring up event
/// handlers or injecting attributes. A <c>[Required]</c> data annotation marks a parameter required
/// without naming it at registration.
/// </para>
/// <para>
/// Registering reads the parameter types by reflection, down to every type they reach, to describe them to the model
/// and to bind the model's JSON onto them later; that is why the <c>Register</c> overloads are marked as unsafe to trim.
/// The component type itself is kept by its annotation, as the renderer needs it whole.
/// </para>
/// </remarks>
public sealed class BitAiGenerativeUiRegistry : IBitAiGenerativeUiRegistry
{
    private readonly ConcurrentDictionary<string, BitAiGenerativeComponent> _components =
        new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlyCollection<BitAiGenerativeComponent> Components => _components.Values.ToArray();

    /// <inheritdoc />
    public BitAiGenerativeComponent? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _components.TryGetValue(name, out var component) ? component : null;
    }

    /// <summary>
    /// Registers a component under a name the model can use.
    /// </summary>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <param name="name">The name the model uses. Keep it descriptive: the model reads it.</param>
    /// <param name="description">What the component is for, included in the schema.</param>
    /// <param name="required">Property names the model must supply.</param>
    /// <returns>The registration.</returns>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.GenerativeDynamicCode)]
    public BitAiGenerativeComponent Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(
        string name,
        string? description = null,
        params string[] required)
        where TComponent : IComponent =>
        Register(typeof(TComponent), name, description, required);

    /// <summary>
    /// Registers a component under a name the model can use.
    /// </summary>
    /// <param name="componentType">The component type.</param>
    /// <param name="name">The name the model uses.</param>
    /// <param name="description">What the component is for.</param>
    /// <param name="required">Property names the model must supply.</param>
    /// <returns>The registration.</returns>
    /// <exception cref="ArgumentException">The type is not a Blazor component.</exception>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    [RequiresDynamicCode(BitAiTrimming.GenerativeDynamicCode)]
    public BitAiGenerativeComponent Register(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
        string name,
        string? description = null,
        params string[] required)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException(
                $"{componentType.Name} is not a Blazor component.", nameof(componentType));
        }

        var properties = DiscoverProperties(componentType, required);
        description ??= componentType.GetCustomAttribute<DescriptionAttribute>()?.Description;
        var component = new BitAiGenerativeComponent(name, componentType, properties, description);
        _components[name] = component;
        return component;
    }

    /// <summary>Removes a registration.</summary>
    /// <param name="name">The name to remove.</param>
    /// <returns><see langword="true"/> when a registration was removed.</returns>
    public bool Unregister(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _components.TryRemove(name, out _);
    }

    /// <summary>
    /// Produces a JSON Schema describing every registered component, for use in a system prompt or
    /// as the schema of a render tool.
    /// </summary>
    /// <returns>The schema.</returns>
    /// <remarks>
    /// Handing this to the model is what makes generative UI reliable. Without it the model guesses
    /// component names, and every guess renders a fallback.
    /// </remarks>
    public JsonElement DescribeAll()
    {
        var variants = _components.Values
            .DistinctBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .Select(c => new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["type"] = "object",
                ["description"] = c.Description,
                ["properties"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["component"] = new Dictionary<string, object?> { ["const"] = c.Name },
                    ["props"] = c.Schema,
                },
                ["required"] = new[] { "component", "props" },
            })
            .ToArray();

        return JsonSerializer.SerializeToElement<Dictionary<string, object?>>(new(StringComparer.Ordinal)
        {
            ["oneOf"] = variants,
        }, BitAiJsonContext.Default.DictionaryStringObject!);
    }

    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    private static IReadOnlyList<BitAiGenerativeProperty> DiscoverProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType,
        string[] required)
    {
        var results = new List<BitAiGenerativeProperty>();

        foreach (var property in componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameter = property.GetCustomAttribute<ParameterAttribute>();
            if (parameter is null || parameter.CaptureUnmatchedValues) continue;
            if (property.SetMethod is null || !property.SetMethod.IsPublic) continue;
            if (property.GetCustomAttribute<BitAiGenerativeIgnoreAttribute>() is not null) continue;
            if (!IsBindable(property.PropertyType)) continue;

            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
            var isRequired = required.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                || property.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>() is not null;
            results.Add(new BitAiGenerativeProperty(property.Name, property.PropertyType, description, isRequired));
        }

        return results;
    }

    /// <summary>
    /// Whether a parameter type is plain data a model may supply. Delegates, render fragments,
    /// event callbacks, <see cref="Type"/>, and any object graph that contains one are refused.
    /// </summary>
    /// <param name="type">The parameter type.</param>
    /// <returns><see langword="true"/> when the type may be bound.</returns>
    /// <remarks>
    /// Reads the type, and every type it reaches, by reflection: members that trimming removed are not seen.
    /// </remarks>
    [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
    public static bool IsBindable(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return BitAiGenerativeSchema.IsBindable(type);
    }
}
