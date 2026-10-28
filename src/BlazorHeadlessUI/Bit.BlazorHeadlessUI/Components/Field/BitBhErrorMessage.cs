using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The error message of the enclosing <see cref="BitBhField"/>. Rendered only while the
/// field is invalid and then referenced by the control's <c>aria-describedby</c>.
///
/// <para>
/// Without child content it lists the validation messages reported by the field's
/// control (when bound with <c>@bind-Value</c> inside an <c>EditForm</c>). With child
/// content it renders that instead, receiving the messages as context.
/// </para>
///
/// <code>
/// &lt;BitBhErrorMessage class="error" /&gt;
/// &lt;BitBhErrorMessage Context="messages"&gt;⚠ @string.Join(" ", messages)&lt;/BitBhErrorMessage&gt;
/// </code>
/// </summary>
public class BitBhErrorMessage : BitBhComponentBase, IDisposable
{
    private string? _id;

    [CascadingParameter]
    private BitBhFieldContext? FieldContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>IReadOnlyList&lt;string&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<IReadOnlyList<string>>? Template { get; set; }

    /// <summary>Render even when the field is valid (the content stays hidden from AT via <c>hidden</c>).</summary>
    [Parameter]
    public bool ForceMount { get; set; }

    protected override string DefaultTag => "p";

    private bool IsInvalid => FieldContext?.Invalid ?? true;
    private string ElementId => _id ?? ComponentId;

    protected override void OnInitialized()
    {
        if (FieldContext is null) return;
        _id = Id ?? FieldContext.NextId("error");
        FieldContext.RegisterError(_id, true);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsInvalid && !ForceMount) return;

        var messages = FieldContext?.Messages ?? [];
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ElementId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (!IsInvalid)
            builder.AddAttribute(25, "hidden", true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(40, BitBhContent.Pick(Template, ChildContent, messages));
        else
            builder.AddContent(41, string.Join(" ", messages));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "invalid", IsInvalid);
        return attrs;
    }

    public void Dispose()
    {
        if (_id is not null) FieldContext?.RegisterError(_id, false);
    }
}
