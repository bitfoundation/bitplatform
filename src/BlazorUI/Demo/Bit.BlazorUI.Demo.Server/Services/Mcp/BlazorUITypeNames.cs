using System.Reflection;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// Writes a reflected type the way it is written in Razor: <c>string?</c> rather than
/// <c>System.String</c>, <c>EventCallback&lt;MouseEventArgs&gt;</c> rather than a mangled generic
/// name. An answer is copied into a component's markup, so what it names has to be what compiles.
/// </summary>
public static class BlazorUITypeNames
{
    private static readonly Dictionary<Type, string> _aliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(object)] = "object",
        [typeof(string)] = "string",
        [typeof(void)] = "void"
    };

    /// <summary>
    /// The constraint clauses of a generic type, as C# writes them - <c>where TItem : class,
    /// new()</c> - or null when it has none.
    /// <para>
    /// A constraint is the difference between markup that compiles and markup that does not, and it
    /// is not visible anywhere else in an answer: nothing about a <c>TItem</c> in a parameter table
    /// says the type argument has to be a reference type with a parameterless constructor.
    /// </para>
    /// </summary>
    public static string? ConstraintsOf(Type type)
    {
        if (type.IsGenericTypeDefinition is false) return null;

        var clauses = type.GetGenericArguments().Select(Clause).Where(c => c is not null).ToArray();

        return clauses.Length == 0 ? null : string.Join(" ", clauses);
    }

    private static string? Clause(Type argument)
    {
        var attributes = argument.GenericParameterAttributes;
        var constraints = new List<string>();

        if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) constraints.Add("class");
        if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) constraints.Add("struct");

        constraints.AddRange(argument.GetGenericParameterConstraints()
            .Where(c => c != typeof(ValueType))
            .Select(Of));

        // Last, as C# requires, and only where struct has not already implied it.
        if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) &&
            attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) is false) constraints.Add("new()");

        return constraints.Count == 0 ? null : $"where {argument.Name} : {string.Join(", ", constraints)}";
    }

    public static string Of(Type type) => Of(type, null);

    /// <summary>
    /// The type of a method's parameter or of its return value, with the <c>?</c> its nullable
    /// annotations put on a reference type. A signature is read for which arguments may be null, and
    /// <c>string name = null</c> reads as a contradiction where <c>string? name = null</c> is what the
    /// method declares.
    /// <para>
    /// The <c>?</c> of an argument is whether a caller may pass null, which is the write state rather
    /// than the read state: the two part under <c>[AllowNull] string name</c>, which takes the null its
    /// type does not show, and <c>[DisallowNull] string? name</c>, which turns one down. What comes back
    /// - the return value, an <c>out</c> argument - is read, so it is the read state.
    /// </para>
    /// <para>
    /// <paramref name="context"/> is one context reused across the parameters of a whole listing: it
    /// caches what it has read off each member, and one per parameter re-reads them every time.
    /// </para>
    /// </summary>
    public static string Of(ParameterInfo parameter, NullabilityInfoContext? context = null)
    {
        NullabilityInfo? nullability;

        try
        {
            nullability = (context ?? new NullabilityInfoContext()).Create(parameter);
        }
        catch (Exception)
        {
            // A parameter whose annotations cannot be read is still named, just without them.
            nullability = null;
        }

        var state = parameter.Position >= 0 && parameter.IsOut is false ? nullability?.WriteState : nullability?.ReadState;

        return Of(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType, nullability, state);
    }

    private static string Of(Type type, NullabilityInfo? nullability) => Of(type, nullability, nullability?.ReadState);

    private static string Of(Type type, NullabilityInfo? nullability, NullabilityState? state)
    {
        var nullable = Nullable.GetUnderlyingType(type);

        if (nullable is not null) return $"{Of(nullable)}?";

        var annotated = type.IsValueType is false && state is NullabilityState.Nullable ? "?" : null;

        if (_aliases.TryGetValue(type, out var alias)) return $"{alias}{annotated}";

        if (type.IsArray) return $"{Of(type.GetElementType()!, nullability?.ElementType)}[]{annotated}";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            var arguments = type.GetGenericArguments();
            var annotations = nullability?.GenericTypeArguments.Length == arguments.Length ? nullability.GenericTypeArguments : null;
            var names = arguments.Select((argument, i) => Of(argument, annotations?[i]));

            // A tuple is written as a tuple: `(double, double, double)` is what a caller declares
            // the variable as, and `ValueTuple<double, double, double>` is the name of the type
            // behind it rather than anything anyone writes.
            return name == "ValueTuple"
                ? $"({string.Join(", ", names)})"
                : $"{name}<{string.Join(", ", names)}>{annotated}";
        }

        return $"{type.Name}{annotated}";
    }
}
