using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The value of a <see cref="BitBhEditable"/> while it isn't edited (its placeholder while empty). Activates editing as
/// the editable's <c>ActivationMode</c> says; hidden while editing.
/// </summary>
public class BitBhEditablePreview : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    /// <summary>Content replacing the value text.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var owner = Context?.Owner;
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", owner?.PreviewId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (owner is not null)
        {
            switch (owner.ActivationMode)
            {
                case BitBhEditableActivation.Focus:
                    builder.AddAttribute(30, "onfocus", On<FocusEventArgs>("onfocus", owner.HandlePreviewFocusAsync));
                    break;
                case BitBhEditableActivation.Click:
                    builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => owner.EditAsync()));
                    builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key is "Enter" or " " ? owner.EditAsync() : Task.CompletedTask));
                    break;
                case BitBhEditableActivation.DoubleClick:
                    builder.AddAttribute(30, "ondblclick", On<MouseEventArgs>("ondblclick", () => owner.EditAsync()));
                    builder.AddAttribute(31, "onkeydown", On<KeyboardEventArgs>("onkeydown", e => e.Key is "Enter" ? owner.EditAsync() : Task.CompletedTask));
                    break;
            }
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (ChildContent is not null) builder.AddContent(50, ChildContent);
        else if (owner is not null) builder.AddContent(50, owner.CurrentValue.Length > 0 ? owner.CurrentValue : owner.Placeholder);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Context?.Owner is not { } owner) return attrs;
        if (owner.IsEditing) attrs["hidden"] = true;
        if (owner.CanEdit && owner.ActivationMode != BitBhEditableActivation.None)
        {
            attrs["tabindex"] = "0";
            if (owner.ActivationMode != BitBhEditableActivation.Focus)
            {
                attrs["role"] = "button";
                attrs[BitBhKeyboard.KeysAttribute] = owner.ActivationMode == BitBhEditableActivation.Click ? "Enter Space" : "Enter";
            }
        }
        attrs["data-bit-bh-interactive"] = "";
        owner.AddState(attrs);
        return attrs;
    }
}
