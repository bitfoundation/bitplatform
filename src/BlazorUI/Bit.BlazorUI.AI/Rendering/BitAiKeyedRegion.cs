namespace Bit.BlazorUI;

/// <summary>
/// Renders its content unchanged, adding no element of its own.
/// </summary>
/// <remarks>
/// It exists to carry a <c>@key</c> around content the library does not own, such as an
/// application's message template. A key needs an element or a component to sit on; this provides
/// the component without adding a wrapper element that would disturb the flex layout and gaps of the
/// list the content sits in.
/// </remarks>
internal sealed class BitAiKeyedRegion : ComponentBase
{
    /// <summary>The content to render.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ChildContent);
}
