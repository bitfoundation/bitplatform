using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>Content of a <see cref="BitBhClipboard"/> shown only while the value was just copied, or only while it wasn't (<see cref="WhenCopied"/> = false).</summary>
public class BitBhClipboardIndicator : ComponentBase
{
    [CascadingParameter]
    private BitBhClipboard? Clipboard { get; set; }

    /// <summary>Show the content while copied (true, default) or while not.</summary>
    [Parameter]
    public bool WhenCopied { get; set; } = true;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if ((Clipboard?.IsCopied ?? false) == WhenCopied) builder.AddContent(0, ChildContent);
    }
}
