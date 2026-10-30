using System.Reflection;
using System.Collections;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Bit.BlazorHeadlessUI.Demo.Server.Services;

/// <summary>
/// Types and values the way a Razor author writes them: <c>bool</c> rather than <c>Boolean</c>,
/// <c>RenderFragment&lt;BitBhMenuItemRenderContext&gt;?</c> rather than <c>RenderFragment`1</c>, and a
/// default value as the literal that would produce it.
/// </summary>
public static class BlazorHeadlessUITypeNames
{
    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(char)] = "char",
        [typeof(decimal)] = "decimal",
        [typeof(double)] = "double",
        [typeof(float)] = "float",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(object)] = "object",
        [typeof(short)] = "short",
        [typeof(string)] = "string",
        [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong",
        [typeof(void)] = "void",
    };

    /// <summary>A type's name without its namespace and with its generic arguments spelled out.</summary>
    public static string Of(Type type, NullabilityInfo? nullability = null)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlying) return $"{Of(underlying)}?";

        var suffix = nullability is { ReadState: NullabilityState.Nullable } && type.IsValueType is false ? "?" : "";

        if (type.IsArray)
        {
            return $"{Of(type.GetElementType()!, nullability?.ElementType)}[]{suffix}";
        }

        if (Keywords.TryGetValue(type, out var keyword)) return keyword + suffix;

        if (type.IsGenericParameter) return type.Name + suffix;

        var name = type.Name;
        var arity = name.IndexOf('`', StringComparison.Ordinal);
        if (arity >= 0) name = name[..arity];

        if (type.IsGenericType is false) return name + suffix;

        var arguments = type.GetGenericArguments();
        var argumentNullability = nullability?.GenericTypeArguments;

        var names = arguments.Select((argument, index) =>
            Of(argument, argumentNullability is { Length: > 0 } && index < argumentNullability.Length ? argumentNullability[index] : null));

        return $"{name}<{string.Join(", ", names)}>{suffix}";
    }

    /// <summary>A property's type, nullable annotations included.</summary>
    public static string Of(PropertyInfo property)
    {
        NullabilityInfo? nullability = null;

        try
        {
            nullability = new NullabilityInfoContext().Create(property);
        }
        catch (Exception)
        {
            // Nullability metadata is a refinement; the bare type name is still right.
        }

        return Of(property.PropertyType, nullability);
    }

    /// <summary>
    /// The value a constructed component holds before any parameter is set, as the literal that would
    /// produce it. Delegates (callbacks, render fragments) have no meaningful default and answer
    /// null, which the table shows as a dash.
    /// </summary>
    public static string? Literal(object? value, Type declaredType)
    {
        if (IsDelegateLike(declaredType)) return null;

        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            string text => $"\"{text}\"",
            char character => $"'{character}'",
            Enum member => Enum.IsDefined(member.GetType(), member)
                ? $"{member.GetType().Name}.{member}"
                : $"({member.GetType().Name}){Convert.ToInt64(member, CultureInfo.InvariantCulture)}",
            TimeSpan span => $"TimeSpan.FromMilliseconds({span.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)})",
            IFormattable number when value.GetType().IsPrimitive || value is decimal => number.ToString(null, CultureInfo.InvariantCulture),
            ICollection { Count: 0 } => "[]",
            _ => "new()",
        };
    }

    /// <summary>Whether a parameter of this type is something you hand in to be called or rendered.</summary>
    public static bool IsDelegateLike(Type type)
    {
        var bare = Nullable.GetUnderlyingType(type) ?? type;

        return typeof(Delegate).IsAssignableFrom(bare)
            || bare == typeof(EventCallback)
            || (bare.IsGenericType && bare.GetGenericTypeDefinition() == typeof(EventCallback<>));
    }

    /// <summary>
    /// The library types a type is built from - itself, its generic arguments, its array element - so
    /// that <c>RenderFragment&lt;BitBhTabRenderContext&gt;?</c> is known to involve BitBhTabRenderContext.
    /// </summary>
    public static IEnumerable<Type> Constituents(Type type)
    {
        if (type.IsGenericParameter) yield break;

        if (Nullable.GetUnderlyingType(type) is Type underlying) type = underlying;

        if (type.IsArray)
        {
            foreach (var element in Constituents(type.GetElementType()!)) yield return element;
            yield break;
        }

        yield return type.IsGenericType ? type.GetGenericTypeDefinition() : type;

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var constituent in Constituents(argument)) yield return constituent;
            }
        }
    }
}
