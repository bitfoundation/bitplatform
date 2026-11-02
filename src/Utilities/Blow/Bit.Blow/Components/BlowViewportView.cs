using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Blow;

/// <summary>
/// Renders its content from the canvas's current viewport, and re-renders it when the viewport changes - the way a
/// node or edge template draws level-of-detail content: a full card zoomed in, a title alone zoomed out.
/// </summary>
/// <remarks>
/// <para>
/// Node and edge templates are not re-rendered when the user pans or zooms - on a large graph that would be every
/// node on every frame - so a template that reads the zoom has to say so. Wrap the part that depends on it:
/// </para>
/// <code>
/// &lt;BlowViewportView Select="@(v =&gt; v.Zoom &gt;= 0.6)" Context="viewport"&gt;
///     @if (viewport.Zoom &gt;= 0.6) { &lt;NodeDetails Node="node" /&gt; } else { &lt;strong&gt;@node.Label&lt;/strong&gt; }
/// &lt;/BlowViewportView&gt;
/// </code>
/// <para>
/// <see cref="Select"/> is what keeps that cheap: the content is re-rendered only when the value it returns changes -
/// here, when the zoom crosses 0.6 - rather than on every frame of a pan. Without it the content follows every
/// change of the viewport. It renders no element of its own, so it works inside SVG edge templates too.
/// </para>
/// </remarks>
public sealed class BlowViewportView : ComponentBase
{
    [CascadingParameter(Name = BlowCascadingNames.Context)] private IBlowContext? Flow { get; set; }
    [CascadingParameter(Name = BlowCascadingNames.ViewportVersion)] private int ViewportVersion { get; set; }

    /// <summary>The content, given the current viewport.</summary>
    [Parameter] public RenderFragment<BlowViewport>? ChildContent { get; set; }

    /// <summary>
    /// What the content depends on: it is re-rendered only when this returns something different from last time.
    /// Null re-renders on every change of the viewport.
    /// </summary>
    [Parameter] public Func<BlowViewport, object?>? Select { get; set; }

    private bool _parametersChanged = true;
    private object? _rendered;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Told apart from a change of the viewport: new content or a new selector always renders. A change of the
        // viewport hands the component its previous parameters again, the very same instances.
        parameters.TryGetValue<RenderFragment<BlowViewport>>(nameof(ChildContent), out var content);
        parameters.TryGetValue<Func<BlowViewport, object?>>(nameof(Select), out var select);
        if (!ReferenceEquals(content, ChildContent) || !ReferenceEquals(select, Select)) _parametersChanged = true;

        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        if (_parametersChanged || Select is null) return true;
        return !Equals(Select(CurrentViewport), _rendered);
    }

    private BlowViewport CurrentViewport => Flow?.Viewport ?? BlowViewport.Identity;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var viewport = CurrentViewport;
        _parametersChanged = false;
        _rendered = Select?.Invoke(viewport);
        builder.AddContent(0, ChildContent?.Invoke(viewport));
    }
}
