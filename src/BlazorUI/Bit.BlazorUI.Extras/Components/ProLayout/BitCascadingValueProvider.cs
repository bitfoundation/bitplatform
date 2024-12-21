using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

public class BitCascadingValueProvider : ComponentBase
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private Type _cascadingValueType = typeof(CascadingValue<>);

    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public IEnumerable<BitCascadingValue> Values { get; set; } = [];

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValue))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        RenderFragment? rf = ChildContent;

        foreach (var value in Values)
        {
            if (value.Value is null) continue;

            var r = rf;
            var s = seq;
            var v = value;

            rf = b => { CreateCascadingValue(b, s, v.Name, v.Value, r); };

            seq += string.IsNullOrEmpty(v.Name) ? 3 : 4;
        }

        builder.AddContent(seq, rf);
    }


    private void CreateCascadingValue(RenderTreeBuilder builder,
        int seq,
        string? name,
        object value,
        RenderFragment? childContent)
    {
#pragma warning disable IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
        builder.OpenComponent(seq, _cascadingValueType.MakeGenericType(value.GetType()));
#pragma warning restore IL2055 // Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.
        if (string.IsNullOrEmpty(name) is false)
        {
            builder.AddComponentParameter(++seq, "Name", name);
        }
        builder.AddComponentParameter(++seq, "Value", value);
        builder.AddComponentParameter(++seq, "ChildContent", childContent);
        builder.CloseComponent();
    }
}
