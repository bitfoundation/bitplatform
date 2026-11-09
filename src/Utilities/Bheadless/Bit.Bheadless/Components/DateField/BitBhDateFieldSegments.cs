using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Renders every segment of the enclosing <see cref="BitBhDateField"/>, <see cref="BitBhTimeField"/> or
/// <see cref="BitBhDatePicker"/>, separators included, in the order the culture writes them. It renders no element of its
/// own. Give it content to render the segments yourself (to style them); its context is the <see cref="BitBhDateSegment"/>:
/// <code>
/// &lt;BitBhDateFieldSegments&gt;
///     &lt;BitBhDateFieldSegment Segment="context" class="segment" /&gt;
/// &lt;/BitBhDateFieldSegments&gt;
/// </code>
/// </summary>
public class BitBhDateFieldSegments : ComponentBase
{
    [CascadingParameter]
    private BitBhDateFieldContext? Context { get; set; }

    /// <summary>
    /// Renders each segment — normally a <see cref="BitBhDateFieldSegment"/> with <c>Segment="context"</c>. Default: a plain
    /// one. Inside other templated content (an <c>EditForm</c>) name the context: <c>Context="segment"</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BitBhDateSegment>? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null) return;
        foreach (var segment in Context.Segments)
        {
            if (ChildContent is not null)
            {
                builder.AddContent(0, ChildContent(segment));
            }
            else
            {
                builder.OpenComponent<BitBhDateFieldSegment>(1);
                builder.AddComponentParameter(2, nameof(BitBhDateFieldSegment.Segment), segment);
                builder.CloseComponent();
            }
        }
    }
}
