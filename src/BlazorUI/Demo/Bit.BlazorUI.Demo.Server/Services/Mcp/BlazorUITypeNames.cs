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

    public static string Of(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);

        if (nullable is not null) return $"{Of(nullable)}?";

        if (_aliases.TryGetValue(type, out var alias)) return alias;

        if (type.IsArray) return $"{Of(type.GetElementType()!)}[]";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];

            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Of))}>";
        }

        return type.Name;
    }
}
