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

                parameters.Add(source.Name, BitCascadeParameter.Create(source, target));
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

        protected BitCascadeParameter(PropertyInfo source, PropertyInfo target)
        {
            _source = source;
            _target = target;
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

        public static BitCascadeParameter Create(PropertyInfo source, PropertyInfo target)
        {
            // Dir reads through to the direction cascaded from above when it is not set, so what it holds of its own
            // is its backing field, which is what has to be put back rather than the direction it happens to show.
            return target.Name == nameof(BitComponentBase.Dir) && typeof(IBitCascadeTarget).IsAssignableFrom(target.DeclaringType)
                ? new DirParameter(source, target)
                : new BitCascadeParameter(source, target);
        }

        /// <summary>
        /// Whether the params object supplies the parameter, by the rule its UpdateParameters writes it by.
        /// </summary>
        public bool IsSuppliedBy(IBitComponentParams parameters) => BitCascadeExtensions.IsSupplied(_source.GetValue(parameters));

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

    private sealed class DirParameter(PropertyInfo source, PropertyInfo target) : BitCascadeParameter(source, target)
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
