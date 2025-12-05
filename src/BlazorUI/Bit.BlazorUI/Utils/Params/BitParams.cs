using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// Provides cascading parameter values for the bit BlazorUI components.
/// </summary>
public class BitParams : ComponentBase
{
    /// <summary>
    /// The content to which the values should be provided.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// List of parameters to provide for the children components.
    /// </summary>
    [Parameter] public IEnumerable<IBitComponentParams>? Parameters { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        foreach (var parameter in parameters)
        {
            switch (parameter.Name)
            {
                case nameof(ChildContent):
                    ChildContent = (RenderFragment?)parameter.Value;
                    break;

                case nameof(Parameters):
                    Parameters = (IEnumerable<IBitComponentParams>?)parameter.Value;
                    break;
            }
        }

        return base.SetParametersAsync(ParameterView.Empty);
    }



    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IBitComponentParams))]
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;
        var values = new List<BitCascadingValue>();

        foreach (var compParams in Parameters ?? [])
        {
            if (compParams is null) continue;

            values.Add(new(compParams, compParams.Name, true));
        }

#pragma warning disable ASP0006, IL2110
        builder.OpenComponent<BitCascadingValueProvider>(seq++);
        builder.AddComponentParameter(seq++, nameof(BitCascadingValueProvider.Values), values);
        builder.AddComponentParameter(seq++, nameof(BitCascadingValueProvider.ChildContent), ChildContent);
        builder.CloseComponent();
#pragma warning restore ASP0006, IL2110

        base.BuildRenderTree(builder);
    }
}
