using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Provides the reading direction to every Bheadless component below it.
/// Components also accept their own <c>Dir</c> parameter which takes precedence.
///
/// <code>
/// &lt;BitBhDirectionProvider Dir="BitBhDirection.Rtl"&gt;
///     &lt;Router … /&gt;
/// &lt;/BitBhDirectionProvider&gt;
/// </code>
/// </summary>
public sealed class BitBhDirectionProvider : ComponentBase
{
    /// <summary>The reading direction. Default: <see cref="BitBhDirection.Ltr"/>.</summary>
    [Parameter]
    public BitBhDirection Dir { get; set; }

    /// <summary>Child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BitBhDirection>>(0);
        builder.AddComponentParameter(1, "Value", Dir);
        builder.AddComponentParameter(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
