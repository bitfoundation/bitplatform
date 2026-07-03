using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

internal class BrouterRouteRenderer
{
    private readonly BrouterRoute _route;

    // Cache the last merged BrouterRouteParameters and the (inherited, local) reference pair
    // it was derived from. RenderRoute runs on every render of every route in the matched
    // chain; each one used to allocate a fresh dictionary + BrouterRouteParameters even when
    // nothing changed. Cache hit -> zero allocations on the hot render path. Cache miss
    // (parameters changed because a new match committed fresh dictionaries onto the route
    // and/or the cascading inherited value got a new instance) -> rebuild and store.
    private BrouterRouteParameters? _cachedRouteParams;
    private BrouterRouteParameters? _cachedInheritedRef;
    private IReadOnlyDictionary<string, object?>? _cachedLocalRef;

    public BrouterRouteRenderer(BrouterRoute route)
    {
        _route = route;
    }

    public void BuildRenderTree(RenderTreeBuilder builder, bool matched)
    {
        builder.OpenComponent<CascadingValue<BrouterRoute>>(0);
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
        var inherited = _route.InheritedParameters;
        var local = _route.Parameters;

        // Reuse the previously-built BrouterRouteParameters when neither the inherited
        // cascading instance nor the local match dictionary has been replaced. Both refs
        // get a fresh instance whenever a navigation actually changes the routing state
        // (Brouter.ProcessNavigationAsync commits fresh dictionaries on a match commit, and
        // the cascading inherited reference is reissued by the parent renderer), so this
        // is conservative: equal refs mean nothing user-visible has changed.
        BrouterRouteParameters routeParams;
        if (_cachedRouteParams is not null
            && ReferenceEquals(_cachedInheritedRef, inherited)
            && ReferenceEquals(_cachedLocalRef, local))
        {
            routeParams = _cachedRouteParams;
        }
        else
        {
            var merged = MergeParameters(inherited, local);
            routeParams = new BrouterRouteParameters(merged);

            _cachedRouteParams = routeParams;
            _cachedInheritedRef = inherited;
            _cachedLocalRef = local;
        }

        builder.OpenComponent<CascadingValue<BrouterRouteParameters>>(0);
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

    internal static void ApplyTypedParameters(RenderTreeBuilder builder, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type componentType, BrouterRouteParameters parameters, BrouterLocation? location)
    {
        // Reflect once per type. Simple, correct, allocates only on first hit per type.
        // Trimming: Component is annotated DynamicallyAccessedMemberTypes.All so its members are preserved.
        var bindings = BrouterTypedParameterCache.GetBindings(componentType);
        // Sequence numbers for dynamic parameter attributes start after the OpenComponent (0).
        // These are stable per render because the same bindings are iterated in the same order.
        var seq = 1;
        foreach (var b in bindings)
        {
            // Always emit an attribute frame per binding, even when the binding is missing or
            // unconvertible. Component instances are reused across navigations that match the
            // same Component (e.g. /profile/saleh -> /profile), so silently skipping a frame
            // would leave the previous value on the property instead of clearing it back to
            // its default. Stable per-binding sequence numbers also keep Blazor's diff happy.
            object? value;

            if (b.IsQuery)
            {
                if (location is null || TryBindQuery(b, location, out value) is false)
                {
                    value = DefaultValueFor(b.PropertyType);
                }
            }
            else if (parameters.Values.TryGetValue(b.ParameterName, out var raw) is false || raw is null)
            {
                value = DefaultValueFor(b.PropertyType);
            }
            else if (b.PropertyType.IsAssignableFrom(raw.GetType()))
            {
                value = raw;
            }
            else if (parameters.TryGetWeak(b.ParameterName, b.PropertyType, out var converted))
            {
                value = converted;
            }
            else
            {
                value = DefaultValueFor(b.PropertyType);
            }

            builder.AddAttribute(seq++, b.PropertyName, value);
        }
    }

    // Cache boxed default(T) per value type so we don't allocate a fresh single-element array
    // on every render. ApplyTypedParameters runs frequently and iterates every binding, so the
    // previous per-call Array.CreateInstance produced avoidable GC pressure. Boxed value-type
    // defaults are effectively immutable for our purposes (Blazor unboxes when assigning to the
    // property setter), so a shared instance per Type is safe to hand out repeatedly.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object?> _boxedDefaultCache = new();

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "t comes from PropertyType of a [Parameter] on a component reached via Route.Component, " +
                        "which is annotated DynamicallyAccessedMemberTypes.All so its parameter property types are preserved.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Same as above. Array.CreateInstance for a single element is safe for the closed set of " +
                        "value types reachable from preserved component parameter properties.")]
    private static object? DefaultValueFor(Type t)
    {
        // For nullable / reference types, default is null. For non-nullable value types,
        // create a single-element array of that type and read element 0; this returns the
        // boxed default(T) without requiring constructor annotations on the Type. The result
        // is cached per Type so subsequent renders reuse the same boxed instance.
        if (t.IsValueType is false || Nullable.GetUnderlyingType(t) is not null)
            return null;

        if (_boxedDefaultCache.TryGetValue(t, out var cached))
            return cached;

        var arr = Array.CreateInstance(t, 1);
        var value = arr.GetValue(0);
        _boxedDefaultCache[t] = value;
        return value;
    }

    private static bool TryBindQuery(BrouterParameterBinding binding, BrouterLocation location, out object? value)
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
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            // See BrouterRouteParameters.TryGetWeak for the rationale: these are the documented
            // Convert.ChangeType failure modes. Narrower catch keeps genuine programming bugs
            // (NREs, OOM) visible.
            value = null;
            return false;
        }
    }

    private static IReadOnlyDictionary<string, object?> MergeParameters(BrouterRouteParameters? inherited, IReadOnlyDictionary<string, object?>? local)
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

internal static class BrouterTypedParameterCache
{
    // ConcurrentDictionary lets cold-start renders run reflection in parallel instead of
    // serializing on a single lock. The cache is read every render of every component that
    // uses [BrouterParameter] / [BrouterQuery], so contention on a coarse lock matters when
    // many such components are mounted at once (e.g. a list page with many cards).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, BrouterParameterBinding[]> _cache = new();

    public static BrouterParameterBinding[] GetBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        // Fast path: hit the cached value without going through the factory delegate.
        if (_cache.TryGetValue(type, out var cached)) return cached;

        // Compute outside GetOrAdd so the trimmer can see the [DynamicallyAccessedMembers]
        // requirement is satisfied at the call site (passing BuildBindings as a delegate would
        // trip IL2111 because the trimmer can't follow annotation through delegate invocation).
        // Under contention multiple threads may race here and produce equivalent arrays; only
        // one wins via TryAdd, the rest are GC'd. BuildBindings is pure for a given Type so
        // either result is correct.
        var bindings = BuildBindings(type);
        _cache.TryAdd(type, bindings);
        return _cache.TryGetValue(type, out var stored) ? stored : bindings;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "type flows from GetBindings whose parameter is annotated with " +
                        "DynamicallyAccessedMemberTypes.PublicProperties; the factory only reads public properties.")]
    private static BrouterParameterBinding[] BuildBindings([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var bindings = new List<BrouterParameterBinding>();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var paramAttr = prop.GetCustomAttribute<BrouterParameterAttribute>();
            var queryAttr = prop.GetCustomAttribute<BrouterQueryAttribute>();
            if (paramAttr is null && queryAttr is null) continue;

            // Reject ambiguous annotations up front: a property carrying both attributes
            // would silently bind as one or the other, leaving the developer unaware that
            // half of their intent was dropped. Fail fast with a clear message that names
            // the offending property and both attribute names.
            if (paramAttr is not null && queryAttr is not null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with both " +
                    $"[{nameof(BrouterParameterAttribute)}] and [{nameof(BrouterQueryAttribute)}]. " +
                    "Pick exactly one: a property can bind to either a route parameter or a query string value, not both.");

            // [BrouterParameter] / [BrouterQuery] only have an effect when Blazor recognises
            // the property as a component parameter, i.e. it's annotated with [Parameter]
            // (or [CascadingParameter], which Brouter doesn't drive) and has a public setter.
            // Without that, AddAttribute below would feed an unknown attribute into the
            // component and Blazor would throw a generic exception the moment the route
            // matches. Failing here gives the developer a clear, actionable message.
            var attrName = paramAttr is not null ? nameof(BrouterParameterAttribute) : nameof(BrouterQueryAttribute);
            if (prop.GetCustomAttribute<ParameterAttribute>() is null)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with [{attrName}] but is missing [Parameter]. " +
                    "Add [Parameter] (or remove the Brouter binding attribute).");
            if (prop.SetMethod is null || prop.SetMethod.IsPublic is false)
                throw new InvalidOperationException(
                    $"Property '{type.FullName}.{prop.Name}' is annotated with [{attrName}] but has no public setter. " +
                    "Add a public setter so the router can assign the bound value.");

            if (paramAttr is not null)
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, paramAttr.Name ?? prop.Name, prop.PropertyType, IsQuery: false));
            }
            else
            {
                bindings.Add(new BrouterParameterBinding(prop.Name, queryAttr!.Name ?? prop.Name, prop.PropertyType, IsQuery: true));
            }
        }

        return bindings.ToArray();
    }
}

internal readonly record struct BrouterParameterBinding(string PropertyName, string ParameterName, Type PropertyType, bool IsQuery);
