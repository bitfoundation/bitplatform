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
            }
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitCascadingValue))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 1;

        RenderFragment current = ChildContent ?? (_ => { });

        if (Values is not null)
        {
            var list = Values.ToList();
            for (int i = list.Count - 1; i > 0; i--)
            {
                var item = list[i];
                var prev = current;
                current = b => CreateCascadingValue(b, i * 4, item.Name, item.Value, prev);
            }
            CreateCascadingValue(builder, 0, list[0].Name, list[0].Value, current);
        }
        else
        {
            ChildContent?.Invoke(builder);
        }


        base.BuildRenderTree(builder);
    }



    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    private static readonly Type _cascadingValueType = typeof(CascadingValue<>);

    public static void CreateCascadingValue(RenderTreeBuilder builder, int seq, string? name, object value, RenderFragment? innerBuilder)
    {
#pragma warning disable IL3050, IL2055
        builder.OpenComponent(seq, _cascadingValueType.MakeGenericType(value.GetType()));
#pragma warning restore IL3050, IL2055
        builder.AddComponentParameter(seq++, "Name", name);
        builder.AddComponentParameter(seq++, "Value", value);
        builder.AddComponentParameter(seq++, "ChildContent", innerBuilder);
        builder.CloseComponent();
    }
}
