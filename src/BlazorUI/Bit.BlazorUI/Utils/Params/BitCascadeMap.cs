using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Bit.BlazorUI;

/// <summary>
/// How a component type reads its params object, and the parameters that object can supply to it - read off the
/// types once and shared by every instance of the component.
/// </summary>
internal sealed class BitCascadeMap
{
    private const string CallOnSetAttributeName = "Bit.BlazorUI.CallOnSetAttribute";
    private const string CallOnSetAsyncAttributeName = "Bit.BlazorUI.CallOnSetAsyncAttribute";
    private const string ResetClassBuilderAttributeName = "Bit.BlazorUI.ResetClassBuilderAttribute";
    private const string ResetStyleBuilderAttributeName = "Bit.BlazorUI.ResetStyleBuilderAttribute";

    private static readonly ConcurrentDictionary<Type, BitCascadeMap?> _maps = new();

    private readonly PropertyInfo _cascade;



    private BitCascadeMap(PropertyInfo cascade, Dictionary<string, BitCascadeParameter> parameters)
    {
        _cascade = cascade;
        Parameters = parameters;
    }



    /// <summary>
    /// Every parameter the params object can supply, by name.
    /// </summary>
    public Dictionary<string, BitCascadeParameter> Parameters { get; }

    /// <summary>
    /// The params object cascaded to the component, read through reflection.
    /// </summary>
    public IBitComponentParams? GetParams(object component) => (IBitComponentParams?)_cascade.GetValue(component);



    /// <summary>
    /// The map of a component type, or null for one that reads no params object or that has nothing it can take
    /// from one.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The public properties of a component type are preserved for its parameters, and every params type by the DynamicDependency of the component that reads it.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The public properties of a component type are preserved for its parameters, and every params type by the DynamicDependency of the component that reads it.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The public properties of a component type are preserved for its parameters, and every params type by the DynamicDependency of the component that reads it.")]
    public static BitCascadeMap? For(Type type)
    {
        return _maps.GetOrAdd(type, static t =>
        {
            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var cascades = properties.Where(p => p.IsDefined(typeof(CascadingParameterAttribute))
                                              && typeof(IBitComponentParams).IsAssignableFrom(p.PropertyType)
                                              && p.CanRead)
                                     .ToArray();

            if (cascades.Length != 1) return null;

            var cascade = cascades[0];
            var parameters = new Dictionary<string, BitCascadeParameter>(StringComparer.Ordinal);

            // Every parameter of the component, the most derived one of each name: the ones the params object cannot
            // supply count too, since a template set in the markup outranks a cascaded icon just as well.
            var componentParameters = properties.Where(p => p.IsDefined(typeof(ParameterAttribute)))
                                                .GroupBy(p => p.Name, StringComparer.Ordinal)
                                                .ToDictionary(g => g.Key,
                                                              g => g.OrderByDescending(p => Depth(p.DeclaringType)).First(),
                                                              StringComparer.Ordinal);

            var settings = Settings(componentParameters);

            foreach (var source in cascade.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // The name is what the params object is read by, and the attributes are merged into the ones the
                // component collects afresh on every render, so neither is a parameter left behind.
                if (source.Name is nameof(IBitComponentParams.Name) or nameof(BitComponentBase.HtmlAttributes)) continue;
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

                var peers = settings.GetValueOrDefault(source.Name)?.Select(n => componentParameters[n]).ToArray() ?? [];

                parameters.Add(source.Name, BitCascadeParameter.Create(source, target, peers));
            }

            return parameters.Count == 0 ? null : new BitCascadeMap(cascade, parameters);
        });

        static int Depth(Type? type)
        {
            var depth = 0;

            for (; type is not null; type = type.BaseType) depth++;

            return depth;
        }
    }



    // The ends of the names that take one icon, longest first so that each name is read by the end it really has.
    private static readonly string[] _iconSuffixes = ["IconTemplate", "IconNames", "IconName", "IconUrl", "Icons", "Icon"];

    /// <summary>
    /// The parameters each parameter of a component is one setting with, in a way that a value of the component's own
    /// for any of them outranks one the params object supplies for it.
    /// </summary>
    /// <remarks>
    /// An icon is taken through several parameters, the first of them set winning: an XIcon, its XIconName, its
    /// XIconUrl, an XIconTemplate drawn in its place, the XIcons / XIconNames maps that pick one per state and a
    /// GetXIcon selector that picks one per item. Their names make them one setting, so a component that picks its
    /// icon through any of them takes none of the others from the cascade: a cascaded XIcon filled in beside its own
    /// XIconName would replace the icon it asked for, and a cascaded XIconName beside its own XIcon would leave a
    /// value on it that says something it does not show. What the names do not tell - a text or a template shown in
    /// place of an icon, an icon that replaces another one while the component is in a state - is declared with
    /// <see cref="OutranksAttribute"/>, which only goes one way: a cascaded OnIcon must not replace the Icon a toggle
    /// button picked, while a cascaded Icon is still what it shows when off, beside an OnIcon of its own.
    /// </remarks>
    private static Dictionary<string, HashSet<string>> Settings(Dictionary<string, PropertyInfo> componentParameters)
    {
        var bySetting = componentParameters.Keys.GroupBy(SettingOf, StringComparer.Ordinal)
                                           .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

        var outranked = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var (name, property) in componentParameters)
        {
            foreach (var attribute in property.GetCustomAttributes<OutranksAttribute>(inherit: true))
            {
                if (componentParameters.ContainsKey(attribute.Parameter) is false)
                {
                    throw new InvalidOperationException($"{property.DeclaringType?.Name}.{name} outranks {attribute.Parameter}, which is not one of its parameters.");
                }

                var setting = SettingOf(name);

                if (outranked.TryGetValue(setting, out var others) is false)
                {
                    outranked.Add(setting, others = new(StringComparer.Ordinal));
                }

                others.Add(SettingOf(attribute.Parameter));
            }
        }

        var peers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var name in componentParameters.Keys)
        {
            var setting = SettingOf(name);
            var names = new HashSet<string>(bySetting[setting], StringComparer.Ordinal);

            foreach (var other in outranked.GetValueOrDefault(setting) ?? [])
            {
                names.UnionWith(bySetting[other]);
            }

            names.Remove(name);

            if (names.Count > 0) peers.Add(name, names);
        }

        return peers;

        static string SettingOf(string name)
        {
            // GetSelectedIcon picks the SelectedIcon of one item.
            if (name.Length >= 7 && name.StartsWith("Get", StringComparison.Ordinal) && name.EndsWith("Icon", StringComparison.Ordinal)) return name[3..];

            foreach (var suffix in _iconSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.Ordinal)) return string.Concat(name.AsSpan(0, name.Length - suffix.Length), "Icon");
            }

            return name;
        }
    }



    /// <summary>
    /// One parameter a params object can supply: where it is read from on the params object, where it is written
    /// to on the component, and the setup hooks the component runs whenever it changes.
    /// </summary>
    internal class BitCascadeParameter
    {
        private readonly PropertyInfo _source;
        private readonly PropertyInfo _target;
        private readonly MethodInfo? _onSet;
        private readonly MethodInfo? _onSetAsync;
        private readonly PropertyInfo[] _peers;

        protected BitCascadeParameter(PropertyInfo source, PropertyInfo target, PropertyInfo[] peers)
        {
            _source = source;
            _target = target;
            _peers = peers;
            _onSet = FindHook(target, CallOnSetAttributeName);
            _onSetAsync = FindHook(target, CallOnSetAsyncAttributeName);

            var attributes = target.GetCustomAttributesData();

            ResetsClass = attributes.Any(a => a.AttributeType.FullName == ResetClassBuilderAttributeName);
            ResetsStyle = attributes.Any(a => a.AttributeType.FullName == ResetStyleBuilderAttributeName);

            // A parameter that names the string it is built into rebuilds only that one. Many name neither while
            // either string still reads them (a Classes the root class is built from, a parameter of a base class
            // whose SetParametersAsync is written by hand), so one that names neither rebuilds both.
            if (ResetsClass is false && ResetsStyle is false)
            {
                ResetsClass = ResetsStyle = true;
            }
        }



        /// <summary>
        /// Whether the class string of the component is built from the parameter, so that it is rebuilt once the
        /// parameter changes.
        /// </summary>
        public bool ResetsClass { get; }

        /// <summary>
        /// Whether the style string of the component is built from the parameter, so that it is rebuilt once the
        /// parameter changes.
        /// </summary>
        public bool ResetsStyle { get; }

        /// <summary>
        /// Whether the parameter has a [CallOnSetAsync] hook, which a params object applied from a synchronous
        /// OnParametersSet has no way to await.
        /// </summary>
        public bool HasAsyncSetupHook => _onSetAsync is not null;

        /// <summary>
        /// The name of the parameter, on the params object and on the component alike.
        /// </summary>
        public string Name => _source.Name;

        public static BitCascadeParameter Create(PropertyInfo source, PropertyInfo target, PropertyInfo[] peers)
        {
            // Dir reads through to the direction cascaded from above when it is not set, so what it holds of its own
            // is its backing field, which is what has to be put back rather than the direction it happens to show.
            return target.Name == nameof(BitComponentBase.Dir) && typeof(IBitCascadeTarget).IsAssignableFrom(target.DeclaringType)
                ? new DirParameter(source, target, peers)
                : new BitCascadeParameter(source, target, peers);
        }

        /// <summary>
        /// Whether the params object supplies the parameter, by the rule its UpdateParameters writes it by.
        /// </summary>
        public bool IsSuppliedBy(IBitComponentParams parameters) => BitCascadeExtensions.IsSupplied(_source.GetValue(parameters));

        /// <summary>
        /// Whether the component has made its own choice of the setting the parameter is part of (see
        /// <see cref="Settings"/>), which outranks whatever the params object supplies for it: the markup set one of
        /// the parameters it is one setting with, to a value that supplies it. One written as null or "" - an icon
        /// bound to an item that has none - picks nothing, and leaves the cascade to fill the setting in.
        /// </summary>
        public bool IsOutrankedOn(IBitCascadeTarget component)
        {
            foreach (var peer in _peers)
            {
                if (component.IsSetByMarkup(peer.Name) && BitCascadeExtensions.IsSupplied(peer.GetValue(component))) return true;
            }

            return false;
        }

        public virtual object? GetFromComponent(IBitCascadeTarget component) => _target.GetValue(component);

        /// <summary>
        /// Writes the value onto the component, and tells whether that changed what it held.
        /// </summary>
        public virtual bool SetOnComponent(IBitCascadeTarget component, object? value)
        {
            if (BitCascadeExtensions.AreSame(_target.GetValue(component), value)) return false;

            _target.SetValue(component, value);

            return true;
        }

        /// <summary>
        /// Runs what the [CallOnSet] and [CallOnSetAsync] attributes of the parameter name, which the generated
        /// code runs whenever the markup changes it.
        /// </summary>
        public Task RunSetupHooks(IBitCascadeTarget component)
        {
            RunSetupHook(component);

            return _onSetAsync?.Invoke(component, null) as Task ?? Task.CompletedTask;
        }

        /// <summary>
        /// Runs what the [CallOnSet] attribute of the parameter names: the synchronous half of
        /// <see cref="RunSetupHooks"/>, which is all a params object applied from OnParametersSet can run.
        /// </summary>
        public void RunSetupHook(IBitCascadeTarget component) => _onSet?.Invoke(component, null);

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

    private sealed class DirParameter(PropertyInfo source, PropertyInfo target, PropertyInfo[] peers) : BitCascadeParameter(source, target, peers)
    {
        public override object? GetFromComponent(IBitCascadeTarget component) => component.OwnDir;

        public override bool SetOnComponent(IBitCascadeTarget component, object? value)
        {
            if (component.OwnDir == (BitDir?)value) return false;

            component.OwnDir = (BitDir?)value;

            return true;
        }
    }
}
