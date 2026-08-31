using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A component that provides a list of cascading values to all descendant components.
/// It renders one nested CascadingValue component per value, in the order the values are listed, so a value
/// listed later shadows an earlier one of the same type or name, exactly like a nested CascadingValue would.
/// </summary>
public class BitCascadingValueProvider : ComponentBase
{
    /// <summary>
    /// The number of render tree sequence slots reserved for each generated CascadingValue component.
    /// </summary>
    private const int SequenceStep = 5;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type _cascadingValueType = typeof(CascadingValue<>);

    private static readonly ConcurrentDictionary<Type, Type> _cascadingValueTypeCache = new();



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

        RenderFragment current = ChildContent ?? (_ => { });

        for (int i = list.Count - 1; i > 0; i--)
        {
            var item = list[i];
            var prev = current;

            current = b => CreateCascadingValue(b, i * SequenceStep, item, prev);
        }

        CreateCascadingValue(builder, 0, list[0], current);
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



    /// <summary>
    /// Flattens the ValueList and the Values parameters into a single list, skipping the null and the disabled entries.
    /// Returns null when there is nothing to cascade so that the child content can be rendered as is.
    /// </summary>
    private List<BitCascadingValue>? CollectValues()
    {
        if (Values is null && ValueList is { Count: > 0 } valueList && IsUsable(valueList))
        {
            return valueList;
        }

        List<BitCascadingValue>? result = null;

        Collect(ValueList);
        Collect(Values);

        return result;

        void Collect(IEnumerable<BitCascadingValue>? source)
        {
            if (source is null) return;

            foreach (var item in source)
            {
                if (item is null || item.Enabled is false) continue;

                (result ??= []).Add(item);
            }
        }
    }

    private static bool IsUsable(List<BitCascadingValue> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            var item = values[i];

            if (item is null || item.Enabled is false) return false;
        }

        return true;
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
}
