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

    /// <summary>
    /// The cascading value list to be provided for the children.
    /// </summary>
    [Parameter] public BitCascadingValueList? ValueList { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var parameter in parameters)
        {
            switch (parameter.Name)
            {
                case nameof(ChildContent):
                    ChildContent = (RenderFragment?)parameter.Value;
                    break;

                case nameof(Values):
                    Values = (IEnumerable<BitCascadingValue>?)parameter.Value;
                    break;

                case nameof(ValueList):
                    ValueList = (BitCascadingValueList?)parameter.Value;
                    break;
            }
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValue))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValueList))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        RenderFragment current = ChildContent ?? (_ => { });

        var list = Values?.ToList() ?? ValueList;

        if (list is not null && list.Count > 0)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                var item = list[i];
                var prev = current;

                if (item is null) continue;

                current = b => CreateCascadingValue(b, i * 5, item, prev);
            }
            CreateCascadingValue(builder, 0, list[0], current);
        }
        else
        {
            ChildContent?.Invoke(builder);
        }

        base.BuildRenderTree(builder);
    }



    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type _cascadingValueType = typeof(CascadingValue<>);

    public static void CreateCascadingValue(RenderTreeBuilder builder, int seq, BitCascadingValue cascadingValue, RenderFragment? innerBuilder)
    {
#pragma warning disable IL3050, IL2055
        builder.OpenComponent(seq, _cascadingValueType.MakeGenericType(cascadingValue.ValueType));
#pragma warning restore IL3050, IL2055

        if (cascadingValue.Name is not null)
        {
            builder.AddComponentParameter(seq++, "Name", cascadingValue.Name);
        }

        builder.AddComponentParameter(seq++, "Value", cascadingValue.Value);
        builder.AddComponentParameter(seq++, "IsFixed", cascadingValue.IsFixed);

        builder.AddComponentParameter(seq++, "ChildContent", innerBuilder);

        builder.CloseComponent();
    }
}
