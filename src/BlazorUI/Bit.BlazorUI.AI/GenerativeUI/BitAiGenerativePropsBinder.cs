using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Bit.BlazorUI;

/// <summary>
/// The outcome of binding model-produced JSON onto a registered component.
/// </summary>
/// <param name="Parameters">The parameters to pass to the component.</param>
/// <param name="Errors">
/// Properties that could not be bound, with the reason, in English - the text of <see cref="Failures"/>, for a log.
/// Non-fatal: the component still renders with whatever did bind, because a partially usable card beats an error
/// message.
/// </param>
public sealed record BitAiGenerativeBindResult(
    IReadOnlyDictionary<string, object?> Parameters,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Supplied property names the component does not declare as bindable, and which were therefore
    /// ignored.
    /// </summary>
    /// <remarks>
    /// Not errors: models routinely add a property nobody asked for, and the card is no less complete
    /// for leaving it out. Listed for diagnostics, such as noticing that a model guesses property
    /// names because the schema it was given is out of date.
    /// </remarks>
    public IReadOnlyList<string> IgnoredProperties { get; init; } = [];

    /// <summary>
    /// The properties that could not be bound, each with why, in the order <see cref="Errors"/> lists them.
    /// </summary>
    /// <remarks>
    /// What a page shows: a failure is described rather than worded, so it can be worded in the language of the page
    /// (<see cref="BitAiStrings.UnreadableProperty"/>, <see cref="BitAiStrings.MissingProperty"/>), and what the
    /// serializer said about it (<see cref="BitAiGenerativeBindError.Detail"/>) stays apart from that text.
    /// </remarks>
    public IReadOnlyList<BitAiGenerativeBindError> Failures { get; init; } = [];

    /// <summary>
    /// <see langword="true"/> when every declared property supplied bound cleanly and every required
    /// one was present. Ignored properties do not count against it.
    /// </summary>
    public bool IsComplete => Errors.Count == 0;
}

/// <summary>
/// Converts model-produced JSON into Blazor component parameters.
/// </summary>
/// <remarks>
/// <para>
/// The binder is intentionally strict about what it accepts and forgiving about what it does with
/// the rest. Unknown property names are ignored rather than passed through, so a hallucinated
/// property cannot reach the component, and are listed in
/// <see cref="BitAiGenerativeBindResult.IgnoredProperties"/> rather than reported as errors; a value of
/// the wrong shape is reported and skipped rather than thrown, so one bad field does not lose the
/// whole card.
/// </para>
/// <para>
/// It tolerates partial input by design, since props stream in and a component is rendered several
/// times before the payload is finished.
/// </para>
/// <para>
/// The JSON settings a value is read with belong to the registration (<see cref="BitAiGenerativeComponent"/>), which
/// is where an app that trims is told the property types are read by reflection; binding adds nothing to that.
/// </para>
/// </remarks>
public static class BitAiGenerativePropsBinder
{
    private static JsonSerializerOptions? _reflectionOptions;

    /// <summary>
    /// The settings every registration binds with: the property types read by reflection.
    /// </summary>
    /// <remarks>
    /// Enums are read from their names because that is what the component schema tells the model to
    /// send; integers are still accepted. Created on first use, by the registration that needs them.
    /// </remarks>
    internal static JsonSerializerOptions ReflectionOptions
    {
        [RequiresUnreferencedCode(BitAiTrimming.GenerativeUnreferencedCode)]
        [RequiresDynamicCode(BitAiTrimming.GenerativeDynamicCode)]
        get => _reflectionOptions ??= new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
    }

    /// <summary>
    /// Binds a JSON object onto the parameters of a registered component.
    /// </summary>
    /// <param name="component">The registration, which defines what may be set.</param>
    /// <param name="props">
    /// The model-produced properties. Anything but an object binds nothing, and leaves every
    /// required property reported as missing.
    /// </param>
    /// <returns>The parameters to render with, and any problems found.</returns>
    public static BitAiGenerativeBindResult Bind(BitAiGenerativeComponent component, JsonElement? props)
    {
        ArgumentNullException.ThrowIfNull(component);

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var failures = new List<BitAiGenerativeBindError>();
        var ignored = new List<string>();

        // A payload that is not an object binds nothing, but the required check still runs: a
        // component whose props never arrived is as incomplete as one whose props left a field out.
        foreach (var property in props is { ValueKind: JsonValueKind.Object } payload ? payload.EnumerateObject() : [])
        {
            var declared = component.FindProperty(property.Name);
            if (declared is null)
            {
                ignored.Add(property.Name);
                continue;
            }

            if (property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (IsNullable(declared.Type)) parameters[declared.Name] = null;
                continue;
            }

            try
            {
                parameters[declared.Name] = property.Value.Deserialize(component.BindingOptions.GetTypeInfo(declared.Type));
            }
            catch (JsonException exception)
            {
                failures.Add(Unreadable(component, property.Name, declared.Type, exception));
            }
            catch (NotSupportedException exception)
            {
                failures.Add(Unreadable(component, property.Name, declared.Type, exception));
            }
            catch (InvalidOperationException exception)
            {
                // Raised for parameter types the serializer cannot construct, such as a record whose
                // constructor parameters do not match its properties.
                failures.Add(Unreadable(component, property.Name, declared.Type, exception));
            }
        }

        foreach (var required in component.Properties)
        {
            if (required.IsRequired && !parameters.ContainsKey(required.Name))
            {
                failures.Add(new(BitAiGenerativeBindErrorKind.Missing, required.Name) { ComponentName = component.Name });
            }
        }

        return new BitAiGenerativeBindResult(parameters, failures.ConvertAll(f => f.ToString()))
        {
            IgnoredProperties = ignored,
            Failures = failures,
        };
    }

    private static BitAiGenerativeBindError Unreadable(BitAiGenerativeComponent component, string name, Type type, Exception exception) =>
        new(BitAiGenerativeBindErrorKind.Unreadable, name)
        {
            ComponentName = component.Name,
            ExpectedType = type,
            Detail = exception.Message,
        };

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
}
