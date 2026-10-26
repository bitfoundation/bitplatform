using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// Help text for the control of the enclosing <see cref="BitBhField"/>, linked through
/// <c>aria-describedby</c>. Renders as <c>&lt;p&gt;</c> by default. A field may contain
/// several descriptions; all of them are referenced.
/// </summary>
public class BitBhDescription : BitBhComponentBase, IDisposable
{
    private string? _id;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    /// <summary>Description text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "p";

    private string ElementId => _id ?? ComponentId;

    protected override void OnInitialized()
    {
        if (FieldContext is null) return;
        _id = Id ?? FieldContext.NextId("description");
        FieldContext.RegisterDescription(_id, true);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ElementId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", FieldContext?.Disabled ?? false);
        return attrs;
    }

    public void Dispose()
    {
        if (_id is not null) FieldContext?.RegisterDescription(_id, false);
    }
}
