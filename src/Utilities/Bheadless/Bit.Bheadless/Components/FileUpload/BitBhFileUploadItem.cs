using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>A chosen file of a <see cref="BitBhFileUpload"/> (<c>&lt;li&gt;</c>), holding its name, size and remove button.</summary>
public class BitBhFileUploadItem : BitBhComponentBase
{
    /// <summary>The file.</summary>
    [Parameter, EditorRequired]
    public IBrowserFile File { get; set; } = default!;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.OpenComponent<CascadingValue<BitBhFileUploadItem>>(30);
        builder.AddComponentParameter(31, "Value", this);
        builder.AddComponentParameter(32, "IsFixed", true);
        builder.AddComponentParameter(33, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-type"] = File.ContentType ?? string.Empty;
        return attrs;
    }
}
