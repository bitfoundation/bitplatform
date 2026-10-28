using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Renders <see cref="ChildContent"/> but only re-renders it when
/// <see cref="Version"/> changes. Composite widgets wrap their consumer content
/// in a boundary so that their own frequent state changes (the highlighted item,
/// <c>aria-activedescendant</c>) don't re-render every child item — items are
/// refreshed individually instead. A consumer re-render bumps the version.
/// </summary>
internal sealed class BitBhRenderBoundary : ComponentBase
{
    private int _renderedVersion = -1;

    [Parameter]
    public int Version { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override bool ShouldRender() => _renderedVersion != Version;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _renderedVersion = Version;
        builder.AddContent(0, ChildContent);
    }

    /// <summary>Adds a boundary to <paramref name="builder"/>.</summary>
    internal static void Render(RenderTreeBuilder builder, int sequence, int version, RenderFragment? content)
    {
        builder.OpenComponent<BitBhRenderBoundary>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(Version), version);
        builder.AddComponentParameter(sequence + 2, nameof(ChildContent), content);
        builder.CloseComponent();
    }
}
