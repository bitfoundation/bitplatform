using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>Shared behaviour of the <see cref="BitBhEditable"/> trigger buttons.</summary>
public abstract class BitBhEditableTriggerBase : BitBhComponentBase
{
    [CascadingParameter]
    private BitBhEditableContext? Context { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>The owning editable.</summary>
    protected BitBhEditable? Owner => Context?.Owner;

    protected override string DefaultTag => "button";

    /// <summary>Whether the trigger is shown while editing (submit / cancel) or while not (edit).</summary>
    protected abstract bool ShownWhileEditing { get; }

    /// <summary>What the trigger does.</summary>
    protected abstract Task ActivateAsync(BitBhEditable owner);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", On<MouseEventArgs>("onclick", () => Owner is { } owner ? ActivateAsync(owner) : Task.CompletedTask));
        // The input's blur would commit or cancel before this click: the press keeps the focus in the input.
        if (ShownWhileEditing)
        {
            builder.AddAttribute(31, "onmousedown", On<MouseEventArgs>("onmousedown", static () => { }));
            builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        }
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Owner is not { } owner) return attrs;
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
        {
            attrs["type"] = "button";
            if (!owner.CanEdit) attrs["disabled"] = true;
        }
        if (owner.IsEditing != ShownWhileEditing) attrs["hidden"] = true;
        attrs["data-bit-bh-interactive"] = "";
        owner.AddState(attrs);
        return attrs;
    }
}
