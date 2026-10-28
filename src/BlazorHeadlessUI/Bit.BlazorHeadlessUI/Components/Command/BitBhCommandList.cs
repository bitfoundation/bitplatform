using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>
/// The scrollable result list of a <see cref="BitBhCommand"/> (<c>role="listbox"</c>).
/// Exposes <c>--bit-bh-command-list-height</c> — the height of its content — so the
/// palette can animate its height as results change.
/// </summary>
public class BitBhCommandList : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    /// <summary>Groups, items, separators, the empty state.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized()
    {
        if (CommandContext is null) return;
        CommandContext.List = this;
        // The input (rendered first) names the list in aria-controls only once it exists.
        CommandContext.Input?.Refresh();
    }

    public void Dispose()
    {
        if (CommandContext?.List != this) return;
        CommandContext.List = null;
        CommandContext.Input?.Refresh();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", CommandContext?.ListId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "listbox";
        // The label exists only when the command has a Label; without one the list is named by the
        // consumer's aria-label / aria-labelledby, never by a reference to nothing.
        if (CommandContext is { HasLabel: true } && GetAdditionalAttribute("aria-label") is null)
            attrs["aria-labelledby"] = CommandContext.LabelId;
        attrs["data-bit-bh-command-list"] = "";
        return attrs;
    }
}
