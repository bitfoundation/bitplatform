using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>
/// The members of a component type the inspector shows: parameters, cascading values (with the
/// <c>[SupplyParameterFrom*]</c> ones) and injected services. One lookup for the in-app inspector and the protocol, so
/// both show the same members in the same order.
/// </summary>
internal static class BevComponentMembers
{
    private static readonly ConcurrentDictionary<Type, (PropertyInfo[] Parameters, PropertyInfo[] Cascading, PropertyInfo[] Injected)> Cache = new();

    public static (PropertyInfo[] Parameters, PropertyInfo[] Cascading, PropertyInfo[] Injected) Of(Type type) => Cache.GetOrAdd(type, static t =>
    {
        var all = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
        return (
            all.Where(p => p.IsDefined(typeof(ParameterAttribute), true)).OrderBy(p => p.Name).ToArray(),
            all.Where(p => p.IsDefined(typeof(CascadingParameterAttribute), true) || p.IsDefined(typeof(SupplyParameterFromQueryAttribute), true) || p.IsDefined(typeof(SupplyParameterFromFormAttribute), true)).OrderBy(p => p.Name).ToArray(),
            all.Where(p => p.IsDefined(typeof(InjectAttribute), true)).OrderBy(p => p.Name).ToArray());
    });

    public static object? SafeGet(PropertyInfo property, object? target)
    {
        try
        {
            return property.GetValue(target);
        }
        catch (Exception ex)
        {
            return "«threw " + (ex is TargetInvocationException { InnerException: { } i } ? i.GetType().Name : ex.GetType().Name) + "»";
        }
    }

    public static object? SafeGet(FieldInfo field, object? target)
    {
        try
        {
            return field.GetValue(target);
        }
        catch (Exception ex)
        {
            return "«threw " + ex.GetType().Name + "»";
        }
    }

    /// <summary>The value of one inspectable member by section ("parameters", "cascading", "state") and name, for expanding it.</summary>
    public static (object? Value, MemberInfo? Member, bool Found) Read(BevComponentRecord record, string section, string name)
    {
        var component = record.Component;
        if (component is null)
        {
            return (null, null, false);
        }

        var (parameters, cascading, _) = Of(record.Type);
        switch (section)
        {
            case "parameters" when parameters.FirstOrDefault(p => p.Name == name) is { } parameter:
                return (SafeGet(parameter, component), parameter, true);
            case "cascading" when cascading.FirstOrDefault(p => p.Name == name) is { } value:
                return (SafeGet(value, component), value, true);
            case "state" when ComponentStateReader.Fields(record.Type).FirstOrDefault(f => f.Name == name) is { Field: not null } field:
                return (SafeGet(field.Field, component), field.Member, true);
            default:
                return (null, null, false);
        }
    }
}
