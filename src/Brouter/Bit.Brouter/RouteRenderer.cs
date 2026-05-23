using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

internal class RouteRenderer
{
    private readonly Route _route;

    public RouteRenderer(Route route)
    {
        _route = route;
    }

    public void BuildRenderTree(RenderTreeBuilder builder, bool matched)
    {
        builder.OpenComponent<CascadingValue<Route>>(0);
        builder.AddAttribute(1, "Name", "ParentRoute");
        builder.AddAttribute(2, "Value", _route);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(0, _route.ChildContent);
            if (matched)
            {
                // RenderRoute restarts its own sequence numbers from 0; wrap it in a region
                // so its frames live in an independent sequence-number space and don't collide
                // with the AddContent above.
                b.OpenRegion(1);
                RenderRoute(b);
                b.CloseRegion();
            }
        }));
        builder.CloseComponent();
    }

    private void RenderRoute(RenderTreeBuilder builder)
    {
        var merged = MergeParameters(_route.InheritedParameters, _route.Parameters);
        var routeParams = new RouteParameters(merged);

        builder.OpenComponent<CascadingValue<RouteParameters>>(0);
        builder.AddAttribute(1, "Name", "RouteParameters");
        builder.AddAttribute(2, "Value", routeParams);
        builder.AddAttribute(3, "IsFixed", false);
        builder.AddAttribute(4, "ChildContent", (RenderFragment)(b1 =>
        {
            b1.OpenComponent<CascadingValue<object?>>(0);
            b1.AddAttribute(1, "Name", "RouteData");
            b1.AddAttribute(2, "Value", _route.LoadedData);
            b1.AddAttribute(3, "ChildContent", (RenderFragment)(b2 =>
            {
                b2.OpenComponent<CascadingValue<object?>>(0);
                b2.AddAttribute(1, "Name", "RouteMeta");
                b2.AddAttribute(2, "Value", _route.Meta);
                b2.AddAttribute(3, "ChildContent", (RenderFragment)(b3 =>
                {
                    if (_route.Parent?.Outlet is null)
                    {
                        if (_route.Content is not null)
                        {
                            b3.AddContent(0, _route.Content(routeParams));
                        }
                        else if (_route.Component is not null)
                        {
                            b3.OpenComponent(0, _route.Component);
                            ApplyTypedParameters(b3, _route.Component, routeParams, _route.Brouter?.CurrentLocation);
                            b3.CloseComponent();
                        }
                    }
                    else
                    {
                        _route.Parent.Outlet.Render(_route, routeParams);
                    }
                }));
                b2.CloseComponent();
            }));
            b1.CloseComponent();
        }));
        builder.CloseComponent();
    }

    internal static void ApplyTypedParameters(RenderTreeBuilder builder, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, RouteParameters parameters, BrouterLocation? location)
    {
        // Reflect once per type. Simple, correct, allocates only on first hit per type.
        // Trimming: Component is annotated DynamicallyAccessedMemberTypes.All so its members are preserved.
        var bindings = TypedParameterCache.GetBindings(componentType);
        // Sequence numbers for dynamic parameter attributes start after the OpenComponent (0).
        // These are stable per render because the same bindings are iterated in the same order.
        var seq = 1;
        foreach (var b in bindings)
        {
            if (b.IsQuery)
            {
                if (location is null) continue;
                if (TryBindQuery(b, location, out var queryValue))
                {
                    builder.AddAttribute(seq++, b.PropertyName, queryValue);
                }
                continue;
            }

            if (parameters.Values.TryGetValue(b.ParameterName, out var raw) is false || raw is null) continue;

            object? value = raw;
            if (b.PropertyType.IsAssignableFrom(raw.GetType()) is false)
            {
                if (parameters.TryGetWeak(b.ParameterName, b.PropertyType, out var converted))
                {
                    value = converted;
                }
                else
                {
                    continue;
                }
            }

            builder.AddAttribute(seq++, b.PropertyName, value);
        }
    }

    private static bool TryBindQuery(ParameterBinding binding, BrouterLocation location, out object? value)
    {
        value = null;
        if (location.QueryParams.TryGetValue(binding.ParameterName, out var values) is false || values.Count == 0)
            return false;

        var propType = binding.PropertyType;

        // Multi-value support: per BrouterQueryAttribute docs, string[]-typed properties receive every value.
        if (propType == typeof(string[]))
        {
            var arr = new string[values.Count];
            for (int i = 0; i < values.Count; i++) arr[i] = values[i];
            value = arr;
            return true;
        }

        // Scalar properties bind to the first value, converted to the property's type.
        return TryConvert(values[0], propType, out value);
    }

    private static bool TryConvert(string raw, Type targetType, out object? value)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying == typeof(string))
        {
            value = raw;
            return true;
        }

        // Convert.ChangeType doesn't support string -> Guid or string -> Enum, so handle them
        // explicitly before falling back. Mirrors RouteParameters.TryGetWeak so [BrouterQuery]
        // bindings accept the same scalar types as [BrouterParameter]. Nullable<T> is honored
        // because we resolved the underlying type above.
        if (underlying == typeof(Guid))
        {
            if (Guid.TryParse(raw, out var guidVal))
            {
                value = guidVal;
                return true;
            }
            value = null;
            return false;
        }

        if (underlying.IsEnum)
        {
            if (Enum.TryParse(underlying, raw, ignoreCase: true, out var enumVal))
            {
                value = enumVal;
                return true;
            }
            value = null;
            return false;
        }

        try
        {
            value = System.Convert.ChangeType(raw, underlying, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    private static IDictionary<string, object?> MergeParameters(RouteParameters? inherited, IDictionary<string, object?>? local)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (inherited is not null)
        {
            foreach (var kv in inherited.Values) result[kv.Key] = kv.Value;
        }
        if (local is not null)
        {
            foreach (var kv in local) result[kv.Key] = kv.Value; // local wins
        }
        return result;
    }
}

internal static class TypedParameterCache
{
    private static readonly Dictionary<Type, ParameterBinding[]> _cache = new();
    private static readonly object _lock = new();

    public static ParameterBinding[] GetBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(type, out var cached)) return cached;

            var bindings = new List<ParameterBinding>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var paramAttr = prop.GetCustomAttribute<BrouterParameterAttribute>();
                if (paramAttr is not null)
                {
                    bindings.Add(new ParameterBinding(prop.Name, paramAttr.Name ?? prop.Name, prop.PropertyType, IsQuery: false));
                    continue;
                }
                var queryAttr = prop.GetCustomAttribute<BrouterQueryAttribute>();
                if (queryAttr is not null)
                {
                    bindings.Add(new ParameterBinding(prop.Name, queryAttr.Name ?? prop.Name, prop.PropertyType, IsQuery: true));
                }
            }

            var arr = bindings.ToArray();
            _cache[type] = arr;
            return arr;
        }
    }
}

internal readonly record struct ParameterBinding(string PropertyName, string ParameterName, Type PropertyType, bool IsQuery);
