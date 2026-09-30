using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bit.BlazorUI;

public abstract partial class BitComponentBase
{
    private static readonly ConcurrentDictionary<Type, CascadeMap?> _cascadeMaps = new();

    // The params object this component took its defaults from the last time, and the value each parameter it has
    // supplied since held before it first did. A value written by the params object is not one the markup passes
    // again on the next render, so Blazor never overwrites it: once the params object stops supplying it - the
    // property cleared, the object removed, an isolated BitParams put in between - it is put back from here.
    private IBitComponentParams? _cascadedParams;
    private Dictionary<string, object?>? _cascadeOriginals;



    /// <summary>
    /// Whether the markup set the named parameter on this render. Each class that tracks its own parameters adds
    /// them, so the answer covers the whole hierarchy rather than the one level a HasNotBeenSet belongs to.
    /// </summary>
    private protected virtual bool IsSetByMarkup(string name) => _assignedParameters.Contains(name);

    /// <summary>
    /// Whether the component puts back the parameters its params object stops supplying on its own, and so opts
    /// out of the shared restore.
    /// </summary>
    private protected virtual bool RestoresCascadeItself => false;



    /// <summary>
    /// Puts back every parameter the params object of this component supplied before and no longer does, and
    /// remembers the value each newly supplied one replaces. Runs once every parameter is assigned and before the
    /// params object is applied, which is when a parameter still holds the value it had before the cascade.
    /// </summary>
    private void RestoreDroppedCascadeParameters()
    {
        if (RestoresCascadeItself) return;

        var map = GetCascadeMap(GetType());

        if (map is null) return;

        var current = map.GetParams(this);

        // BitParams hands down a new object whenever what it carries changes, so the same object means nothing to do.
        if (ReferenceEquals(current, _cascadedParams)) return;

        _cascadedParams = current;

        var isChanged = false;

        if (_cascadeOriginals is { Count: > 0 })
        {
            foreach (var (name, original) in _cascadeOriginals.ToArray())
            {
                var parameter = map.Parameters[name];

                if (current is not null && parameter.GetFromParams(current) is not null) continue;

                _cascadeOriginals.Remove(name);

                if (IsSetByMarkup(name)) continue;

                parameter.SetOnComponent(this, original);

                isChanged = true;
            }
        }

        if (current is not null)
        {
            foreach (var (name, parameter) in map.Parameters)
            {
                if (parameter.GetFromParams(current) is null) continue;
                if (_cascadeOriginals?.ContainsKey(name) is true) continue;
                if (IsSetByMarkup(name)) continue;

                (_cascadeOriginals ??= []).Add(name, parameter.GetFromComponent(this));
            }
        }

        if (isChanged is false) return;

        ClassBuilder.Reset();
        StyleBuilder.Reset();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The public properties of a component type are preserved for its parameters, and every params type by the DynamicDependency of the component that reads it.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The public properties of a component type are preserved for its parameters, and every params type by the DynamicDependency of the component that reads it.")]
    private static CascadeMap? GetCascadeMap(Type type)
    {
        return _cascadeMaps.GetOrAdd(type, static t =>
        {
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var cascades = properties.Where(p => p.IsDefined(typeof(CascadingParameterAttribute))
                                              && typeof(IBitComponentParams).IsAssignableFrom(p.PropertyType)
                                              && p.CanRead)
                                     .ToArray();

            if (cascades.Length != 1) return null;

            var cascade = cascades[0];
            var parameters = new Dictionary<string, CascadeParameter>(StringComparer.Ordinal);

            foreach (var source in cascade.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // The name is what the params object is read by, and the attributes are merged into the ones the
                // component collects afresh on every render, so neither is a parameter left behind.
                if (source.Name is nameof(IBitComponentParams.Name) or nameof(HtmlAttributes)) continue;
                if (source.CanRead is false || source.GetIndexParameters().Length > 0) continue;

                var target = properties.Where(p => p.Name == source.Name
                                                && p.IsDefined(typeof(ParameterAttribute))
                                                && p.GetSetMethod() is not null)
                                       .OrderByDescending(p => Depth(p.DeclaringType))
                                       .FirstOrDefault();

                if (target is null) continue;

                var valueType = Nullable.GetUnderlyingType(source.PropertyType) ?? source.PropertyType;

                if (target.PropertyType.IsAssignableFrom(valueType) is false
                    && target.PropertyType != source.PropertyType) continue;

                parameters.Add(source.Name, CascadeParameter.Create(source, target));
            }

            return parameters.Count == 0 ? null : new CascadeMap(cascade, parameters);
        });

        static int Depth(Type? type)
        {
            var depth = 0;

            for (; type is not null; type = type.BaseType) depth++;

            return depth;
        }
    }



    /// <summary>
    /// How a component type reads its params object, and the parameters that object can supply to it.
    /// </summary>
    private sealed class CascadeMap(PropertyInfo cascade, Dictionary<string, CascadeParameter> parameters)
    {
        public Dictionary<string, CascadeParameter> Parameters { get; } = parameters;

        public IBitComponentParams? GetParams(BitComponentBase component) => (IBitComponentParams?)cascade.GetValue(component);
    }

    /// <summary>
    /// One parameter a params object can supply: where it is read from on the params object and where it is written
    /// to on the component.
    /// </summary>
    private class CascadeParameter(PropertyInfo source, PropertyInfo target)
    {
        public static CascadeParameter Create(PropertyInfo source, PropertyInfo target)
        {
            // Dir reads through to the direction cascaded from above when it is not set, so what it holds of its own
            // is its backing field, which is what has to be put back rather than the direction it happens to show.
            return target.Name == nameof(BitComponentBase.Dir) && target.DeclaringType == typeof(BitComponentBase)
                ? new DirParameter(source, target)
                : new CascadeParameter(source, target);
        }

        public object? GetFromParams(IBitComponentParams parameters) => source.GetValue(parameters);

        public virtual object? GetFromComponent(BitComponentBase component) => target.GetValue(component);

        public virtual void SetOnComponent(BitComponentBase component, object? value) => target.SetValue(component, value);
    }

    private sealed class DirParameter(PropertyInfo source, PropertyInfo target) : CascadeParameter(source, target)
    {
        public override object? GetFromComponent(BitComponentBase component) => component._dir;

        public override void SetOnComponent(BitComponentBase component, object? value) => component._dir = (BitDir?)value;
    }
}
