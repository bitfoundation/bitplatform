using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A label for the control of the enclosing <see cref="BitBhField"/>. Renders as
/// <c>&lt;label for="…"&gt;</c>; non-native controls (listbox, radio group, slider …)
/// reference it through <c>aria-labelledby</c>.
///
/// <para>Can also be used standalone: set <see cref="For"/> to the id of the control.</para>
/// </summary>
public class BitBhLabel : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    /// <summary>Id of the labelled control when used outside a <see cref="BitBhField"/>.</summary>
    [Parameter]
    public string? For { get; set; }

    /// <summary>Label content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "label";

    protected override void OnInitialized() => FieldContext?.RegisterLabel(true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", FieldContext?.LabelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (For is null && FieldContext is { LabelTargetsControl: false } field)
            builder.AddAttribute(25, "onclick", On<Microsoft.AspNetCore.Components.Web.MouseEventArgs>("onclick", field.FocusControlAsync));
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        // <label for> only works with labelable elements; other controls reference the
        // label through aria-labelledby and are focused by the click handler.
        var target = For ?? (FieldContext is { LabelTargetsControl: true } ? FieldContext.ControlId : null);
        if (target is not null && Tag.Equals("label", StringComparison.OrdinalIgnoreCase))
            attrs["for"] = target;
        SetDataFlag(attrs, "disabled", FieldContext?.Disabled ?? false);
        SetDataFlag(attrs, "invalid", FieldContext?.Invalid ?? false);
        SetDataFlag(attrs, "required", FieldContext?.Required ?? false);
        return attrs;
    }

    public void Dispose() => FieldContext?.RegisterLabel(false);
}
