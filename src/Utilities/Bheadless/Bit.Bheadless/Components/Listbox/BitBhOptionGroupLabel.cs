using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The heading of a <see cref="BitBhOptionGroup"/>: names the group (<c>aria-labelledby</c>) and is itself skipped by
/// assistive technology's option count (<c>role="presentation"</c>).
/// </summary>
public class BitBhOptionGroupLabel : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhOptionGroupContext? Group { get; set; }

    /// <summary>The heading text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => Group?.RegisterLabel(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", Group?.LabelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "presentation";
        return attrs;
    }

    public void Dispose() => Group?.RegisterLabel(false);
}
