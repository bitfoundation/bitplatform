using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>Reads a component's own fields (state) excluding parameters, injected services and framework plumbing, and its parameters.</summary>
internal static class BevToolsComponentStateReader
{
    private static readonly ConcurrentDictionary<Type, (FieldInfo Field, string Name, MemberInfo Member)[]> Cache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ParameterCache = new();

    /// <summary>
    /// Each field's value with the member whose attributes decide whether it is sensitive: the property for an
    /// auto-property's backing field, which is where <c>[BevToolsSensitive]</c> is written (the compiler does not copy
    /// it to the field), unless the field itself carries it (<c>[field: BevToolsSensitive]</c>).
    /// </summary>
    public static IEnumerable<(string Name, object? Value, MemberInfo Member)> ReadFields(object component)
    {
        foreach (var (field, name, member) in Fields(component.GetType()))
        {
            object? value;
            try
            {
                value = field.GetValue(component);
            }
            catch (Exception ex)
            {
                value = "«threw " + ex.GetType().Name + "»";
            }

            yield return (name, value, member);
        }
    }

    public static IEnumerable<(string Name, object? Value, PropertyInfo Property)> ReadParameters(object component)
    {
        foreach (var property in Parameters(component.GetType()))
        {
            object? value;
            try
            {
                value = property.GetValue(component);
            }
            catch (Exception ex)
            {
                value = "«threw " + (ex is TargetInvocationException { InnerException: { } inner } ? inner : ex).GetType().Name + "»";
            }

            yield return (property.Name, value, property);
        }
    }

    /// <summary>
    /// <c>[Parameter]</c>, <c>[CascadingParameter]</c> and <c>[SupplyParameterFrom*]</c> properties, including
    /// non-public ones declared on base classes (the framework assigns those too). Sorted by name so snapshots diff
    /// deterministically.
    /// </summary>
    public static PropertyInfo[] Parameters(Type type) => ParameterCache.GetOrAdd(type, static t =>
    {
        var result = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        for (var current = t; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0 || result.ContainsKey(property.Name)
                    || Attribute.IsDefined(property, typeof(BevToolsIgnoreAttribute), true))
                {
                    continue;
                }

                if (property.IsDefined(typeof(ParameterAttribute), true) || property.IsDefined(typeof(CascadingParameterAttribute), true)
                    || property.IsDefined(typeof(SupplyParameterFromQueryAttribute), true) || property.IsDefined(typeof(SupplyParameterFromFormAttribute), true))
                {
                    result[property.Name] = property;
                }
            }
        }

        return result.Values.OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
    });

    public static (FieldInfo Field, string Name, MemberInfo Member)[] Fields(Type type) => Cache.GetOrAdd(type, static t =>
    {
        var result = new List<(FieldInfo, string, MemberInfo)>();
        var excludedProperties = new HashSet<string>(StringComparer.Ordinal);
        for (var current = t; current is not null && !IsFrameworkType(current); current = current.BaseType)
        {
            foreach (var property in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.IsDefined(typeof(ParameterAttribute), true) || property.IsDefined(typeof(CascadingParameterAttribute), true)
                    || property.IsDefined(typeof(InjectAttribute), true) || Attribute.IsDefined(property, typeof(BevToolsIgnoreAttribute), true)
                    || property.IsDefined(typeof(SupplyParameterFromQueryAttribute), true) || property.IsDefined(typeof(SupplyParameterFromFormAttribute), true))
                {
                    excludedProperties.Add(property.Name);
                }
            }
        }

        for (var current = t; current is not null && !IsFrameworkType(current); current = current.BaseType)
        {
            foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.IsDefined(typeof(BevToolsIgnoreAttribute), true) || field.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) && !field.Name.EndsWith("k__BackingField", StringComparison.Ordinal))
                {
                    continue;
                }

                var name = field.Name;
                MemberInfo member = field;
                if (name.StartsWith('<') && name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                {
                    name = name[1..name.IndexOf('>')];
                    if (excludedProperties.Contains(name))
                    {
                        continue;
                    }

                    if (!field.IsDefined(typeof(BevToolsSensitiveAttribute), true)
                        && current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } property)
                    {
                        member = property;
                    }
                }

                if (typeof(Delegate).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(EventCallback) && name.Contains("__", StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add((field, name, member));
            }
        }

        return result.ToArray();
    });

    private static bool IsFrameworkType(Type type) =>
        type == typeof(object) || type == typeof(ComponentBase) || type == typeof(LayoutComponentBase) || type == typeof(OwningComponentBase)
        || (type.Namespace?.StartsWith("Microsoft.AspNetCore.Components", StringComparison.Ordinal) ?? false);
}
