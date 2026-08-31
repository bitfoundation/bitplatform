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
    /// The number of render tree sequence slots reserved for each value that is given to this provider.
    /// Five of them are the frames of one generated CascadingValue component; the other five are the slots
    /// the same value moves to whenever the shape of what it cascades - its type, its name or its IsFixed
    /// flag - changes, so that the CascadingValue is re-created instead of being reused with a different
    /// shape, which would keep the already matched consumers bound to the old one.
    /// </summary>
    private const int SequenceStep = 10;

    private static readonly RenderFragment _emptyContent = _ => { };

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type _cascadingValueType = typeof(CascadingValue<>);

    private static readonly ConcurrentDictionary<Type, Type> _cascadingValueTypeCache = new();



    private bool _disposed;

    // The values of the two parameters flattened in order, the ones actually rendered (the enabled values
    // that nothing later shadows) with the sequence number each of them renders at, the keys used to find
    // those, the shape the values were last rendered with, and the values this provider is subscribed to.
    // All of them are reused across renders so that a re-render allocates nothing of its own.
    private readonly List<BitCascadingValue> _allValues = [];
    private readonly List<BitCascadingValue> _renderedValues = [];
    private readonly List<int> _renderedSequences = [];
    private readonly List<ValueSlot> _slots = [];
    private readonly List<BitCascadingValue> _subscribedValues = [];
    private readonly HashSet<BitCascadingValue> _distinctSubscribedValues = [];
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
        if (CollectValues() is false)
        {
            ChildContent?.Invoke(builder);
            return;
        }

        RenderFragment current = ChildContent ?? _emptyContent;

        for (int i = _renderedValues.Count - 1; i > 0; i--)
        {
            var item = _renderedValues[i];
            var seq = _renderedSequences[i];
            var prev = current;

            current = b => CreateCascadingValue(b, seq, item, prev);
        }

        CreateCascadingValue(builder, _renderedSequences[0], _renderedValues[0], current);
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
    /// shadows, works out the sequence number each of them renders at, and re-subscribes to whatever the
    /// two parameters currently hold.
    /// Returns false when there is nothing to cascade so that the child content can be rendered as is.
    /// </summary>
    private bool CollectValues()
    {
        _allValues.Clear();

        Collect(ValueList);
        Collect(Values);

        UpdateSubscriptions();
        UpdateSlots();

        _renderedValues.Clear();
        _renderedSequences.Clear();
        _renderedKeys.Clear();

        // Walked from the last value to the first one, so that of the values sharing a type and a name it is
        // the last one - the one that shadows all the others - that makes it into the rendered chain.
        for (int i = _allValues.Count - 1; i >= 0; i--)
        {
            var item = _allValues[i];

            if (item.Enabled is false) continue;
            if (_renderedKeys.Add((item.ValueType, item.Name)) is false) continue;

            _renderedValues.Add(item);
            _renderedSequences.Add(i * SequenceStep + _slots[i].Parity * (SequenceStep / 2));
        }

        if (_renderedValues.Count == 0) return false;

        _renderedValues.Reverse();
        _renderedSequences.Reverse();

        return true;

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
    /// Keeps one slot per value, remembering the shape it was last rendered with, and flips the parity of a
    /// slot whose shape changed. The parity is what moves that value's CascadingValue to another range of
    /// sequence numbers, which is how the framework is told to create a new component rather than to hand
    /// the old one a shape it does not accept - a changed IsFixed is rejected outright, and a changed Name
    /// would leave the consumers that were already matched under the old name bound to it.
    /// </summary>
    private void UpdateSlots()
    {
        if (_slots.Count > _allValues.Count)
        {
            _slots.RemoveRange(_allValues.Count, _slots.Count - _allValues.Count);
        }

        for (int i = 0; i < _allValues.Count; i++)
        {
            var item = _allValues[i];
            var shape = new ValueShape(item.ValueType, item.Name, item.IsFixed);

            if (i == _slots.Count)
            {
                _slots.Add(new ValueSlot(shape, 0));
                continue;
            }

            var slot = _slots[i];

            if (slot.Shape == shape) continue;

            _slots[i] = new ValueSlot(shape, slot.Parity ^ 1);
        }
    }

    /// <summary>
    /// Points the change subscriptions at whatever the parameters currently hold, so that mutating any of
    /// those values re-renders this provider. Disabled values are subscribed too, because enabling one of
    /// them is itself a change this provider has to react to, and a value listed more than once is
    /// subscribed only once, so that it never triggers more than one re-render.
    /// </summary>
    private void UpdateSubscriptions()
    {
        if (IsSubscribedTo(_allValues)) return;

        Unsubscribe();

        _subscribedValues.AddRange(_allValues);

        for (int i = 0; i < _subscribedValues.Count; i++)
        {
            var item = _subscribedValues[i];

            if (_distinctSubscribedValues.Add(item) is false) continue;

            item.ChangedAsync += HandleValueChangedAsync;
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
        foreach (var item in _distinctSubscribedValues)
        {
            item.ChangedAsync -= HandleValueChangedAsync;
        }

        _distinctSubscribedValues.Clear();
        _subscribedValues.Clear();
    }

    private Task HandleValueChangedAsync(BitCascadingValue value)
    {
        if (_disposed) return Task.CompletedTask;

        try
        {
            return InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            // The renderer is already gone - a value changed from a background thread while this
            // provider was being torn down - so there is nothing left to refresh.
            return Task.CompletedTask;
        }
    }

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
    /// Everything about a value that the underlying CascadingValue component cannot be handed a new reading
    /// of, so that a change of any of them is rendered as a new component rather than as a parameter update.
    /// </summary>
    private readonly record struct ValueShape(Type ValueType, string? Name, bool IsFixed);

    private readonly record struct ValueSlot(ValueShape Shape, int Parity);

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
