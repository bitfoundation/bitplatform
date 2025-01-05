using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// A component that provides a list of cascading values to all descendant components.
/// </summary>
public class BitCascadingValueProvider : ComponentBase
{
    /// <summary>
    /// The content to which the values should be provided.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The cascading values to be provided for the children.
    /// </summary>
    [Parameter] public IEnumerable<BitCascadingValue>? Values { get; set; }



    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type _cascadingValueType = typeof(CascadingValue<>);



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValue))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        RenderFragment? rf = ChildContent;

        foreach (var value in Values ?? [])
        {
            if (value.Value is null) continue;

            var r = rf;
            var s = seq;
            var v = value;

            rf = b => { CreateCascadingValue(b, s, v.Name, v.Value, v.IsFixed, r); };

            seq += v.Name.HasValue() ? 5 : 4;
        }

        builder.AddContent(seq, rf);
    }


    private void CreateCascadingValue(RenderTreeBuilder builder,
        int seq,
        string? name,
        object value,
        bool isFixed,
        RenderFragment? childContent)
    {
#pragma warning disable IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
        builder.OpenComponent(seq, _cascadingValueType.MakeGenericType(value.GetType()));
#pragma warning restore IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
        if (name.HasValue())
        {
            builder.AddComponentParameter(++seq, "Name", name);
        }
        builder.AddComponentParameter(++seq, "Value", value);
        builder.AddComponentParameter(++seq, "IsFixed", isFixed);
        builder.AddComponentParameter(++seq, "ChildContent", childContent);
        builder.CloseComponent();
    }
}
