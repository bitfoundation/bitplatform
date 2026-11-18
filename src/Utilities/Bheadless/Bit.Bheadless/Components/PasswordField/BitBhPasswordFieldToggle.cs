using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The show / hide button of a <see cref="BitBhPasswordField"/> ("Show password" / "Hide password" unless you name it).
/// Its <see cref="Template"/> receives whether the password is shown, for the icon.
/// </summary>
public class BitBhPasswordFieldToggle : BitBhComponentBase
{
    private bool _pointer;

    [CascadingParameter]
    private BitBhPasswordField? PasswordField { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Content that receives whether the password is shown.</summary>
    [Parameter]
    public RenderFragment<bool>? Template { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerdown", On<PointerEventArgs>("onpointerdown", () => _pointer = true));
        builder.AddAttribute(31, "onclick", On<MouseEventArgs>("onclick", ToggleAsync));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, BitBhContent.Pick(Template, ChildContent, PasswordField?.IsVisible ?? false));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        if (PasswordField is { } field)
        {
            attrs["aria-label"] = field.IsVisible ? Strings.HidePassword : Strings.ShowPassword;
            attrs["aria-controls"] = field.InputId;
            SetDataFlag(attrs, "visible", field.IsVisible);
        }
        attrs["data-bit-bh-interactive"] = "";
        return attrs;
    }

    private async Task ToggleAsync()
    {
        var pointer = _pointer;
        _pointer = false;
        if (PasswordField is not { } field) return;
        await field.SetVisibleAsync(!field.IsVisible);
        // A pointer toggle goes back to typing; a keyboard one stays on the button.
        if (pointer) await Interop.FocusAsync(field.InputId, preventScroll: true, select: false);
    }
}
