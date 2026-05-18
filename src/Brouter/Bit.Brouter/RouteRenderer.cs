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
        var seq = 0;
        builder.OpenComponent<CascadingValue<Route>>(seq++);
        builder.AddAttribute(seq++, "Name", "ParentRoute");
        builder.AddAttribute(seq++, "Value", _route);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b =>
        {
            b.AddContent(seq, _route.ChildContent);
            if (matched) RenderRoute(b, seq);
        }));
        builder.CloseComponent();
    }

    private void RenderRoute(RenderTreeBuilder builder, int seq)
    {
        var merged = MergeParameters(_route.InheritedParameters, _route.Parameters);
        var routeParams = new RouteParameters(merged);

        builder.OpenComponent<CascadingValue<RouteParameters>>(seq++);
        builder.AddAttribute(seq++, "Name", "RouteParameters");
        builder.AddAttribute(seq++, "Value", routeParams);
        builder.AddAttribute(seq++, "IsFixed", false);
        builder.AddAttribute(seq++, "ChildContent", (RenderFragment)(b1 =>
        {
            b1.OpenComponent<CascadingValue<object?>>(seq++);
            b1.AddAttribute(seq++, "Name", "RouteData");
            b1.AddAttribute(seq++, "Value", _route.LoadedData);
            b1.AddAttribute(seq++, "ChildContent", (RenderFragment)(b2 =>
            {
                b2.OpenComponent<CascadingValue<object?>>(seq++);
                b2.AddAttribute(seq++, "Name", "RouteMeta");
                b2.AddAttribute(seq++, "Value", _route.Meta);
                b2.AddAttribute(seq++, "ChildContent", (RenderFragment)(b3 =>
                {
                    if (_route.Parent?.Outlet is null)
                    {
                        if (_route.Content is not null)
                        {
                            b3.AddContent(seq, _route.Content(routeParams));
                        }
                        else if (_route.Component is not null)
                        {
                            b3.OpenComponent(seq++, _route.Component);
                            ApplyTypedParameters(b3, _route.Component, routeParams, ref seq);
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

    private static void ApplyTypedParameters(RenderTreeBuilder builder, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, RouteParameters parameters, ref int seq)
    {
        // Reflect once per type. Simple, correct, allocates only on first hit per type.
        // Trimming: Component is annotated DynamicallyAccessedMemberTypes.All so its members are preserved.
        var bindings = TypedParameterCache.GetBindings(componentType);
        foreach (var b in bindings)
        {
            if (b.IsQuery)
            {
                // Wired by the consumer via [BrouterQuery] — but query state lives on the location, not the route.
                // We expose the raw values through cascading; query auto-binding is intentionally off here to keep
                // routes orthogonal to query-string state. Components can read [BrouterQuery] via cascading Location.
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

    private static IDictionary<string, object?> MergeParameters(RouteParameters? inherited, IDictionary<string, object?>? local)
    {
        var result = new Dictionary<string, object?>();
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
