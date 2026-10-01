using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bit.BlazorUI;

public abstract partial class BitComponentBase
{
    private const string CallOnSetAttributeName = "Bit.BlazorUI.CallOnSetAttribute";
    private const string CallOnSetAsyncAttributeName = "Bit.BlazorUI.CallOnSetAsyncAttribute";

    private static readonly ConcurrentDictionary<Type, CascadeMap?> _cascadeMaps = new();

    // The map of this component's type, looked up once rather than on every render.
    private CascadeMap? _cascadeMap;
    private bool _isCascadeMapResolved;

    // The params object this component took its defaults from the last time, and the value each parameter it has
    // supplied since held before it first did. A value written by the params object is not one the markup passes
    // again on the next render, so Blazor never overwrites it: once the params object stops supplying it - the
    // property cleared, the object removed, an isolated BitParams put in between - it is put back from here.
    private IBitComponentParams? _cascadedParams;
    private Dictionary<string, object?>? _cascadeOriginals;

    // The parameters the params object supplies while the markup sets them, so that there is no value of the
    // component's own to record for them yet: the moment the markup lets go of one, the value it leaves behind is.
    private HashSet<string>? _cascadeSetByMarkup;



    /// <summary>
    /// Whether the markup set the named parameter on this render. Each class that tracks its own parameters adds
    /// them, so the answer covers the whole hierarchy rather than the one level a HasNotBeenSet belongs to.
    /// </summary>
    private protected virtual bool IsSetByMarkup(string name) => _assignedParameters.Contains(name);

    /// <summary>
    /// The params object cascaded to this component, if it reads one. The generated code of a component reads its
    /// cascading parameter directly; any other one is read through reflection.
    /// </summary>
    private protected virtual IBitComponentParams? CascadedParams => GetCascadeMap()?.GetParams(this);



    /// <summary>
    /// Puts back every parameter the params object of this component supplied before and no longer does, and
    /// remembers the value each newly supplied one replaces. Runs once every parameter is assigned and before the
    /// params object is applied, which is when a parameter still holds the value it had before the cascade.
    /// </summary>
    /// <returns>
    /// The setup hooks of the restored parameters that run asynchronously, or a completed task when there are none.
    /// </returns>
    private Task RestoreDroppedCascadeParameters()
    {
        var current = CascadedParams;

        // Nothing supplied now and nothing before, which is where every component outside a BitParams stays.
        if (current is null && _cascadedParams is null) return Task.CompletedTask;

        var map = GetCascadeMap();

        if (map is null) return Task.CompletedTask;

        List<CascadeParameter>? restored = null;

        // BitParams hands down a new object whenever what it carries changes, so the same object supplies the same
        // parameters, and only what the markup took over or let go of since is left to keep track of.
        if (ReferenceEquals(current, _cascadedParams) is false)
        {
            _cascadedParams = current;

            if (_cascadeOriginals is { Count: > 0 })
            {
                List<string>? dropped = null;

                foreach (var name in _cascadeOriginals.Keys)
                {
                    if (current is not null && map.Parameters[name].GetFromParams(current) is not null) continue;

                    (dropped ??= []).Add(name);
                }

                foreach (var name in dropped ?? [])
                {
                    var original = _cascadeOriginals[name];

                    _cascadeOriginals.Remove(name);

                    if (IsSetByMarkup(name)) continue;

                    var parameter = map.Parameters[name];

                    if (parameter.SetOnComponent(this, original) is false) continue;

                    (restored ??= []).Add(parameter);
                }
            }

            if (_cascadeSetByMarkup is { Count: > 0 })
            {
                _cascadeSetByMarkup.RemoveWhere(name => current is null || map.Parameters[name].GetFromParams(current) is null);
            }

            if (current is not null)
            {
                foreach (var (name, parameter) in map.Parameters)
                {
                    if (parameter.GetFromParams(current) is null) continue;
                    if (_cascadeOriginals?.ContainsKey(name) is true) continue;
                    if (_cascadeSetByMarkup?.Contains(name) is true) continue;

                    if (IsSetByMarkup(name))
                    {
                        (_cascadeSetByMarkup ??= []).Add(name);
                    }
                    else
                    {
                        (_cascadeOriginals ??= []).Add(name, parameter.GetFromComponent(this));
                    }
                }
            }
        }

        TrackMarkupChanges(map);

        if (restored is null) return Task.CompletedTask;

        ClassBuilder.Reset();
        StyleBuilder.Reset();

        // The hooks keep the state derived from a parameter in step with it, exactly as they do when the markup or
        // the params object sets one, so they run once every parameter is back.
        List<Task>? pending = null;

        foreach (var parameter in restored)
        {
            var task = parameter.RunSetupHooks(this);

            if (task.IsCompletedSuccessfully) continue;

            (pending ??= []).Add(task);
        }

        return pending is null ? Task.CompletedTask : Task.WhenAll(pending);
    }

    /// <summary>
    /// Moves a supplied parameter the markup has started to set out of the recorded ones, since what the markup
    /// gives it is what it holds of its own now, and records one the markup has stopped setting with the value it
    /// left behind, which is what goes back once the params object stops supplying it too.
    /// </summary>
    private void TrackMarkupChanges(CascadeMap map)
    {
        if (_cascadeOriginals is { Count: > 0 })
        {
            List<string>? takenOver = null;

            foreach (var name in _cascadeOriginals.Keys)
            {
                if (IsSetByMarkup(name)) (takenOver ??= []).Add(name);
            }

            foreach (var name in takenOver ?? [])
            {
                _cascadeOriginals.Remove(name);

                (_cascadeSetByMarkup ??= []).Add(name);
            }
        }

        if (_cascadeSetByMarkup is { Count: > 0 })
        {
            List<string>? letGo = null;

            foreach (var name in _cascadeSetByMarkup)
            {
                if (IsSetByMarkup(name) is false) (letGo ??= []).Add(name);
            }

            foreach (var name in letGo ?? [])
            {
                _cascadeSetByMarkup.Remove(name);

                (_cascadeOriginals ??= []).Add(name, map.Parameters[name].GetFromComponent(this));
            }
        }
    }

    private CascadeMap? GetCascadeMap()
    {
        if (_isCascadeMapResolved) return _cascadeMap;

        _cascadeMap = GetCascadeMap(GetType());
        _isCascadeMapResolved = true;

        return _cascadeMap;
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
    /// One parameter a params object can supply: where it is read from on the params object, where it is written
    /// to on the component, and the setup hooks the component runs whenever it changes.
    /// </summary>
    private class CascadeParameter
    {
        private readonly PropertyInfo _source;
        private readonly PropertyInfo _target;
        private readonly MethodInfo? _onSet;
        private readonly MethodInfo? _onSetAsync;

        protected CascadeParameter(PropertyInfo source, PropertyInfo target)
        {
            _source = source;
            _target = target;
            _onSet = FindHook(target, CallOnSetAttributeName);
            _onSetAsync = FindHook(target, CallOnSetAsyncAttributeName);
        }

        public static CascadeParameter Create(PropertyInfo source, PropertyInfo target)
        {
            // Dir reads through to the direction cascaded from above when it is not set, so what it holds of its own
            // is its backing field, which is what has to be put back rather than the direction it happens to show.
            return target.Name == nameof(BitComponentBase.Dir) && target.DeclaringType == typeof(BitComponentBase)
                ? new DirParameter(source, target)
                : new CascadeParameter(source, target);
        }

        public object? GetFromParams(IBitComponentParams parameters) => _source.GetValue(parameters);

        public virtual object? GetFromComponent(BitComponentBase component) => _target.GetValue(component);

        /// <summary>
        /// Writes the value onto the component, and tells whether that changed what it held.
        /// </summary>
        public virtual bool SetOnComponent(BitComponentBase component, object? value)
        {
            if (Equals(_target.GetValue(component), value)) return false;

            _target.SetValue(component, value);

            return true;
        }

        /// <summary>
        /// Runs what the [CallOnSet] and [CallOnSetAsync] attributes of the parameter name, which the generated
        /// code runs whenever the markup changes it.
        /// </summary>
        public Task RunSetupHooks(BitComponentBase component)
        {
            _onSet?.Invoke(component, null);

            return _onSetAsync?.Invoke(component, null) as Task ?? Task.CompletedTask;
        }

        // The attributes are internal to every assembly that declares components, so they are matched by name.
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "A setup hook is called by the generated code of its component, so it is never trimmed away.")]
        private static MethodInfo? FindHook(PropertyInfo target, string attributeName)
        {
            var attribute = target.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType.FullName == attributeName);

            if (attribute?.ConstructorArguments.FirstOrDefault().Value is not string name) return null;

            return target.DeclaringType?.GetMethod(name,
                                                   BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                   Type.EmptyTypes);
        }
    }

    private sealed class DirParameter(PropertyInfo source, PropertyInfo target) : CascadeParameter(source, target)
    {
        public override object? GetFromComponent(BitComponentBase component) => component._dir;

        public override bool SetOnComponent(BitComponentBase component, object? value)
        {
            if (component._dir == (BitDir?)value) return false;

            component._dir = (BitDir?)value;

            return true;
        }
    }
}
