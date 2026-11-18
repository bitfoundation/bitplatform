using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// The container of a <see cref="BitBhTagsInput"/>'s tags: a <c>role="grid"</c> whose rows are the
/// <see cref="BitBhTagsInputTag"/>s (<c>role="group"</c> while there are none). Named by the field's label
/// inside a <see cref="BitBhField"/>, otherwise give it an <c>aria-label</c> (default: "Tags").
/// </summary>
public class BitBhTagsInputList : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhTagsInputContext? Context { get; set; }

    /// <summary>The tags.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        var empty = owner.CurrentValues.Count == 0;
        attrs["role"] = empty ? "group" : "grid";
        if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
        {
            if (owner.Field?.LabelledBy is { } label) attrs["aria-labelledby"] = label;
            else attrs["aria-label"] = Strings.Tags;
        }
        SetDataFlag(attrs, "empty", empty);
        SetDataFlag(attrs, "disabled", owner.IsDisabled);
        return attrs;
    }
}
