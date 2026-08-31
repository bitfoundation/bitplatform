using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A component that provides a list of cascading values to all descendant components.
/// It renders one nested CascadingValue component per value, in the order the values are listed, so a value
/// listed later shadows an earlier one of the same type or name, exactly like a nested CascadingValue would.
/// A shadowed value is dropped from the rendered chain rather than being cascaded and then hidden, and the
/// provider listens to every value it is given, so changing one of them refreshes the consumers on its own.
/// </summary>
public class BitCascadingValueProvider : ComponentBase, IDisposable
{
    /// <summary>
    /// The number of render tree sequence slots reserved for each generated CascadingValue component.
    /// Five of them are the frames of the component itself; the other five are the slots the same component
    /// moves to when its value is fixed, so that toggling IsFixed re-creates the CascadingValue instead of
    /// tripping the framework's "The value of IsFixed cannot be changed dynamically" guard.
    /// </summary>
    private const int SequenceStep = 10;

    private static readonly RenderFragment _emptyContent = _ => { };

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type _cascadingValueType = typeof(CascadingValue<>);

    private static readonly ConcurrentDictionary<Type, Type> _cascadingValueTypeCache = new();



    private bool _disposed;

    // The values of the two parameters flattened in order, the ones actually rendered (the enabled values
    // that nothing later shadows), the keys used to find those, and the values this provider is currently
    // subscribed to. All four are reused across renders so that a re-render allocates nothing of its own.
    private readonly List<BitCascadingValue> _allValues = [];
    private readonly List<BitCascadingValue> _renderedValues = [];
    private readonly List<BitCascadingValue> _subscribedValues = [];
    private readonly HashSet<(Type ValueType, string? Name)> _renderedKeys = new(ValueKeyComparer.Instance);



    /// <summary>
    /// The content to which the values should be provided.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The cascading values to be provided for the children.
    /// The values of this parameter are provided after (so they take precedence over) the ones of the ValueList parameter.
    /// </summary>
    [Parameter] public IEnumerable<BitCascadingValue>? Values { get; set; }

    /// <summary>
    /// The cascading value list to be provided for the children.
    /// The values of this parameter are provided before (so they can be overridden by) the ones of the Values parameter.
    /// </summary>
    [Parameter] public BitCascadingValueList? ValueList { get; set; }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValue))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValueList))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var list = CollectValues();

        if (list is null)
        {
            ChildContent?.Invoke(builder);
            return;
        }

        RenderFragment current = ChildContent ?? _emptyContent;

        for (int i = list.Count - 1; i > 0; i--)
        {
            var item = list[i];
            var seq = GetSequence(i, item);
            var prev = current;

            current = b => CreateCascadingValue(b, seq, item, prev);
        }

        CreateCascadingValue(builder, GetSequence(0, list[0]), list[0], current);
    }



    /// <summary>
    /// Renders a CascadingValue component of the runtime type of the given BitCascadingValue into the render tree.
    /// </summary>
    /// <param name="builder">The render tree builder to render the CascadingValue component into.</param>
    /// <param name="seq">
    /// The starting sequence number of the generated frames.
    /// Five consecutive sequence numbers are consumed starting from this one.
    /// </param>
    /// <param name="cascadingValue">The cascading value to render.</param>
    /// <param name="childContent">The content that receives the cascading value.</param>
    public static void CreateCascadingValue(RenderTreeBuilder builder, int seq, BitCascadingValue cascadingValue, RenderFragment? childContent)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(cascadingValue);

        builder.OpenComponent(seq, GetCascadingValueType(cascadingValue.ValueType));

        if (cascadingValue.Name is not null)
        {
            builder.AddComponentParameter(seq + 1, "Name", cascadingValue.Name);
        }

        builder.AddComponentParameter(seq + 2, "Value", cascadingValue.Value);
        builder.AddComponentParameter(seq + 3, "IsFixed", cascadingValue.IsFixed);
        builder.AddComponentParameter(seq + 4, "ChildContent", childContent);

        builder.CloseComponent();
    }



    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Unsubscribe();
        }

        _disposed = true;
    }



    /// <summary>
    /// Flattens the ValueList and the Values parameters into the list of the values to render, skipping the
    /// null and the disabled entries as well as every entry that a later one of the same type and name
    /// shadows, and re-subscribes to whatever the two parameters currently hold.
    /// Returns null when there is nothing to cascade so that the child content can be rendered as is.
    /// </summary>
    private List<BitCascadingValue>? CollectValues()
    {
        _allValues.Clear();

        Collect(ValueList);
        Collect(Values);

        UpdateSubscriptions();

        _renderedValues.Clear();
        _renderedKeys.Clear();

        // Walked from the last value to the first one, so that of the values sharing a type and a name it is
        // the last one - the one that shadows all the others - that makes it into the rendered chain.
        for (int i = _allValues.Count - 1; i >= 0; i--)
        {
            var item = _allValues[i];

            if (item.Enabled is false) continue;
            if (_renderedKeys.Add((item.ValueType, item.Name)) is false) continue;

            _renderedValues.Add(item);
        }

        if (_renderedValues.Count == 0) return null;

        _renderedValues.Reverse();

        return _renderedValues;

        void Collect(IEnumerable<BitCascadingValue>? source)
        {
            if (source is null) return;

            foreach (var item in source)
            {
                if (item is null) continue;

                _allValues.Add(item);
            }
        }
    }

    /// <summary>
    /// Points the change subscriptions at whatever the parameters currently hold, so that mutating any of
    /// those values re-renders this provider. Disabled values are subscribed too, because enabling one of
    /// them is itself a change this provider has to react to.
    /// </summary>
    private void UpdateSubscriptions()
    {
        if (IsSubscribedTo(_allValues)) return;

        Unsubscribe();

        _subscribedValues.AddRange(_allValues);

        for (int i = 0; i < _subscribedValues.Count; i++)
        {
            _subscribedValues[i].Changed += HandleValueChanged;
        }
    }

    private bool IsSubscribedTo(List<BitCascadingValue> values)
    {
        if (_subscribedValues.Count != values.Count) return false;

        for (int i = 0; i < values.Count; i++)
        {
            if (ReferenceEquals(_subscribedValues[i], values[i]) is false) return false;
        }

        return true;
    }

    private void Unsubscribe()
    {
        for (int i = 0; i < _subscribedValues.Count; i++)
        {
            _subscribedValues[i].Changed -= HandleValueChanged;
        }

        _subscribedValues.Clear();
    }

    private void HandleValueChanged(BitCascadingValue value)
    {
        if (_disposed) return;

        try
        {
            _ = InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // The renderer is already gone - a value changed from a background thread while this
            // provider was being torn down - so there is nothing left to refresh.
        }
    }

    private static int GetSequence(int index, BitCascadingValue value) => index * SequenceStep + (value.IsFixed ? SequenceStep / 2 : 0);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static Type GetCascadingValueType(Type valueType)
    {
#pragma warning disable IL2073
        return _cascadingValueTypeCache.GetOrAdd(valueType, static type =>
        {
#pragma warning disable IL3050, IL2055
            return _cascadingValueType.MakeGenericType(type);
#pragma warning restore IL3050, IL2055
        });
#pragma warning restore IL2073
    }



    /// <summary>
    /// Identifies a cascading value the way a consumer does: by the cascaded type and by the name, which the
    /// CascadingValue component matches case-insensitively.
    /// </summary>
    private sealed class ValueKeyComparer : IEqualityComparer<(Type ValueType, string? Name)>
    {
        public static readonly ValueKeyComparer Instance = new();

        public bool Equals((Type ValueType, string? Name) x, (Type ValueType, string? Name) y)
            => x.ValueType == y.ValueType && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((Type ValueType, string? Name) obj)
            => HashCode.Combine(obj.ValueType, obj.Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
    }
}
