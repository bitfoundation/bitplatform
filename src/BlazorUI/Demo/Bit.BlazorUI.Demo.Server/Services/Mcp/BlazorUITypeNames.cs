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

    public static string Of(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);

        if (nullable is not null) return $"{Of(nullable)}?";

        if (_aliases.TryGetValue(type, out var alias)) return alias;

        if (type.IsArray) return $"{Of(type.GetElementType()!)}[]";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];

            // A tuple is written as a tuple: `(double, double, double)` is what a caller declares
            // the variable as, and `ValueTuple<double, double, double>` is the name of the type
            // behind it rather than anything anyone writes.
            return name == "ValueTuple"
                ? $"({string.Join(", ", type.GetGenericArguments().Select(Of))})"
                : $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Of))}>";
        }

        return type.Name;
    }
}
